using System.Text.Json;
using Framework.Domain;
using Interview.Domain;
using Interview.Infrastructure.Interfaces.AiEvaluation;
using Interview.Infrastructure.Interfaces.AiEvaluation.Session;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestionBank.ModuleContract;
using QuestionType = Interview.Domain.QuestionType;

namespace Interview.UseCases.Commands;

public record EvaluateInterviewSessionCommand(Guid SessionId) : IRequest<Result>;

internal class EvaluateInterviewSessionCommandHandler(
    IDbContext dbContext,
    IAiEvaluationService aiEvaluationService,
    IQuestionBankApi questionBankApi,
    IOptions<InterviewAiRetryOptions> retryOptions,
    ILogger<EvaluateInterviewSessionCommandHandler> logger) : IRequestHandler<EvaluateInterviewSessionCommand, Result>
{
    private readonly InterviewAiRetryOptions _retry = retryOptions.Value;
    
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

            var overallScore = CalculateOverallScore(session.Questions);

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
            session.AiRetryCount = 0;
            session.AiNextRetryAt = null;

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Ошибка AI-оценки сессии {SessionId}", session.Id);

            var nextRetry = session.AiRetryCount + 1;

            if (nextRetry <= _retry.MaxRetries)
            {
                session.AiRetryCount = nextRetry;
                session.AiNextRetryAt = AiRetryBackoff.NextRetryAtUtc(nextRetry, _retry);
                session.Status = InterviewStatus.Finished;

                await dbContext.SaveChangesAsync(cancellationToken);

                return Result.Failure(Error.External(
                    "SESSION_AI_EVALUATION_RETRY_SCHEDULED",
                    $"Запланирован повтор AI-оценки ({nextRetry}/{_retry.MaxRetries})"));
            }
            
            var fallbackOverallScore = CalculateOverallScore(session.Questions);

            session.AiRetryCount = nextRetry;
            session.AiNextRetryAt = null;
            session.Status = InterviewStatus.Evaluated;
            session.FinishedAt ??= DateTime.UtcNow;
            session.SessionVerdict = MapSessionVerdict(fallbackOverallScore);
            session.AiFeedbackJson ??= BuildFallbackSessionAiFeedbackJson();

            await dbContext.SaveChangesAsync(cancellationToken);

            return Result.Failure(Error.External(
                "SESSION_AI_EVALUATION_FAILED",
                "Не удалось выполнить AI-оценку сессии после нескольких попыток"));
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

        if (AiFeedbackJsonParser.TryParseQuestion(q.AiFeedbackJson, out var aiScore, out var aiFeedback))
        {
            return new SessionQuestionResult(title, status, aiScore, aiFeedback);
        }

        return new SessionQuestionResult(title, status, InterviewQuestionScoreResolver.Resolve(q), ResolveQuestionFeedbackFallback(q));
    }

    private IReadOnlyList<SessionCompetencyResult> BuildCompetencyResults(
        IReadOnlyCollection<InterviewQuestion> questions,
        IReadOnlyCollection<PresetCompetencyApiDto> presetCompetencies)
    {
        var scoredQuestions = questions
            .Where(q => q.CompetencyId.HasValue)
            .Select(q => new { q.CompetencyId, Score = (double)InterviewQuestionScoreResolver.Resolve(q) })
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
    
    private double CalculateOverallScore(IReadOnlyCollection<InterviewQuestion> questions)
    {
        if (questions.Count == 0) return 0;
        return Math.Round(questions.Average(InterviewQuestionScoreResolver.Resolve), 2);
    }
    
    private string ResolveQuestionFeedbackFallback(InterviewQuestion q) =>
        q.Status switch
        {
            QuestionStatus.NotStarted => "Ответ на это задание не был дан",
            QuestionStatus.Skipped => "Это задание было пропущено",
            _ => string.IsNullOrWhiteSpace(q.ErrorMessage)
                ? "Оценка сформирована по итоговому вердикту"
                : q.ErrorMessage!
        };
    
    private string BuildFallbackSessionAiFeedbackJson() =>
        JsonSerializer.Serialize(new
        {
            summary = "Не удалось получить итоговую AI-оценку сессии: сервис временно недоступен.",
            strengths = Array.Empty<string>(),
            weaknesses = Array.Empty<string>(),
            recommendations = Array.Empty<string>()
        });

    private SessionVerdict MapSessionVerdict(double overallScore) =>
        overallScore switch
        {
            >= 7 => SessionVerdict.Passed,
            >= 4 => SessionVerdict.Borderline,
            _ => SessionVerdict.Failed
        };
    
    private static string MapQuestionStatusForLlm(InterviewQuestion q) =>
        q.Status switch
        {
            QuestionStatus.NotStarted => "без ответа",
            QuestionStatus.Skipped => "пропущено",
            _ => "оценено"
        };
}
