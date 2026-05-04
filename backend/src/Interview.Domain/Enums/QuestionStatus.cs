namespace Interview.Domain.Enums;

/// <summary>
/// Состояние выполнения задания в сессии собеседования
/// </summary>
public enum QuestionStatus
{
    NotStarted,
    InProgress,
    Skipped,
    EvaluatingCode,
    EvaluatedCode,
    Submitted,
    EvaluatingAi,
    EvaluatedAi,
    AiEvaluationFailed
}
