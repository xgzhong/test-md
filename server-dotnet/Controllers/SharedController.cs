using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using server_dotnet.Common.Result;
using server_dotnet.Data;
using server_dotnet.DTOs;

namespace server_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[EnableRateLimiting("shared")]
public class SharedController : BaseController
{
    private readonly AppDbContext _context;

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
}
