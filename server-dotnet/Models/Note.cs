using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server_dotnet.Models;

[Table("notes")]
public class Note
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; }

    [Required]
    public long UserId { get; set; }

    public long? FolderId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = "无标题笔记";

    public string Content { get; set; } = string.Empty;

    [Required]
    public bool IsShared { get; set; } = false;

    /// <summary>
    /// Share token stored as SHA256 hash for security
    /// </summary>
    [MaxLength(64)]
    public string? ShareToken { get; set; }

    [Required]
    public bool IsDeleted { get; set; } = false;

    /// <summary>
    /// 数据版本：雪花ID，用于记录版本
    /// </summary>
    public long Version { get; set; } = 0;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public long? CreatedBy { get; set; }

    public long? UpdatedBy { get; set; }

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("FolderId")]
    public Folder? Folder { get; set; }

    public ICollection<NoteVersion> Versions { get; set; } = new List<NoteVersion>();
}
