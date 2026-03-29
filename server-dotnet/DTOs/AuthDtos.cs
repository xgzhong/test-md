using System.ComponentModel.DataAnnotations;
using server_dotnet.Constants;

namespace server_dotnet.DTOs;

// Auth DTOs with validation
public record RegisterRequest(
    [Required(ErrorMessage = "用户名不能为空")]
    [StringLength(AppConstants.MaxUsernameLength, MinimumLength = AppConstants.MinUsernameLength, ErrorMessage = "用户名长度必须在2-50之间")]
    [RegularExpression(@"^[a-zA-Z0-9_\u4e00-\u9fa5]+$", ErrorMessage = "用户名只能包含字母、数字、下划线和中文")]
    string Username,

    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    [StringLength(AppConstants.MaxEmailLength)]
    string Email,

    [Required(ErrorMessage = "密码不能为空")]
    [StringLength(100, MinimumLength = AppConstants.MinPasswordLength, ErrorMessage = "密码长度必须在8-100之间")]
    [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).+$", ErrorMessage = "密码必须包含大小写字母和数字")]
    string Password
);

public record LoginRequest(
    [Required(ErrorMessage = "邮箱不能为空")]
    [EmailAddress(ErrorMessage = "邮箱格式不正确")]
    string Email,

    [Required(ErrorMessage = "密码不能为空")]
    string Password
);

public record AuthResponse(string Message, string? Token, UserDto? User);
public record UserDto(long Id, string Username, string Email);
public record UserResponse(UserDto User);

// Note DTOs
public record NoteDto(long Id, long? FolderId, string? FolderName, string Title, string Content, bool IsShared, string? ShareToken, long Version, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public record CreateNoteRequest(string? Title, string? Content, string? FolderId);
public record UpdateNoteRequest(string? Title, string? Content, string? FolderId, bool SaveVersion = false);
public record NoteResponse(string Message, NoteDto? Note = null);
public record NotesResponse(List<NoteDto> Notes, int TotalCount = 0);
public record ShareResponse(string Message, string ShareUrl);
public record VersionsResponse(List<NoteVersionDto> Versions);

// Folder DTOs
public record FolderDto(long Id, string Name, int NoteCount, int SortOrder = 0, bool IsPinned = false, long ParentId = 0, List<FolderDto>? Children = null);
public record FoldersResponse(List<FolderDto> Folders, int UncategorizedCount);
public record CreateFolderRequest(string Name, string? ParentId = null);
public record UpdateFolderRequest(string? Name = null, string? ParentId = null);
public record FolderResponse(string Message, FolderDto? Folder = null);

// Shared DTOs
public record SharedNoteDto(long Id, string Title, string Content, string Author, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public record SharedNoteResponse(SharedNoteDto Note);
