namespace Interview.Domain.Enums;

/// <summary>
/// Состояние сессии собеседования
/// </summary>
public enum InterviewStatus
{
    InProgress,
    Finished,
    EvaluatingAi,
    Evaluated,
    AiEvaluationFailed
}
