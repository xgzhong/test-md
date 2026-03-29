using Microsoft.EntityFrameworkCore;
using server_dotnet.Models;

namespace server_dotnet.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Folder> Folders { get; set; }
    public DbSet<Note> Notes { get; set; }
    public DbSet<NoteVersion> NoteVersions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configurations
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.Username).IsUnique();
        });

        // Folder configurations
        modelBuilder.Entity<Folder>(entity =>
        {
            entity.HasOne(f => f.User)
                .WithMany(u => u.Folders)
                .HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // 性能优化：添加索引
            entity.HasIndex(f => f.ParentId);
            entity.HasIndex(f => new { f.UserId, f.IsPinned });
            entity.HasIndex(f => new { f.UserId, f.SortOrder });
        });

        // Note configurations
        modelBuilder.Entity<Note>(entity =>
        {
            entity.HasOne(n => n.User)
                .WithMany(u => u.Notes)
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(n => n.Folder)
                .WithMany(f => f.Notes)
                .HasForeignKey(n => n.FolderId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasIndex(n => n.ShareToken);

            // 性能优化：添加复合索引
            entity.HasIndex(n => new { n.FolderId, n.UserId, n.IsDeleted });
            entity.HasIndex(n => new { n.UserId, n.IsDeleted });
            entity.HasIndex(n => n.UpdatedAt);
        });

        // NoteVersion configurations
        modelBuilder.Entity<NoteVersion>(entity =>
        {
            entity.HasOne(nv => nv.Note)
                .WithMany(n => n.Versions)
                .HasForeignKey(nv => nv.NoteId)
                .OnDelete(DeleteBehavior.Cascade);

            // 性能优化：添加索引
            entity.HasIndex(nv => nv.NoteId);
            entity.HasIndex(nv => new { nv.NoteId, nv.Version });
            entity.HasIndex(nv => new { nv.NoteId, nv.CreatedAt }); // 版本查询排序优化
        });
    }
}
