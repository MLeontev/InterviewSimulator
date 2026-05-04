using System.ComponentModel.DataAnnotations;

namespace Interview.Presentation.Requests;

/// <summary>
/// Запрос на отправку черновика программного кода на тестовую проверку
/// </summary>
public record SubmitDraftCodeRequest
{
    /// <summary>
    /// Исходный код решения
    /// </summary>
    [Required]
    public string Code { get; init; } = string.Empty;
}
