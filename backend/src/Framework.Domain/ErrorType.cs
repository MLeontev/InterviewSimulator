namespace Framework.Domain;

public enum ErrorType
{
    None = 0,
    Validation = 1,
    NotFound = 2,
    Conflict = 3,
    Business = 4,
    External = 5
}
