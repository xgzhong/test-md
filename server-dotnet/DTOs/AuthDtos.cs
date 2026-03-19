namespace server_dotnet.DTOs;

// Auth DTOs
public record RegisterRequest(string Username, string Email, string Password);
public record LoginRequest(string Email, string Password);
public record AuthResponse(string Message, string Token, UserDto User);
public record UserDto(long Id, string Username, string Email);
public record UserResponse(UserDto User);

// Note DTOs
public record NoteDto(long Id, long? FolderId, string? FolderName, string Title, string Content, bool IsShared, string? ShareToken, long Version, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public record CreateNoteRequest(string? Title, string? Content, long? FolderId);
public record UpdateNoteRequest(string? Title, string? Content, long? FolderId, bool SaveVersion = false);
public record NoteResponse(string Message, NoteDto? Note = null);
public record NotesResponse(List<NoteDto> Notes, int TotalCount = 0);
public record ShareResponse(string Message, string ShareUrl);
public record VersionsResponse(List<NoteVersionDto> Versions);

// Folder DTOs
public record FolderDto(long Id, string Name, int NoteCount, int SortOrder = 0, bool IsPinned = false);
public record FoldersResponse(List<FolderDto> Folders, int UncategorizedCount);
public record CreateFolderRequest(string Name);
public record UpdateFolderRequest(string Name);
public record FolderResponse(string Message, FolderDto? Folder = null);

// Shared DTOs
public record SharedNoteDto(long Id, string Title, string Content, string Author, DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt);
public record SharedNoteResponse(SharedNoteDto Note);
