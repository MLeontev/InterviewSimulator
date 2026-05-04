namespace Framework.Domain;

/// <summary>
/// Ошибка валидации входных данных
/// </summary>
public record ValidationError : Error
{
    /// <summary>
    /// Ошибки валидации по полям запроса
    /// </summary>
    public Dictionary<string, string[]> Errors { get; } = [];

    public ValidationError(Dictionary<string, string[]> errors) 
        : base("VALIDATION_ERROR", 
            "Возникли одна или несколько ошибок валидации", 
            ErrorType.Validation)
    {
        Errors = errors;
    }
}
