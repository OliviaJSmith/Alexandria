namespace Alexandria.API.Services;

public class ServiceResult<T>
{
    public T? Data { get; }
    public string? Error { get; }
    public bool IsSuccess => Error is null;

    private ServiceResult(T? data, string? error)
    {
        Data = data;
        Error = error;
    }

    public static ServiceResult<T> Success(T data) => new(data, null);
    public static ServiceResult<T> Failure(string error) => new(default, error);
}

public class ServiceResult
{
    public string? Error { get; }
    public bool IsSuccess => Error is null;

    private ServiceResult(string? error)
    {
        Error = error;
    }

    public static ServiceResult Success() => new(null);
    public static ServiceResult Failure(string error) => new(error);
}
