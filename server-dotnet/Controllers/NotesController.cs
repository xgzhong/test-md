using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server_dotnet.Data;
using server_dotnet.DTOs;

namespace server_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : ControllerBase
{
    private readonly AppDbContext _context;

    public NotesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<NotesResponse>> GetNotes([FromQuery] int? folderId, [FromQuery] string? search)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var query = _context.Notes
            .Where(n => n.UserId == userId)
            .Include(n => n.Folder)
            .AsQueryable();

        if (folderId.HasValue)
        {
            query = query.Where(n => n.FolderId == (folderId == -1 ? null : folderId));
        }

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(n => n.Title.Contains(search) || n.Content.Contains(search));
        }

        var notes = await query
            .OrderByDescending(n => n.UpdatedAt)
            .Select(n => new NoteDto(
                n.Id,
                n.FolderId,
                n.Folder != null ? n.Folder.Name : null,
                n.Title,
                n.Content,
                n.IsShared,
                n.ShareToken,
                n.CreatedAt,
                n.UpdatedAt
            ))
            .ToListAsync();

        return Ok(new NotesResponse(notes));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NoteResponse>> GetNote(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var note = await _context.Notes
            .Where(n => n.Id == id && n.UserId == userId)
            .Include(n => n.Folder)
            .FirstOrDefaultAsync();

        if (note == null)
        {
            return NotFound(new { error = "笔记不存在" });
        }

        return Ok(new NoteResponse("获取成功", new NoteDto(
            note.Id,
            note.FolderId,
            note.Folder?.Name,
            note.Title,
            note.Content,
            note.IsShared,
            note.ShareToken,
            note.CreatedAt,
            note.UpdatedAt
        )));
    }

    [HttpPost]
    public async Task<ActionResult<NoteResponse>> CreateNote([FromBody] CreateNoteRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var note = new server_dotnet.Models.Note
        {
            UserId = userId.Value,
            Title = request.Title ?? "无标题笔记",
            Content = request.Content ?? "",
            FolderId = request.FolderId
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        // Create initial version
        var version = new server_dotnet.Models.NoteVersion
        {
            NoteId = note.Id,
            Title = note.Title,
            Content = note.Content
        };
        _context.NoteVersions.Add(version);
        await _context.SaveChangesAsync();

        return StatusCode(201, new NoteResponse("笔记创建成功", new NoteDto(
            note.Id,
            note.FolderId,
            null,
            note.Title,
            note.Content,
            note.IsShared,
            note.ShareToken,
            note.CreatedAt,
            note.UpdatedAt
        )));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<NoteResponse>> UpdateNote(int id, [FromBody] UpdateNoteRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var note = await _context.Notes.FindAsync(id);
        if (note == null || note.UserId != userId)
        {
            return NotFound(new { error = "笔记不存在" });
        }

        // Save version if content changed
        if ((request.Content != null && request.Content != note.Content) || (request.Title != null && request.Title != note.Title))
        {
            var version = new server_dotnet.Models.NoteVersion
            {
                NoteId = note.Id,
                Title = note.Title,
                Content = note.Content
            };
            _context.NoteVersions.Add(version);
        }

        if (request.Title != null) note.Title = request.Title;
        if (request.Content != null) note.Content = request.Content;
        if (request.FolderId != null) note.FolderId = request.FolderId == -1 ? null : request.FolderId;

        await _context.SaveChangesAsync();

        var folder = note.FolderId.HasValue ? await _context.Folders.FindAsync(note.FolderId) : null;

        return Ok(new NoteResponse("笔记更新成功", new NoteDto(
            note.Id,
            note.FolderId,
            folder?.Name,
            note.Title,
            note.Content,
            note.IsShared,
            note.ShareToken,
            note.CreatedAt,
            note.UpdatedAt
        )));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNote(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var note = await _context.Notes.FindAsync(id);
        if (note == null || note.UserId != userId)
        {
            return NotFound(new { error = "笔记不存在" });
        }

        // Delete versions
        var versions = await _context.NoteVersions.Where(v => v.NoteId == id).ToListAsync();
        _context.NoteVersions.RemoveRange(versions);

        _context.Notes.Remove(note);
        await _context.SaveChangesAsync();

        return Ok(new { message = "笔记删除成功" });
    }

    [HttpPost("{id}/share")]
    public async Task<ActionResult<ShareResponse>> ShareNote(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var note = await _context.Notes.FindAsync(id);
        if (note == null || note.UserId != userId)
        {
            return NotFound(new { error = "笔记不存在" });
        }

        note.IsShared = true;
        note.ShareToken = Guid.NewGuid().ToString();
        await _context.SaveChangesAsync();

        return Ok(new ShareResponse("分享成功", $"/shared/{note.ShareToken}"));
    }

    [HttpPost("{id}/unshare")]
    public async Task<ActionResult> UnshareNote(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var note = await _context.Notes.FindAsync(id);
        if (note == null || note.UserId != userId)
        {
            return NotFound(new { error = "笔记不存在" });
        }

        note.IsShared = false;
        note.ShareToken = null;
        await _context.SaveChangesAsync();

        return Ok(new { message = "取消分享成功" });
    }

    [HttpGet("{id}/versions")]
    public async Task<ActionResult<VersionsResponse>> GetVersions(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var note = await _context.Notes.FindAsync(id);
        if (note == null || note.UserId != userId)
        {
            return NotFound(new { error = "笔记不存在" });
        }

        var versions = await _context.NoteVersions
            .Where(v => v.NoteId == id)
            .OrderByDescending(v => v.CreatedAt)
            .Take(50)
            .Select(v => new NoteVersionDto(v.Id, v.Title, v.Content, v.CreatedAt))
            .ToListAsync();

        return Ok(new VersionsResponse(versions));
    }

    [HttpPost("{id}/restore/{versionId}")]
    public async Task<ActionResult<NoteResponse>> RestoreVersion(int id, int versionId)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var note = await _context.Notes.FindAsync(id);
        if (note == null || note.UserId != userId)
        {
            return NotFound(new { error = "笔记不存在" });
        }

        var version = await _context.NoteVersions.FindAsync(versionId);
        if (version == null || version.NoteId != id)
        {
            return NotFound(new { error = "版本不存在" });
        }

        // Save current version
        var currentVersion = new server_dotnet.Models.NoteVersion
        {
            NoteId = note.Id,
            Title = note.Title,
            Content = note.Content
        };
        _context.NoteVersions.Add(currentVersion);

        // Restore
        note.Title = version.Title;
        note.Content = version.Content;
        await _context.SaveChangesAsync();

        var folder = note.FolderId.HasValue ? await _context.Folders.FindAsync(note.FolderId) : null;

        return Ok(new NoteResponse("恢复成功", new NoteDto(
            note.Id,
            note.FolderId,
            folder?.Name,
            note.Title,
            note.Content,
            note.IsShared,
            note.ShareToken,
            note.CreatedAt,
            note.UpdatedAt
        )));
    }

    private int? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && int.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
