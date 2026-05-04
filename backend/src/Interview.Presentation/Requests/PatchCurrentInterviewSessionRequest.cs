namespace Interview.Presentation.Requests;

/// <summary>
/// Запрос на изменение состояния текущей сессии собеседования
/// </summary>
public record PatchCurrentInterviewSessionRequest
{
    /// <summary>
    /// Новое состояние текущей сессии
    /// </summary>
    public InterviewSessionStatusPatch Status { get; init; }
}

/// <summary>
/// Допустимое состояние текущей сессии
/// </summary>
public enum InterviewSessionStatusPatch
{
    /// <summary>
    /// Завершить текущую сессию
    /// </summary>
    Finished
}
