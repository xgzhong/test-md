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
public class FoldersController : ControllerBase
{
    private readonly AppDbContext _context;

    public FoldersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<FoldersResponse>> GetFolders()
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var folders = await _context.Folders
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.CreatedAt)
            .ToListAsync();

        var foldersWithCount = new List<FolderDto>();
        foreach (var folder in folders)
        {
            var noteCount = await _context.Notes
                .CountAsync(n => n.FolderId == folder.Id && n.UserId == userId);
            foldersWithCount.Add(new FolderDto(folder.Id, folder.Name, noteCount));
        }

        var uncategorizedCount = await _context.Notes
            .CountAsync(n => n.FolderId == null && n.UserId == userId);

        return Ok(new FoldersResponse(foldersWithCount, uncategorizedCount));
    }

    [HttpPost]
    public async Task<ActionResult<FolderResponse>> CreateFolder([FromBody] CreateFolderRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        if (string.IsNullOrEmpty(request.Name))
        {
            return BadRequest(new { error = "请输入分类名称" });
        }

        var folder = new server_dotnet.Models.Folder
        {
            UserId = userId.Value,
            Name = request.Name
        };

        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        return StatusCode(201, new FolderResponse("分类创建成功", new FolderDto(folder.Id, folder.Name, 0)));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FolderResponse>> UpdateFolder(int id, [FromBody] UpdateFolderRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var folder = await _context.Folders.FindAsync(id);
        if (folder == null || folder.UserId != userId)
        {
            return NotFound(new { error = "分类不存在" });
        }

        if (!string.IsNullOrEmpty(request.Name))
        {
            folder.Name = request.Name;
        }

        await _context.SaveChangesAsync();

        var noteCount = await _context.Notes.CountAsync(n => n.FolderId == folder.Id && n.UserId == userId);

        return Ok(new FolderResponse("分类更新成功", new FolderDto(folder.Id, folder.Name, noteCount)));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteFolder(int id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var folder = await _context.Folders.FindAsync(id);
        if (folder == null || folder.UserId != userId)
        {
            return NotFound(new { error = "分类不存在" });
        }

        // Move notes to uncategorized
        var notes = await _context.Notes.Where(n => n.FolderId == folder.Id).ToListAsync();
        foreach (var note in notes)
        {
            note.FolderId = null;
        }

        _context.Folders.Remove(folder);
        await _context.SaveChangesAsync();

        return Ok(new { message = "分类删除成功" });
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
