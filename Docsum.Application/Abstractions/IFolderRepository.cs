using Docsum.Domain;

namespace Docsum.Application.Abstractions;

public interface IFolderRepository
{
    Task<List<Folder>> ListAsync(int userId, CancellationToken ct);
    Task<Folder?> GetAsync(int userId, int folderId, CancellationToken ct);
    Task AddAsync(Folder folder, CancellationToken ct);
    Task SaveAsync(Folder folder, CancellationToken ct);

    Task<bool> NameExistsAsync(
        int userId, int? parentId, string name, int? excludeFolderId, CancellationToken ct);

    /// <summary>
    /// Moves direct child folders and documents to the folder's parent (root when top-level),
    /// then deletes the folder — all in one transaction.
    /// </summary>
    Task DeleteAsync(Folder folder, CancellationToken ct);
}
