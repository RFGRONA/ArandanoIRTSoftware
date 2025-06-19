namespace ArandanoIRT.Web.Common; 

public class Result
{
    public bool IsSuccess { get; }
    public string ErrorMessage { get; }
    public bool IsFailure => !IsSuccess;

    protected Result(bool isSuccess, string errorMessage)
    {
        if (isSuccess && !string.IsNullOrEmpty(errorMessage))
            throw new InvalidOperationException("A successful result cannot have an error message.");
        if (!isSuccess && string.IsNullOrEmpty(errorMessage))
            throw new InvalidOperationException("A failed result requires an error message.");

        IsSuccess = isSuccess;
        ErrorMessage = errorMessage ?? string.Empty;
    }

    public static Result Success() => new(true, string.Empty);
    public static Result Failure(string errorMessage) => new(false, errorMessage);

    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
    public static Result<T> Failure<T>(string errorMessage) => new(default, false, errorMessage); 
}

public class Result<T> : Result
{
    private readonly T? _value; 

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of a failed result. Check IsSuccess first.");

    protected internal Result(T? value, bool isSuccess, string errorMessage) : base(isSuccess, errorMessage)
    {
        _value = value;
    }
}