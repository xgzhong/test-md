using System.Net;
using System.Text.Json;

namespace server_dotnet.Middleware;

public class GlobalExceptionHandler
{
    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(RequestDelegate next, ILogger<GlobalExceptionHandler> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception occurred");
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        string code;
        string message;
        HttpStatusCode statusCode;

        switch (exception)
        {
            case ArgumentException argEx:
                statusCode = HttpStatusCode.BadRequest;
                code = "VALIDATION_ERROR";
                message = argEx.Message;
                break;
            case KeyNotFoundException notFoundEx:
                statusCode = HttpStatusCode.NotFound;
                code = "NOT_FOUND";
                message = notFoundEx.Message;
                break;
            case UnauthorizedAccessException:
                statusCode = HttpStatusCode.Unauthorized;
                code = "UNAUTHORIZED";
                message = "未授权";
                break;
            case Microsoft.EntityFrameworkCore.DbUpdateException dbEx:
                statusCode = HttpStatusCode.BadRequest;
                code = "DATABASE_ERROR";
                message = "数据库操作失败";
                // 记录详细错误信息用于调试
                _logger.LogError(dbEx, "Database error: {InnerMessage}", dbEx.InnerException?.Message ?? "No inner exception");
                break;
            default:
                statusCode = HttpStatusCode.InternalServerError;
                code = "INTERNAL_ERROR";
                message = "服务器内部错误";
                break;
        }

        context.Response.StatusCode = (int)statusCode;

        var response = new { error = new { code, message } };
        await context.Response.WriteAsJsonAsync(response);
    }
}
