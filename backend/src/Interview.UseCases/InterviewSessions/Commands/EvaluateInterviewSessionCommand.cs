using System.Text.Json;
using Framework.Domain;
using Interview.Domain.Entities;
using Framework.UseCases.Resilience;
using Interview.Domain.Enums;
using Interview.Domain.Policies;
using Interview.Infrastructure.Interfaces.AiEvaluation;
using Interview.Infrastructure.Interfaces.AiEvaluation.Session;
using Interview.Infrastructure.Interfaces.DataAccess;
using Interview.UseCases.Services;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using QuestionBank.ModuleContract;
using QuestionType = Interview.Domain.Enums.QuestionType;

namespace Interview.UseCases.InterviewSessions.Commands;

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

        if (session.HasPendingQuestionsForAiEvaluation())
        {
            var cancelResult = session.CancelAiEvaluation();
            if (cancelResult.IsFailure)
                return Result.Failure(cancelResult.Error);

            await dbContext.SaveChangesAsync(cancellationToken);
            return Result.Failure(Error.Business("SESSION_HAS_PENDING_QUESTIONS", "Есть задания, которые еще не оценены"));
        }
        
        try
        {
            var preset = await questionBankApi.GetPresetDetailsAsync(session.InterviewPresetId, cancellationToken);
            if (preset is null)
            {
                var cancelResult = session.CancelAiEvaluation();
                if (cancelResult.IsFailure)
                    return Result.Failure(cancelResult.Error);

                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Failure(Error.NotFound("PRESET_NOT_FOUND", "Пресет интервью не найден"));
            }

            var questionResults = session.Questions
                .OrderBy(q => q.OrderIndex)
                .Select(MapQuestionResult)
                .ToList();

            var competencyResults = BuildCompetencyResults(session.Questions, preset.Competencies);

            var questionScores = session.Questions
                .Select(InterviewQuestionScoreResolver.Resolve)
                .ToList();

            var overallScore = InterviewSessionScoringPolicy.CalculateOverallScore(questionScores);

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

            var applyResult = session.ApplyAiEvaluationSuccess(
                aiResult.RawJson,
                overallScore);
            
            if (applyResult.IsFailure)
                return Result.Failure(applyResult.Error);

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
                var nextRetryAt = RetryBackoff.NextRetryAtUtc(
                    nextRetry,
                    _retry.BaseDelaySeconds,
                    _retry.MaxDelaySeconds,
                    _retry.JitterSeconds);

                var scheduleRetryResult = session.ScheduleAiEvaluationRetry(nextRetry, nextRetryAt);
                if (scheduleRetryResult.IsFailure)
                    return Result.Failure(scheduleRetryResult.Error);

                await dbContext.SaveChangesAsync(cancellationToken);
                return Result.Failure(Error.External(
                    "SESSION_AI_EVALUATION_RETRY_SCHEDULED",
                    $"Запланирован повтор AI-оценки ({nextRetry}/{_retry.MaxRetries})"));
            }
            
            var fallbackQuestionScores = session.Questions
                .Select(InterviewQuestionScoreResolver.Resolve)
                .ToList();

            var fallbackOverallScore = InterviewSessionScoringPolicy.CalculateOverallScore(fallbackQuestionScores);

            var failResult = session.MarkAiEvaluationFailed(
                nextRetry,
                fallbackOverallScore,
                BuildFallbackSessionAiFeedbackJson());
            
            if (failResult.IsFailure)
                return Result.Failure(failResult.Error);

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
    
    private static string MapQuestionStatusForLlm(InterviewQuestion q) =>
        q.Status switch
        {
            QuestionStatus.NotStarted => "без ответа",
            QuestionStatus.Skipped => "пропущено",
            QuestionStatus.AiEvaluationFailed => "ошибка AI-оценки",
            _ => "оценено"
        };
}
