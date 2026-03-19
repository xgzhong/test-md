using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server_dotnet.Models;

[Table("folders")]
public class Folder
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public long Id { get; set; }

    [Required]
    public long UserId { get; set; }

    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    public int SortOrder { get; set; } = 0;

    public bool IsPinned { get; set; } = false;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;

    public long? CreatedBy { get; set; }

    public long? UpdatedBy { get; set; }

    /// <summary>
    /// 数据版本：雪花ID，用于记录版本
    /// </summary>
    public long Version { get; set; } = 0;

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }

    public ICollection<Note> Notes { get; set; } = new List<Note>();
}
