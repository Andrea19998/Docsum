using Docsum.Domain;
using Microsoft.EntityFrameworkCore;

namespace Docsum.Infrastructure.Persistence;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<Document> Documents => Set<Document>();
    public DbSet<Folder> Folders => Set<Folder>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(e =>
        {
            e.HasIndex(u => u.Email).IsUnique();
            e.Property(u => u.Email).HasMaxLength(255);
        });

        modelBuilder.Entity<Folder>(e =>
        {
            e.HasOne(f => f.User).WithMany().HasForeignKey(f => f.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // Deleting a folder is handled explicitly in FolderRepository (contents move to parent),
            // so the DB-level restriction only guards against accidental cascades.
            e.HasOne(f => f.ParentFolder).WithMany(f => f.Children)
                .HasForeignKey(f => f.ParentFolderId)
                .OnDelete(DeleteBehavior.Restrict);

            e.Property(f => f.Name).HasMaxLength(100).IsRequired();
            e.HasIndex(f => new { f.UserId, f.ParentFolderId });
        });

        modelBuilder.Entity<Document>(e =>
        {
            e.HasOne(d => d.User).WithMany().HasForeignKey(d => d.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            e.HasOne(d => d.Folder).WithMany(f => f.Documents)
                .HasForeignKey(d => d.FolderId)
                .OnDelete(DeleteBehavior.SetNull);

            e.HasIndex(d => d.UserId);
            e.HasIndex(d => d.FolderId);
            e.Property(d => d.FileName).HasMaxLength(255);
            e.Property(d => d.StoredFileName).HasMaxLength(100);
        });

        modelBuilder.Entity<ChatMessage>(e =>
        {
            e.HasOne(m => m.Document).WithMany().HasForeignKey(m => m.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            e.Property(m => m.Content).IsRequired();
            e.HasIndex(m => new { m.DocumentId, m.CreatedAt });
        });
    }
}
