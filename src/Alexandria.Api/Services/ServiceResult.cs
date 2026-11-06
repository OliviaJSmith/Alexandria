namespace Alexandria.Api.Services;

public enum ServiceErrorType
{
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
}

public sealed record ServiceError(ServiceErrorType Type, string Message);

/// <summary>Outcome of a service call: either a value or an error the controller maps to an HTTP status.</summary>
public sealed class ServiceResult<T>
{
    private ServiceResult(T? value, ServiceError? error)
    {
        Value = value;
        Error = error;
    }

    public T? Value { get; }

    public ServiceError? Error { get; }

    public bool IsSuccess => Error is null;

    public static ServiceResult<T> Success(T value) => new(value, null);

    public static ServiceResult<T> Failure(ServiceErrorType type, string message) => new(default, new ServiceError(type, message));
}
