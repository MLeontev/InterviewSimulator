using System.Text.Json.Serialization;

namespace Framework.Domain;

public record Error
{
    public static readonly Error None = new(string.Empty, string.Empty, ErrorType.None);
    
    public string Code { get; }
    public string Description { get; }
    
    [JsonIgnore]
    public ErrorType Type { get; }

    public Error(string code, string description, ErrorType type)
    {
        Code = code;
        Description = description;
        Type = type;
    }
    
    public static Error Validation(Dictionary<string, string[]> errors) => new ValidationError(errors);
    public static Error NotFound(string code, string description) => new(code, description, ErrorType.NotFound);
    public static Error Conflict(string code, string description) => new(code, description, ErrorType.Conflict);
    public static Error Business(string code, string description) => new(code, description, ErrorType.Business);
}