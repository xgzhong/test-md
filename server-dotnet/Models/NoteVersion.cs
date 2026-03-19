using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server_dotnet.Models;

[Table("note_versions")]
public class NoteVersion
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; }

    [Required]
    public long NoteId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = string.Empty;

    public string Content { get; set; } = string.Empty;

    [Required]
    public bool IsDeleted { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public long? CreatedBy { get; set; }

    /// <summary>
    /// 数据版本：雪花ID，用于记录版本
    /// </summary>
    public long Version { get; set; } = 0;

    // Navigation properties
    [ForeignKey("NoteId")]
    public Note? Note { get; set; }
}
