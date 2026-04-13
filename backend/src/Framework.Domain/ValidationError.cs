namespace Framework.Domain;

public record ValidationError : Error
{
    public Dictionary<string, string[]> Errors { get; } = [];

    public ValidationError(Dictionary<string, string[]> errors) 
        : base("VALIDATION_ERROR", 
            "Возникли одна или несколько ошибок валидации", 
            ErrorType.Validation)
    {
        Errors = errors;
    }
}