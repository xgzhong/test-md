using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server_dotnet.Data;
using server_dotnet.DTOs;

namespace server_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SharedController : ControllerBase
{
    private readonly AppDbContext _context;

    public SharedController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("{token}")]
    public async Task<ActionResult<SharedNoteResponse>> GetSharedNote(string token)
    {
        var note = await _context.Notes
            .Where(n => n.ShareToken == token && n.IsShared)
            .Include(n => n.User)
            .FirstOrDefaultAsync();

        if (note == null)
        {
            return NotFound(new { error = "笔记不存在或未分享" });
        }

        return Ok(new SharedNoteResponse(new SharedNoteDto(
            note.Id,
            note.Title,
            note.Content,
            note.User?.Username ?? "未知",
            note.CreatedAt,
            note.UpdatedAt
        )));
    }
}
