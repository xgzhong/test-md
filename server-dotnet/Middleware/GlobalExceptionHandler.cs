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

    private static async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";

        var (statusCode, code, message) = exception switch
        {
            ArgumentException argEx => (HttpStatusCode.BadRequest, "VALIDATION_ERROR", argEx.Message),
            KeyNotFoundException notFoundEx => (HttpStatusCode.NotFound, "NOT_FOUND", notFoundEx.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, "UNAUTHORIZED", "未授权"),
            Microsoft.EntityFrameworkCore.DbUpdateException => (HttpStatusCode.BadRequest, "DATABASE_ERROR", "数据库操作失败"),
            _ => (HttpStatusCode.InternalServerError, "INTERNAL_ERROR", "服务器内部错误")
        };

        context.Response.StatusCode = (int)statusCode;

        var response = new { error = new { code, message } };
        await context.Response.WriteAsJsonAsync(response);
    }
}
