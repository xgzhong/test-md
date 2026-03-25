namespace server_dotnet.DTOs;

public record ApiError(string Code, string Message)
{
    public ApiError(string message) : this("ERROR", message) { }
}

public record ApiSuccess<T>(T Data, string? Message = null)
{
    public ApiSuccess(T data) : this(data, null) { }
}
