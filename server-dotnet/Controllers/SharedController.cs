using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using server_dotnet.Common.Result;
using server_dotnet.Data;
using server_dotnet.DTOs;
using System.Collections.Concurrent;
using System.Security.Cryptography;

namespace server_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("shared")]
public class SharedController : BaseController
{
    private readonly AppDbContext _context;

    // In-memory session storage for shared note access (simple implementation)
    private static readonly ConcurrentDictionary<string, SharedSession> _sessions = new();

    public SharedController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{token}")]
    public async Task<IActionResult> GetSharedNote(string token)
    {
        var note = await _context.Notes
            .Where(n => n.ShareToken == token && n.IsShared)
            .Include(n => n.User)
            .FirstOrDefaultAsync();

        if (note == null)
        {
            return ReturnResult(Result.NotFound("笔记不存在"));
        }

        return ReturnResult(Result.Success(new SharedNoteDto(
            note.Id,
            note.Title,
            note.Content,
            note.User?.Username ?? "未知",
            note.CreatedAt,
            note.UpdatedAt
        )));
    }

    /// <summary>
    /// 使用 POST 方式交换分享 token，获取 session cookie，然后重定向到干净 URL
    /// 解决 token 在 URL 中暴露的安全问题
    /// </summary>
    [HttpPost("access")]
    public async Task<IActionResult> AccessSharedNote([FromBody] AccessSharedRequest request)
    {
        if (string.IsNullOrEmpty(request.Token))
        {
            return ReturnResult(Result.Invalid("Token 不能为空"));
        }

        var note = await _context.Notes
            .Where(n => n.ShareToken == request.Token && n.IsShared)
            .Include(n => n.User)
            .FirstOrDefaultAsync();

        if (note == null)
        {
            return ReturnResult(Result.NotFound("笔记不存在或未分享"));
        }

        // 生成安全的 session ID
        var sessionId = GenerateSecureToken();
        var expiry = DateTime.UtcNow.AddHours(1);

        // 存储 session
        _sessions[sessionId] = new SharedSession(note.Id, sessionId, expiry);

        // 设置 HttpOnly cookie
        Response.Cookies.Append("shared_note", sessionId, new CookieOptions
        {
            HttpOnly = true,
            Secure = true,
            SameSite = SameSiteMode.Strict,
            Expires = expiry
        });

        // 返回 session ID 和跳转 URL
        return Ok(new AccessSharedResponse($"/shared/view/{note.Id}", sessionId));
    }

    /// <summary>
    /// 通过 session cookie 获取分享笔记（重定向后的页面使用）
    /// </summary>
    [HttpGet("view/{noteId}")]
    public async Task<IActionResult> GetSharedNoteBySession(long noteId)
    {
        if (!Request.Cookies.TryGetValue("shared_note", out var sessionId) || string.IsNullOrEmpty(sessionId))
        {
            return ReturnResult(Result.Unauthorized("请通过分享链接访问"));
        }

        if (!_sessions.TryGetValue(sessionId, out var session) || session.NoteId != noteId || session.ExpiresAt < DateTime.UtcNow)
        {
            // Session 无效或已过期
            Response.Cookies.Delete("shared_note");
            return ReturnResult(Result.Unauthorized("会话已过期，请重新访问分享链接"));
        }

        var note = await _context.Notes
            .Where(n => n.Id == noteId && n.IsShared)
            .Include(n => n.User)
            .FirstOrDefaultAsync();

        if (note == null)
        {
            return ReturnResult(Result.NotFound("笔记不存在"));
        }

        return ReturnResult(Result.Success(new SharedNoteDto(
            note.Id,
            note.Title,
            note.Content,
            note.User?.Username ?? "未知",
            note.CreatedAt,
            note.UpdatedAt
        )));
    }

    /// <summary>
    /// 清除分享 session
    /// </summary>
    [HttpPost("logout")]
    public IActionResult LogoutShared()
    {
        if (Request.Cookies.TryGetValue("shared_note", out var sessionId) && !string.IsNullOrEmpty(sessionId))
        {
            _sessions.TryRemove(sessionId, out _);
        }
        Response.Cookies.Delete("shared_note");
        return Ok(new { message = "已退出分享阅读" });
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}

public record AccessSharedRequest(string Token);

public record AccessSharedResponse(string redirectUrl, string sessionId);

public record SharedSession(long NoteId, string SessionId, DateTime ExpiresAt);
