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

        // 优化：一次查询获取所有文件夹的笔记数量，避免 N+1 问题
        var folderIds = folders.Select(f => f.Id).ToList();
        var noteCounts = await _context.Notes
            .Where(n => folderIds.Contains(n.FolderId ?? 0) && n.UserId == userId && !n.IsDeleted)
            .GroupBy(n => n.FolderId)
            .Select(g => new { FolderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FolderId ?? 0, x => x.Count);

        var foldersWithCount = folders.Select(folder => new FolderDto(
            folder.Id,
            folder.Name,
            noteCounts.GetValueOrDefault(folder.Id, 0),
            folder.SortOrder,
            folder.IsPinned,
            folder.ParentId
        )).ToList();

        // 构建层级结构 - 使用递归方式
        var folderById = foldersWithCount.ToDictionary(f => f.Id);

        // 递归构建带子节点的文件夹
        FolderDto BuildWithChildren(FolderDto folder)
        {
            var children = folderById.Values
                .Where(f => f.ParentId == folder.Id)
                .Select(BuildWithChildren)
                .ToList();

            return folder with { Children = children };
        }

        // 获取根文件夹（ParentId 为 0 表示顶级分类）
        var roots = foldersWithCount
            .Where(f => f.ParentId == 0)
            .Select(BuildWithChildren)
            .ToList();

        var uncategorizedCount = await _context.Notes
            .CountAsync(n => n.FolderId == null && n.UserId == userId && !n.IsDeleted);

        return Ok(new FoldersResponse(roots, uncategorizedCount));
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
            ParentId = string.IsNullOrEmpty(request.ParentId) ? 0 : long.TryParse(request.ParentId, out var pid) ? pid : 0,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId.Value,
            UpdatedBy = userId.Value
        };

        _context.Folders.Add(folder);
        await _context.SaveChangesAsync();

        return StatusCode(201, new FolderResponse("分类创建成功", new FolderDto(folder.Id, folder.Name, 0, folder.SortOrder, folder.IsPinned, folder.ParentId)));
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

        // 更新父分类（允许设置为 '0' 来移除父级）
        if (request.ParentId != null)
        {
            var parentIdStr = request.ParentId;
            var newParentId = long.TryParse(parentIdStr, out var pid) ? pid : 0;

            // 如果是 0，表示移除父级（设为顶级分类）
            if (newParentId == 0)
            {
                folder.ParentId = 0;
            }
            else
            {
                // 禁止将分类设为自己的子分类
                if (newParentId == folder.Id)
                {
                    return BadRequest(new { error = "不能将分类设为自己" });
                }

                // 检查是否形成循环引用
                var allFolders = await _context.Folders.Where(f => f.UserId == userId).ToListAsync();
                var visited = new HashSet<long>();
                var currentId = newParentId;
                while (currentId != 0)
                {
                    if (visited.Contains(currentId))
                    {
                        return BadRequest(new { error = "不能将分类设为子分类的父分类" });
                    }
                    visited.Add(currentId);
                    var parent = allFolders.FirstOrDefault(f => f.Id == currentId);
                    if (parent == null) break;
                    currentId = parent.ParentId;
                }

                folder.ParentId = newParentId;
            }
        }

        folder.UpdatedAt = DateTimeOffset.UtcNow;
        folder.UpdatedBy = userId.Value;

        await _context.SaveChangesAsync();

        var noteCount = await _context.Notes.CountAsync(n => n.FolderId == folder.Id && n.UserId == userId && !n.IsDeleted);

        return Ok(new FolderResponse("分类更新成功", new FolderDto(folder.Id, folder.Name, noteCount, folder.SortOrder, folder.IsPinned, folder.ParentId)));
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

        // Get all child folders recursively
        async Task<List<server_dotnet.Models.Folder>> GetAllChildren(long parentId)
        {
            var children = await _context.Folders.Where(f => f.ParentId == parentId).ToListAsync();
            var result = new List<server_dotnet.Models.Folder>(children);
            foreach (var child in children)
            {
                result.AddRange(await GetAllChildren(child.Id));
            }
            return result;
        }

        var allFoldersToDelete = new List<server_dotnet.Models.Folder> { folder };
        allFoldersToDelete.AddRange(await GetAllChildren(folder.Id));

        var folderIdsToDelete = allFoldersToDelete.Select(f => f.Id).ToHashSet();

        // Move notes to uncategorized
        var notes = await _context.Notes.Where(n => folderIdsToDelete.Contains(n.FolderId ?? 0)).ToListAsync();
        foreach (var note in notes)
        {
            note.FolderId = null;
        }

        _context.Folders.RemoveRange(allFoldersToDelete);
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
        return Ok(new FolderResponse(message, new FolderDto(folder.Id, folder.Name, noteCount, folder.SortOrder, folder.IsPinned, folder.ParentId)));
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
