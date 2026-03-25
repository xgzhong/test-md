using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;

namespace server_dotnet.Controllers;

[ApiController]
public abstract class BaseController : ControllerBase
{
    protected long? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && long.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }

    protected IActionResult? UnauthorizedIfNull(long? userId, string message = "请先登录")
    {
        if (userId == null)
        {
            return Unauthorized(new { error = message });
        }
        return null;
    }
}
