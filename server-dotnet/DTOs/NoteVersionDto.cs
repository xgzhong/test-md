namespace server_dotnet.DTOs;

public record NoteVersionDto(long Id, string Title, string Content, DateTimeOffset CreatedAt);
