using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server_dotnet.Data;
using server_dotnet.DTOs;
using Yitter.IdGenerator;

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
            .OrderByDescending(f => f.IsPinned)
            .ThenBy(f => f.SortOrder)
            .ThenBy(f => f.CreatedAt)
            .ToListAsync();

        var foldersWithCount = new List<FolderDto>();
        foreach (var folder in folders)
        {
            var noteCount = await _context.Notes
                .CountAsync(n => n.FolderId == folder.Id && n.UserId == userId && !n.IsDeleted);
            foldersWithCount.Add(new FolderDto(folder.Id, folder.Name, noteCount, folder.SortOrder, folder.IsPinned));
        }

        var uncategorizedCount = await _context.Notes
            .CountAsync(n => n.FolderId == null && n.UserId == userId && !n.IsDeleted);

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
            Id = YitIdHelper.NextId(),
            UserId = userId.Value,
            Name = request.Name,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId.Value,
            UpdatedBy = userId.Value
        };

        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        return StatusCode(201, new FolderResponse("分类创建成功", new FolderDto(folder.Id, folder.Name, 0, folder.SortOrder, folder.IsPinned)));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<FolderResponse>> UpdateFolder(long id, [FromBody] UpdateFolderRequest request)
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

        folder.UpdatedAt = DateTimeOffset.UtcNow;
        folder.UpdatedBy = userId.Value;

        await _context.SaveChangesAsync();

        var noteCount = await _context.Notes.CountAsync(n => n.FolderId == folder.Id && n.UserId == userId && !n.IsDeleted);

        return Ok(new FolderResponse("分类更新成功", new FolderDto(folder.Id, folder.Name, noteCount, folder.SortOrder, folder.IsPinned)));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteFolder(long id)
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

    [HttpPut("reorder")]
    public async Task<ActionResult> ReorderFolders([FromBody] List<long> folderIds)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        for (int i = 0; i < folderIds.Count; i++)
        {
            var folder = await _context.Folders.FindAsync(folderIds[i]);
            if (folder != null && folder.UserId == userId)
            {
                folder.SortOrder = i;
            }
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "排序更新成功" });
    }

    [HttpPut("{id}/pin")]
    public async Task<ActionResult<FolderResponse>> TogglePin(long id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var folder = await _context.Folders.FindAsync(id);
        if (folder == null || folder.UserId != userId)
        {
            return NotFound(new { error = "分类不存在" });
        }

        folder.IsPinned = !folder.IsPinned;
        await _context.SaveChangesAsync();

        var noteCount = await _context.Notes.CountAsync(n => n.FolderId == folder.Id && n.UserId == userId && !n.IsDeleted);

        var message = folder.IsPinned ? "置顶成功" : "取消置顶成功";
        return Ok(new FolderResponse(message, new FolderDto(folder.Id, folder.Name, noteCount, folder.SortOrder, folder.IsPinned)));
    }

    private long? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim != null && long.TryParse(userIdClaim, out var userId))
        {
            return userId;
        }
        return null;
    }
}
