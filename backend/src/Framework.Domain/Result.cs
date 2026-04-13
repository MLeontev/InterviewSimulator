namespace Framework.Domain;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public Error Error { get; }

    public Result(bool isSuccess, Error error)
    {
        if (isSuccess && error != Error.None
            || !isSuccess && error == Error.None)
            throw new ArgumentException("Invalid error", nameof(error));
        
        IsSuccess = isSuccess;
        Error = error;
    }
    
    public static Result Success() => new(true, Error.None);
    public static Result Failure(Error error) => new(false, error);
    
    public static Result<T> Success<T>(T value) => new(value, true, Error.None);
    public static Result<T> Failure<T>(Error error) => new(default, false, error);
}

public class Result<TValue> : Result
{
    private readonly TValue? _value;
    public TValue Value => IsSuccess 
        ? _value! 
        : throw new InvalidOperationException("Cannot access the value of a failed result");

    public Result(TValue? value, bool isSuccess, Error error) 
        : base(isSuccess, error)
    {
        if (value == null && isSuccess)
            throw new ArgumentException("Successful result cannot have null value");
        
        _value = value;
    }

    public static implicit operator Result<TValue>(TValue? value)
    {
        return value == null 
            ? throw new ArgumentException("Successful result cannot have null value") 
            : Success(value);
    }
}