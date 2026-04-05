using System.Text.Json;
using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.AiEvaluation;
using Interview.Infrastructure.Interfaces.AiEvaluation.Session;
using Interview.Infrastructure.Interfaces.DataAccess;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using QuestionBank.ModuleContract;
using QuestionType = Interview.Domain.QuestionType;

namespace Interview.UseCases.Commands;

public record EvaluateInterviewSessionCommand(Guid SessionId) : IRequest<Result>;

internal class EvaluateInterviewSessionCommandHandler(
    IDbContext dbContext,
    IAiEvaluationService aiEvaluationService,
    IQuestionBankApi questionBankApi,
    ILogger<EvaluateInterviewSessionCommandHandler> logger) : IRequestHandler<EvaluateInterviewSessionCommand, Result>
{
    public async Task<Result> Handle(EvaluateInterviewSessionCommand request, CancellationToken cancellationToken)
    {
        var session = await dbContext.InterviewSessions
            .Include(s => s.Questions)
            .FirstOrDefaultAsync(s => s.Id == request.SessionId, cancellationToken);

        if (session is null)
            return Result.Failure(Error.NotFound("SESSION_NOT_FOUND", "Сессия не найдена"));

        if (session.Status != InterviewStatus.EvaluatingAi)
            return Result.Failure(Error.Business("SESSION_NOT_IN_EVALUATING_AI", "Сессия не готова к AI-оценке"));

        var hasPendingQuestions = session.Questions.Any(q =>
            q.Status is QuestionStatus.Submitted
                or QuestionStatus.EvaluatingCode
                or QuestionStatus.EvaluatedCode
                or QuestionStatus.EvaluatingAi);

        if (hasPendingQuestions)
        {
            session.Status = InterviewStatus.Finished;
            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure(Error.Business("SESSION_HAS_PENDING_QUESTIONS", "Есть задания, которые еще не оценены"));
        }
        
        try
        {
            var preset = await questionBankApi.GetPresetDetailsAsync(session.InterviewPresetId, cancellationToken);
            if (preset is null)
            {
                session.Status = InterviewStatus.Finished;
                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Failure(Error.NotFound("PRESET_NOT_FOUND", "Пресет интервью не найден"));
            }

            var questionResults = session.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(MapQuestionResult)
                .ToList();

            var competencyResults = BuildCompetencyResults(session.Questions, preset.Competencies);

            var overallScore = questionResults.Count > 0
                ? Math.Round(questionResults.Average(x => x.Score), 2)
                : 0;

            var technologyStack = preset.Technologies.Count == 0
                ? "Не указан"
                : string.Join(", ", preset.Technologies);

            var aiResult = await aiEvaluationService.EvaluateSessionAsync(
                new SessionEvaluationRequest(
                    PresetName: preset.Name,
                    TechnologyStack: technologyStack,
                    QuestionResults: questionResults,
                    CompetencyResults: competencyResults,
                    OverallScore: overallScore),
                cancellationToken);

            session.AiFeedbackJson = aiResult.RawJson;
            session.SessionVerdict = MapSessionVerdict(overallScore);
            session.Status = InterviewStatus.Evaluated;
            session.FinishedAt ??= DateTime.UtcNow;

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка AI-оценки сессии {SessionId}", session.Id);

            session.Status = InterviewStatus.Finished;
            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Failure(Error.External("SESSION_AI_EVALUATION_FAILED", "Не удалось выполнить AI-оценку сессии"));
        }
    }
    
    private SessionQuestionResult MapQuestionResult(InterviewQuestion q)
    {
        var questionType = q.Type switch
        {
            QuestionType.Theory => "Теоретический вопрос",
            QuestionType.Coding => "Алгоритмическая задача",
            _ => throw new ArgumentOutOfRangeException(nameof(q.Type), q.Type, null)
        };
        
        var questionTitle = string.IsNullOrWhiteSpace(q.Title) ? "Без названия" : q.Title;
        var title = $"{q.OrderIndex}. {questionType}: {questionTitle}";
        
        var status = MapQuestionStatusForLlm(q);

        if (TryParseQuestionAiJson(q.AiFeedbackJson, out var score, out var feedback))
        {
            return new SessionQuestionResult(
                Title: title,
                Status: status,
                Score: score,
                Feedback: feedback);
        }

        var (fallbackScore, fallbackFeedback) = q.Status switch
        {
            QuestionStatus.NotStarted => (0, "Ответ на это задание не был дан"),
            QuestionStatus.Skipped => (0, "Это задание было пропущено"),
            _ => (
                q.QuestionVerdict switch
                {
                    QuestionVerdict.Correct => 8,
                    QuestionVerdict.PartiallyCorrect => 5,
                    QuestionVerdict.Incorrect => 2,
                    _ => 0
                },
                string.IsNullOrWhiteSpace(q.ErrorMessage)
                    ? "Оценка сформирована по итоговому вердикту"
                    : q.ErrorMessage!
            )
        };

        return new SessionQuestionResult(
            Title: title,
            Status: status,
            Score: fallbackScore,
            Feedback: fallbackFeedback);
    }
    
    private bool TryParseQuestionAiJson(string? rawJson, out int score, out string feedback)
    {
        score = 0;
        feedback = string.Empty;

        if (string.IsNullOrWhiteSpace(rawJson))
            return false;

        try
        {
            using var doc = JsonDocument.Parse(rawJson);
            var root = doc.RootElement;

            if (!root.TryGetProperty("score", out var scoreNode))
                return false;

            score = scoreNode.GetInt32();
            if (score is < 0 or > 10)
                return false;

            feedback = root.TryGetProperty("feedback", out var feedbackNode)
                ? feedbackNode.GetString() ?? string.Empty
                : string.Empty;

            return true;
        }
        catch
        {
            return false;
        }
    }
    
    private static string MapQuestionStatusForLlm(InterviewQuestion q) =>
        q.Status switch
        {
            QuestionStatus.NotStarted => "без ответа",
            QuestionStatus.Skipped => "пропущено",
            _ => "оценено"
        };
    
    private IReadOnlyList<SessionCompetencyResult> BuildCompetencyResults(
        IReadOnlyCollection<InterviewQuestion> questions,
        IReadOnlyCollection<PresetCompetencyApiDto> presetCompetencies)
    {
        var scoredQuestions = questions
            .Select(q =>
            {
                if (TryParseQuestionAiJson(q.AiFeedbackJson, out var parsedScore, out _))
                    return new { q.CompetencyId, Score = (double)parsedScore };

                var fallback = q.QuestionVerdict switch
                {
                    QuestionVerdict.Correct => 8d,
                    QuestionVerdict.PartiallyCorrect => 5d,
                    QuestionVerdict.Incorrect => 2d,
                    _ => 0d
                };

                return new { q.CompetencyId, Score = fallback };
            })
            .Where(x => x.CompetencyId.HasValue)
            .ToList();

        var avgByCompetencyId = scoredQuestions
            .GroupBy(x => x.CompetencyId!.Value)
            .ToDictionary(g => g.Key, g => Math.Round(g.Average(x => x.Score), 1));

        return presetCompetencies
            .Where(c => avgByCompetencyId.ContainsKey(c.CompetencyId))
            .Select(c => new SessionCompetencyResult(
                CompetencyName: c.CompetencyName,
                AverageScore: avgByCompetencyId.GetValueOrDefault(c.CompetencyId, 0)))
            .ToList();
    }

    private SessionVerdict MapSessionVerdict(double overallScore) =>
        overallScore switch
        {
            >= 7 => SessionVerdict.Passed,
            >= 4 => SessionVerdict.Borderline,
            _ => SessionVerdict.Failed
        };
}
