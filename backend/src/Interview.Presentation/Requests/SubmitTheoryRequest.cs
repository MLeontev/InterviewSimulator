using System.ComponentModel.DataAnnotations;

namespace Interview.Presentation.Requests;

/// <summary>
/// Запрос на отправку ответа на теоретический вопрос
/// </summary>
public record SubmitTheoryRequest
{
    /// <summary>
    /// Текстовый ответ кандидата
    /// </summary>
    [Required]
    public string Answer { get; init; } = string.Empty;
}
