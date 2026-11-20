using Docsum.Application.Abstractions;
using Docsum.Domain;
using Microsoft.EntityFrameworkCore;

namespace Docsum.Infrastructure.Persistence;

public class FolderRepository(AppDbContext db) : IFolderRepository
{
    public Task<List<Folder>> ListAsync(int userId, CancellationToken ct) =>
        db.Folders
            .AsNoTracking()
            .Where(f => f.UserId == userId)
            .OrderBy(f => f.Name)
            .ToListAsync(ct);

    public Task<Folder?> GetAsync(int userId, int folderId, CancellationToken ct) =>
        db.Folders.FirstOrDefaultAsync(f => f.Id == folderId && f.UserId == userId, ct);

    public async Task AddAsync(Folder folder, CancellationToken ct)
    {
        db.Folders.Add(folder);
        await db.SaveChangesAsync(ct);
    }

    public Task SaveAsync(Folder folder, CancellationToken ct) => db.SaveChangesAsync(ct);

    public Task<bool> NameExistsAsync(
        int userId, int? parentId, string name, int? excludeFolderId, CancellationToken ct)
    {
        var lower = name.ToLower();
        return db.Folders.AnyAsync(
            f => f.UserId == userId
                 && f.ParentFolderId == parentId
                 && f.Id != excludeFolderId
                 && f.Name.ToLower() == lower,
            ct);
    }

    public async Task DeleteAsync(Folder folder, CancellationToken ct)
    {
        await using var tx = await db.Database.BeginTransactionAsync(ct);

        // Re-parent before removing the folder so neither documents nor subfolders are lost.
        await db.Folders
            .Where(f => f.ParentFolderId == folder.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(f => f.ParentFolderId, folder.ParentFolderId), ct);

        await db.Documents
            .Where(d => d.FolderId == folder.Id)
            .ExecuteUpdateAsync(s => s.SetProperty(d => d.FolderId, folder.ParentFolderId), ct);

        db.Folders.Remove(folder);
        await db.SaveChangesAsync(ct);

        await tx.CommitAsync(ct);
    }
}
