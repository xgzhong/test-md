using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using server_dotnet.Common.Result;
using IResult = server_dotnet.Common.Result.IResult;

namespace server_dotnet.Controllers;

/// <summary>
/// 业务控制器基类，提供统一的返回值处理
/// </summary>
[Route("api/[controller]")]
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

    /// <summary>
    /// 根据结果返回对应的HTTP响应
    /// </summary>
    [NonAction]
    protected IActionResult ReturnResult(IResult result)
    {
        switch (result.Status)
        {
            case ResultStatus.Ok:
                {
                    var value = result.GetValue();
                    if (value is null)
                        return NoContent();
                    return Ok(value);
                }
            case ResultStatus.Error:
                return result.Errors is null
                    ? BadRequest(result.Message ?? "操作失败")
                    : BadRequest(result.Errors.Prepend(result.Message ?? "操作失败").ToArray());

            case ResultStatus.NotFound:
                return result.Errors is null
                    ? NotFound(result.Message ?? "资源不存在")
                    : NotFound(result.Errors.Prepend(result.Message ?? "资源不存在").ToArray());

            case ResultStatus.Invalid:
                return result.Errors is null
                    ? BadRequest(result.Message ?? "操作无效")
                    : BadRequest(result.Errors.Prepend(result.Message ?? "操作无效").ToArray());

            case ResultStatus.Forbidden:
                return StatusCode(403, result.Message ?? "拒绝访问");

            case ResultStatus.Unauthorized:
                {
                    var msg = result.Message ?? "请先登录";
                    return Unauthorized(msg);
                }

            default:
                return BadRequest(result.Errors?.ToArray() ?? new[] { result.Message ?? "未知错误" });
        }
    }
}
