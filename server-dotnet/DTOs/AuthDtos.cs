namespace server_dotnet.DTOs;

// Auth DTOs
public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Message, string Token, UserDto User);
public record UserDto(int Id, string Username, string Email);
public record UserResponse(UserDto User);

// Note DTOs
public record NoteDto(int Id, int? FolderId, string? FolderName, string Title, string Content, bool IsShared, string? ShareToken, int DataVersion, DateTime CreatedAt, DateTime UpdatedAt);
public record CreateNoteRequest(string? Title, string? Content, int? FolderId);
public record UpdateNoteRequest(string? Title, string? Content, int? FolderId, bool SaveVersion = false);
public record NoteResponse(string Message, NoteDto? Note = null);
public record NotesResponse(List<NoteDto> Notes, int TotalCount = 0);
public record ShareResponse(string Message, string ShareUrl);
public record VersionsResponse(List<NoteVersionDto> Versions);

// Folder DTOs
public record FolderDto(int Id, string Name, int NoteCount, int SortOrder = 0, bool IsPinned = false);
public record FoldersResponse(List<FolderDto> Folders, int UncategorizedCount);
public record CreateFolderRequest(string Name);
public record UpdateFolderRequest(string Name);
public record FolderResponse(string Message, FolderDto? Folder = null);

// Shared DTOs
public record SharedNoteDto(int Id, string Title, string Content, string Author, DateTime CreatedAt, DateTime UpdatedAt);
public record SharedNoteResponse(SharedNoteDto Note);
