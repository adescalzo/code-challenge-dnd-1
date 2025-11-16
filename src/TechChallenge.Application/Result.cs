namespace TechChallenge.Application;

public enum ErrorDefinition
{
    None,
    Error
}

public sealed record ErrorResult(
    string Code,
    ErrorDefinition Definition,
    string Description,
    Exception? Exception = null)
{
    public static readonly ErrorResult None = new(nameof(ErrorDefinition.None), ErrorDefinition.None, string.Empty);

    public static ErrorResult Error(string description, Exception? exception = null) => new(
        nameof(ErrorDefinition.Error),
        ErrorDefinition.Error,
        description,
        exception);

    public static ErrorResult Error(string code, string description, Exception? exception = null) => new(
        code,
        ErrorDefinition.Error,
        description,
        exception);
}

public class Result
{
    protected Result(ErrorResult error)
    {
        ArgumentNullException.ThrowIfNull(error);

        IsSuccess = error.Definition == ErrorDefinition.None;
        IsFailure = !IsSuccess;
        Error = error;
        Identifier = Guid.NewGuid().ToString();
    }

    public bool IsSuccess { get; }
    public bool IsFailure { get; }
    public ErrorResult Error { get; }
    public string Identifier { get; }

    public static Result Success() => new(ErrorResult.None);

    public static Result<T> Success<T>(T value) => Result<T>.CreateSuccess(value);

    public static Result Failure(ErrorResult error) => new(error);

    public static Result<T> Failure<T>(ErrorResult error, T? value = default)
        => Result<T>.CreateFailure(error, value);
}

public class Result<T> : Result
{
    private Result(ErrorResult error, T? value) : base(error)
    {
        Value = value;
    }

    public T? Value { get; }
    public T GetValue => Value ?? throw new InvalidOperationException("Value is null");
    public bool HasValue => Value is not null;

    internal static Result<T> CreateSuccess(T value) => new(ErrorResult.None, value);

    internal static Result<T> CreateFailure(ErrorResult error, T? value) => new(error, value);
}
