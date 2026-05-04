namespace Interview.Presentation.Requests;

/// <summary>
/// Запрос на изменение состояния текущего задания
/// </summary>
public record PatchCurrentInterviewQuestionRequest
{
    /// <summary>
    /// Новое состояние текущего задания
    /// </summary>
    public InterviewQuestionStatusPatch Status { get; init; }
}

/// <summary>
/// Допустимое состояние текущего задания
/// </summary>
public enum InterviewQuestionStatusPatch
{
    /// <summary>
    /// Начать выполнение задания
    /// </summary>
    InProgress,

    /// <summary>
    /// Пропустить задание
    /// </summary>
    Skipped
}
