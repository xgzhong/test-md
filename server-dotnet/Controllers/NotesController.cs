using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using server_dotnet.Constants;
using server_dotnet.Data;
using server_dotnet.DTOs;
using Yitter.IdGenerator;

namespace server_dotnet.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotesController : BaseController
{
    private readonly AppDbContext _context;

    public NotesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<NotesResponse>> GetNotes([FromQuery] string? folderId, [FromQuery] string? search)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        // 处理 folderId 字符串参数
        long? parsedFolderId = null;
        if (!string.IsNullOrEmpty(folderId))
        {
            if (folderId == "null")
            {
                parsedFolderId = null; // 未分类
            }
            else if (long.TryParse(folderId, out long fid))
            {
                parsedFolderId = fid;
            }
        }

        var query = _context.Notes
            .Where(n => n.UserId == userId && !n.IsDeleted)
            .Include(n => n.Folder)
            .AsQueryable();

        if (parsedFolderId.HasValue)
        {
            query = query.Where(n => n.FolderId == parsedFolderId);
        }
        else if (folderId == "null")
        {
            // 显示未分类笔记（FolderId 为 null）
            query = query.Where(n => n.FolderId == null);
        }

        if (!string.IsNullOrEmpty(search) && search.Length <= 100)
        {
            query = query.Where(n => n.Title.Contains(search) || n.Content.Contains(search));
        }

        // 优化：一次查询获取笔记和总数
        var totalCount = await query.CountAsync();

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
                n.Version,
                n.CreatedAt,
                n.UpdatedAt
            ))
            .ToListAsync();

        return Ok(new NotesResponse(notes, totalCount));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<NoteResponse>> GetNote(long id)
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
            note.Version,
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
            Id = YitIdHelper.NextId(),
            UserId = userId.Value,
            Title = request.Title ?? "无标题笔记",
            Content = request.Content ?? "",
            FolderId = request.FolderId,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId.Value,
            UpdatedBy = userId.Value,
            Version = YitIdHelper.NextId()
        };

        _context.Notes.Add(note);
        await _context.SaveChangesAsync();

        // Create initial version
        var noteVersion = new server_dotnet.Models.NoteVersion
        {
            Id = YitIdHelper.NextId(),
            NoteId = note.Id,
            Title = note.Title,
            Content = note.Content,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId.Value,
            Version = YitIdHelper.NextId()
        };
        _context.NoteVersions.Add(noteVersion);
        await _context.SaveChangesAsync();

        return StatusCode(201, new NoteResponse("笔记创建成功", new NoteDto(
            note.Id,
            note.FolderId,
            null,
            note.Title,
            note.Content,
            note.IsShared,
            note.ShareToken,
            note.Version,
            note.CreatedAt,
            note.UpdatedAt
        )));
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<NoteResponse>> UpdateNote(long id, [FromBody] UpdateNoteRequest request)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var note = await _context.Notes.FindAsync(id);
        if (note == null || note.UserId != userId)
        {
            return NotFound(new { error = "笔记不存在" });
        }

        // 只有手动点击保存版本按钮时才保存版本历史
        if (request.SaveVersion)
        {
            var versionRecord = new server_dotnet.Models.NoteVersion
            {
                Id = YitIdHelper.NextId(),
                NoteId = note.Id,
                Title = note.Title,
                Content = note.Content,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = userId.Value,
                Version = YitIdHelper.NextId()
            };
            _context.NoteVersions.Add(versionRecord);
            await _context.SaveChangesAsync();

            // Cleanup old versions
            await CleanupOldVersionsAsync(note.Id);
        }

        if (request.Title != null) note.Title = request.Title;
        if (request.Content != null) note.Content = request.Content;
        if (request.FolderId != null)
        {
            // 支持 -1 表示移除分类（未分类）
            if (request.FolderId == "-1" || request.FolderId == "")
            {
                note.FolderId = null;
            }
            else if (long.TryParse(request.FolderId, out var fid))
            {
                note.FolderId = fid;
            }
        }

        // 每次保存都更新版本号为新的雪花ID
        note.Version = YitIdHelper.NextId();
        note.UpdatedAt = DateTimeOffset.UtcNow;
        note.UpdatedBy = userId.Value;

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
            note.Version,
            note.CreatedAt,
            note.UpdatedAt
        )));
    }

    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteNote(long id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var note = await _context.Notes.FindAsync(id);
        if (note == null || note.UserId != userId)
        {
            return NotFound(new { error = "笔记不存在" });
        }

        // 逻辑删除
        note.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "笔记删除成功" });
    }

    [HttpPost("{id}/share")]
    public async Task<ActionResult<ShareResponse>> ShareNote(long id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var note = await _context.Notes.FindAsync(id);
        if (note == null || note.UserId != userId)
        {
            return NotFound(new { error = "笔记不存在" });
        }

        note.IsShared = true;
        note.ShareToken = GenerateSecureToken();
        await _context.SaveChangesAsync();

        return Ok(new ShareResponse("分享成功", $"/shared/{note.ShareToken}"));
    }

    [HttpPost("{id}/unshare")]
    public async Task<ActionResult> UnshareNote(long id)
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
    public async Task<ActionResult<VersionsResponse>> GetVersions(long id)
    {
        var userId = GetUserId();
        if (userId == null) return Unauthorized();

        var note = await _context.Notes.FindAsync(id);
        if (note == null || note.UserId != userId)
        {
            return NotFound(new { error = "笔记不存在" });
        }

        var versions = await _context.NoteVersions
            .Where(v => v.NoteId == id && !v.IsDeleted)
            .OrderByDescending(v => v.CreatedAt)
            .Take(AppConstants.VersionsPageSize)
            .Select(v => new NoteVersionDto(v.Id, v.Title, v.Content, v.CreatedAt))
            .ToListAsync();

        return Ok(new VersionsResponse(versions));
    }

    [HttpPost("{id}/restore/{versionId}")]
    public async Task<ActionResult<NoteResponse>> RestoreVersion(long id, long versionId)
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
        var currentVersionRecord = new server_dotnet.Models.NoteVersion
        {
            Id = YitIdHelper.NextId(),
            NoteId = note.Id,
            Title = note.Title,
            Content = note.Content,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = userId.Value,
            Version = YitIdHelper.NextId()
        };
        _context.NoteVersions.Add(currentVersionRecord);

        // Restore
        note.Title = version.Title;
        note.Content = version.Content;
        note.UpdatedAt = DateTimeOffset.UtcNow;
        note.UpdatedBy = userId.Value;
        note.Version = YitIdHelper.NextId();
        await _context.SaveChangesAsync();

        // Cleanup old versions after restore
        await CleanupOldVersionsAsync(note.Id);

        var folder = note.FolderId.HasValue ? await _context.Folders.FindAsync(note.FolderId) : null;

        return Ok(new NoteResponse("恢复成功", new NoteDto(
            note.Id,
            note.FolderId,
            folder?.Name,
            note.Title,
            note.Content,
            note.IsShared,
            note.ShareToken,
            note.Version,
            note.CreatedAt,
            note.UpdatedAt
        )));
    }

    [HttpDelete("{id}/versions/{versionId}")]
    public async Task<ActionResult> DeleteVersion(long id, long versionId)
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

        // 逻辑删除
        version.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "版本删除成功" });
    }

    /// <summary>
    /// Cleanup old versions, keeping only the most recent ones
    /// </summary>
    private async Task CleanupOldVersionsAsync(long noteId)
    {
        var oldVersions = await _context.NoteVersions
            .Where(v => v.NoteId == noteId && !v.IsDeleted)
            .OrderByDescending(v => v.CreatedAt)
            .Skip(AppConstants.MaxVersionsToKeep)
            .ToListAsync();

        foreach (var version in oldVersions)
        {
            version.IsDeleted = true;
        }

        if (oldVersions.Count > 0)
        {
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Generate cryptographically secure token for share URLs
    /// </summary>
    private static string GenerateSecureToken()
    {
        var bytes = new byte[AppConstants.ShareTokenLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .Replace("=", "");
    }
}
