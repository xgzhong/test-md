using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace server_dotnet.Models;

[Table("notes")]
public class Note
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    public int? FolderId { get; set; }

    [Required]
    [MaxLength(255)]
    public string Title { get; set; } = "无标题笔记";

    public string Content { get; set; } = string.Empty;

    [Required]
    public bool IsShared { get; set; } = false;

    public string? ShareToken { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    [ForeignKey("UserId")]
    public User? User { get; set; }

    [ForeignKey("FolderId")]
    public Folder? Folder { get; set; }

    public ICollection<NoteVersion> Versions { get; set; } = new List<NoteVersion>();
}
