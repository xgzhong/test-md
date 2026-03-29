using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server_dotnet.Common.Result;
using server_dotnet.Data;
using server_dotnet.DTOs;
using Yitter.IdGenerator;

namespace server_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class FoldersController : BaseController
{
    private readonly AppDbContext _context;

    public FoldersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetFolders()
    {
        var userId = GetUserId();
        if (userId == null) return ReturnResult(Result.Unauthorized());

        var folders = await _context.Folders
            .Where(f => f.UserId == userId)
            .OrderByDescending(f => f.IsPinned)
            .ThenBy(f => f.SortOrder)
            .ThenBy(f => f.CreatedAt)
            .ToListAsync();

        // 优化：一次查询获取所有文件夹的笔记数量，避免 N+1 问题
        var folderIds = folders.Select(f => f.Id).ToList();
        var noteCounts = await _context.Notes
            .Where(n => n.UserId == userId && !n.IsDeleted && n.FolderId != null && folderIds.Contains(n.FolderId.Value))
            .GroupBy(n => n.FolderId)
            .Select(g => new { FolderId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FolderId!.Value, x => x.Count);

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

        return ReturnResult(Result.Success(new FoldersResponse(roots, uncategorizedCount), "获取成功"));
    }

    [HttpPost]
    public async Task<IActionResult> CreateFolder([FromBody] CreateFolderRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return ReturnResult(Result.Unauthorized());

        if (string.IsNullOrEmpty(request.Name))
        {
            return ReturnResult(Result.Invalid("请输入分类名称"));
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

        return ReturnResult(Result.Success(new FolderDto(folder.Id, folder.Name, 0, folder.SortOrder, folder.IsPinned, folder.ParentId), "分类创建成功"));
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateFolder(long id, [FromBody] UpdateFolderRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return ReturnResult(Result.Unauthorized());

        var folder = await _context.Folders.FindAsync(id);
        if (folder == null || folder.UserId != userId)
        {
            return ReturnResult(Result.NotFound("分类不存在"));
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
                    return ReturnResult(Result.Invalid("不能将分类设为自己"));
                }

                // 检查是否形成循环引用
                var allFolders = await _context.Folders.Where(f => f.UserId == userId).ToListAsync();
                var visited = new HashSet<long>();
                var currentId = newParentId;
                while (currentId != 0)
                {
                    if (visited.Contains(currentId))
                    {
                        return ReturnResult(Result.Invalid("不能将分类设为子分类的父分类"));
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

        return ReturnResult(Result.Success(new FolderDto(folder.Id, folder.Name, noteCount, folder.SortOrder, folder.IsPinned, folder.ParentId), "分类更新成功"));
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteFolder(long id)
    {
        var userId = GetUserId();
        if (userId == null) return ReturnResult(Result.Unauthorized());

        var folder = await _context.Folders.FindAsync(id);
        if (folder == null || folder.UserId != userId)
        {
            return ReturnResult(Result.NotFound("分类不存在"));
        }

        // 优化：一次性获取所有文件夹，避免递归 N+1 查询
        var allFolders = await _context.Folders.Where(f => f.UserId == userId).ToListAsync();
        var foldersToDelete = new List<server_dotnet.Models.Folder> { folder };

        // 递归收集所有子文件夹（内存中处理）
        void CollectChildren(long parentId)
        {
            var children = allFolders.Where(f => f.ParentId == parentId).ToList();
            foreach (var child in children)
            {
                foldersToDelete.Add(child);
                CollectChildren(child.Id);
            }
        }

        CollectChildren(folder.Id);

        var folderIdsToDelete = foldersToDelete.Select(f => f.Id).ToHashSet();

        // Move notes to uncategorized
        var notes = await _context.Notes
            .Where(n => n.FolderId != null && folderIdsToDelete.Contains(n.FolderId.Value))
            .ToListAsync();
        foreach (var note in notes)
        {
            note.FolderId = null;
        }

        _context.Folders.RemoveRange(foldersToDelete);
        await _context.SaveChangesAsync();

        return ReturnResult(Result.Success("分类删除成功"));
    }

    [HttpPut("reorder")]
    public async Task<IActionResult> ReorderFolders([FromBody] List<long> folderIds)
    {
        var userId = GetUserId();
        if (userId == null) return ReturnResult(Result.Unauthorized());

        // 验证所有文件夹都存在且属于当前用户
        var folders = await _context.Folders
            .Where(f => folderIds.Contains(f.Id) && f.UserId == userId)
            .ToListAsync();

        if (folders.Count != folderIds.Distinct().Count())
        {
            return ReturnResult(Result.Forbidden("存在无权操作的分类"));
        }

        for (int i = 0; i < folderIds.Count; i++)
        {
            var folder = folders.First(f => f.Id == folderIds[i]);
            folder.SortOrder = i;
        }

        await _context.SaveChangesAsync();

        return ReturnResult(Result.Success("排序更新成功"));
    }

    [HttpPut("{id}/pin")]
    public async Task<IActionResult> TogglePin(long id)
    {
        var userId = GetUserId();
        if (userId == null) return ReturnResult(Result.Unauthorized());

        var folder = await _context.Folders.FindAsync(id);
        if (folder == null || folder.UserId != userId)
        {
            return ReturnResult(Result.NotFound("分类不存在"));
        }

        folder.IsPinned = !folder.IsPinned;
        await _context.SaveChangesAsync();

        var noteCount = await _context.Notes.CountAsync(n => n.FolderId == folder.Id && n.UserId == userId && !n.IsDeleted);

        var message = folder.IsPinned ? "置顶成功" : "取消置顶成功";
        return ReturnResult(Result.Success(new FolderDto(folder.Id, folder.Name, noteCount, folder.SortOrder, folder.IsPinned, folder.ParentId), message));
    }
}
