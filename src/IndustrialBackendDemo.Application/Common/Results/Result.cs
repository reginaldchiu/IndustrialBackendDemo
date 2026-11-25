namespace IndustrialBackendDemo.Application.Common.Results;

public class Result
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public ApplicationError Error { get; }

    protected Result(bool isSuccess, ApplicationError error)
    {
        if (isSuccess && error != ApplicationError.None)
        {
            throw new ArgumentException(
                "A successful result cannot contain an error.",
                nameof(error)
            );
        }

        if (!isSuccess && error == ApplicationError.None)
        {
            throw new ArgumentException("A failed result must contain an error.", nameof(error));
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public static Result Success()
    {
        return new Result(true, ApplicationError.None);
    }

    public static Result Failure(ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result(false, error);
    }
}

#pragma warning disable CA1000

public sealed class Result<T> : Result
{
    private readonly T? _value;

    public T Value =>
        IsSuccess
            ? _value!
            : throw new InvalidOperationException("Cannot access the value of a failed result.");

    private Result(bool isSuccess, T? value, ApplicationError error)
        : base(isSuccess, error)
    {
        if (isSuccess && value is null)
        {
            throw new ArgumentNullException(
                nameof(value),
                "A successful result must contain a value."
            );
        }

        _value = value;
    }

    public static Result<T> Success(T value)
    {
        return new Result<T>(true, value, ApplicationError.None);
    }

    public static new Result<T> Failure(ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        return new Result<T>(false, default, error);
    }
}

#pragma warning restore CA1000
