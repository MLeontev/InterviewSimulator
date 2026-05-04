namespace Interview.Presentation.Requests;

/// <summary>
/// Запрос на создание новой тренировочной сессии собеседования
/// </summary>
public record CreateSessionRequest
{
    /// <summary>
    /// Идентификатор пресета собеседования
    /// </summary>
    public Guid InterviewPresetId { get; init; }
}
