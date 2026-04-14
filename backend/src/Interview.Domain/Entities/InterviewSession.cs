using Framework.Domain;
using Interview.Domain.Enums;
using Interview.Domain.Policies;

namespace Interview.Domain.Entities;

public class InterviewSession
{
    private readonly List<InterviewQuestion> _questions = [];
    public IReadOnlyList<InterviewQuestion> Questions => _questions;
    
    public Guid Id { get; private set; }
    
    public Guid CandidateId { get; private set; }
    
    public Guid InterviewPresetId { get; private set; }
    public string InterviewPresetName { get; private set; } = string.Empty;

    public DateTime StartedAt { get; private set; }
    public DateTime PlannedEndAt { get; private set; }
    public DateTime? FinishedAt { get; private set; }

    public InterviewStatus Status { get; private set; }
    public SessionVerdict SessionVerdict { get; private set; } = SessionVerdict.None;
    
    public string? AiFeedbackJson { get; private set; }
    public int AiRetryCount { get; private set; }
    public DateTime? AiNextRetryAt { get; private set; }

    public static InterviewSession Create(
        Guid id,
        Guid candidateId,
        Guid interviewPresetId,
        string interviewPresetName,
        DateTime startedAt,
        DateTime plannedEndAt,
        IReadOnlyCollection<InterviewQuestion>? questions = null)
    {
        var session = new InterviewSession
        {
            Id = id,
            CandidateId = candidateId,
            InterviewPresetId = interviewPresetId,
            InterviewPresetName = interviewPresetName,
            StartedAt = startedAt,
            PlannedEndAt = plannedEndAt,
            Status = InterviewStatus.InProgress
        };

        if (questions is not null)
        {
            foreach (var question in questions.OrderBy(q => q.OrderIndex))
                session._questions.Add(question);
        }

        return session;
    }

    public Result Finish(DateTime nowUtc)
    {
        if (Status != InterviewStatus.InProgress)
        {
            return Result.Failure(Error.Business(
                "SESSION_CANNOT_BE_FINISHED",
                "Сессию нельзя завершить в текущем статусе"));
        }

        Status = InterviewStatus.Finished;
        FinishedAt = nowUtc;

        foreach (var question in Questions)
            question.MarkSkippedWhenSessionFinishes();

        return Result.Success();
    }

    public Result ResetForAiRetry(DateTime nowUtc)
    {
        if (Status != InterviewStatus.AiEvaluationFailed)
        {
            return Result.Failure(Error.Business(
                "SESSION_NOT_FAILED",
                "ИИ-оценка сессии не была завершена с ошибкой"));
        }

        var retriedQuestions = 0;

        foreach (var question in Questions.Where(x => x.Status == QuestionStatus.AiEvaluationFailed))
        {
            var retryResult = question.ResetForAiRetry(nowUtc);
            if (retryResult.IsFailure)
                return Result.Failure(retryResult.Error);

            if (retryResult.Value)
                retriedQuestions++;
        }

        if (retriedQuestions == 0)
        {
            return Result.Failure(Error.Business(
                "NO_FAILED_AI_QUESTIONS",
                "Нет заданий с ошибкой ИИ-оценки"));
        }

        Status = InterviewStatus.Finished;
        AiRetryCount = 0;
        AiNextRetryAt = null;
        AiFeedbackJson = null;

        return Result.Success();
    }

    public bool HasPendingQuestionsForAiEvaluation() =>
        Questions.Any(q => InterviewQuestionStatusRules.PendingSessionEvaluation.Contains(q.Status));

    public Result CancelAiEvaluation()
    {
        if (Status != InterviewStatus.EvaluatingAi)
        {
            return Result.Failure(Error.Business(
                "SESSION_NOT_IN_EVALUATING_AI",
                "Сессия не готова к AI-оценке"));
        }

        Status = InterviewStatus.Finished;
        return Result.Success();
    }

    public Result ApplyAiEvaluationSuccess(string rawJson, double overallScore)
    {
        if (Status != InterviewStatus.EvaluatingAi)
        {
            return Result.Failure(Error.Business(
                "SESSION_NOT_IN_EVALUATING_AI",
                "Сессия не готова к AI-оценке"));
        }

        AiFeedbackJson = rawJson;
        SessionVerdict = InterviewSessionScoringPolicy.ResolveVerdict(overallScore);
        Status = InterviewStatus.Evaluated;
        AiRetryCount = 0;
        AiNextRetryAt = null;

        return Result.Success();
    }

    public Result ScheduleAiEvaluationRetry(int nextRetryCount, DateTime nextRetryAtUtc)
    {
        if (Status != InterviewStatus.EvaluatingAi)
        {
            return Result.Failure(Error.Business(
                "SESSION_NOT_IN_EVALUATING_AI",
                "Сессия не готова к AI-оценке"));
        }

        AiRetryCount = nextRetryCount;
        AiNextRetryAt = nextRetryAtUtc;
        Status = InterviewStatus.Finished;

        return Result.Success();
    }

    public Result MarkAiEvaluationFailed(
        int retryCount,
        double overallScore,
        string fallbackAiFeedbackJson)
    {
        if (Status != InterviewStatus.EvaluatingAi)
        {
            return Result.Failure(Error.Business(
                "SESSION_NOT_IN_EVALUATING_AI",
                "Сессия не готова к AI-оценке"));
        }

        AiRetryCount = retryCount;
        AiNextRetryAt = null;
        Status = InterviewStatus.AiEvaluationFailed;
        SessionVerdict = InterviewSessionScoringPolicy.ResolveVerdict(overallScore);
        AiFeedbackJson ??= fallbackAiFeedbackJson;

        return Result.Success();
    }
}
