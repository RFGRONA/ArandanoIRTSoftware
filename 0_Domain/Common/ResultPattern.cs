namespace ArandanoIRT.Web._0_Domain.Common;

/// <summary>
/// Representa el resultado de una operación, que puede ser exitosa o fallida.
/// Este patrón se utiliza para evitar el uso de excepciones para el control de flujo.
/// </summary>
public class Result
{
    /// <summary>
    /// Obtiene un valor que indica si la operación fue exitosa.
    /// </summary>
    public bool IsSuccess { get; }
    /// <summary>
    /// Obtiene el mensaje de error si la operación falló.
    /// </summary>
    public string ErrorMessage { get; }
    /// <summary>
    /// Obtiene un valor que indica si la operación falló.
    /// </summary>
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

    /// <summary>
    /// Crea una instancia de un resultado exitoso.
    /// </summary>
    public static Result Success() => new(true, string.Empty);
    /// <summary>
    /// Crea una instancia de un resultado fallido con un mensaje de error.
    /// </summary>
    public static Result Failure(string errorMessage) => new(false, errorMessage);

    /// <summary>
    /// Crea una instancia de un resultado exitoso con un valor.
    /// </summary>
    public static Result<T> Success<T>(T value) => new(value, true, string.Empty);
    /// <summary>
    /// Crea una instancia de un resultado fallido para una operación que debería devolver un valor.
    /// </summary>
    public static Result<T> Failure<T>(string errorMessage) => new(default, false, errorMessage);
}

/// <summary>
/// Representa el resultado de una operación que devuelve un valor en caso de éxito.
/// </summary>
/// <typeparam name="T">El tipo del valor devuelto.</typeparam>
public class Result<T> : Result
{
    private readonly T? _value;

    public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Cannot access value of a failed result. Check IsSuccess first.");

    protected internal Result(T? value, bool isSuccess, string errorMessage) : base(isSuccess, errorMessage)
    {
        _value = value;
    }
}