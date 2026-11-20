using Docsum.Application.Abstractions;
using Docsum.Domain;

namespace Docsum.Application.Tests.Fakes;

public class FakeFolderRepository : IFolderRepository
{
    public List<Folder> Items { get; } = [];

    private int _nextId = 1;

    public Task<List<Folder>> ListAsync(int userId, CancellationToken ct) =>
        Task.FromResult(Items.Where(f => f.UserId == userId).ToList());

    public Task<Folder?> GetAsync(int userId, int folderId, CancellationToken ct) =>
        Task.FromResult(Items.FirstOrDefault(f => f.Id == folderId && f.UserId == userId));

    public Task AddAsync(Folder folder, CancellationToken ct)
    {
        folder.Id = _nextId++;
        Items.Add(folder);
        return Task.CompletedTask;
    }

    public Task SaveAsync(Folder folder, CancellationToken ct) => Task.CompletedTask;

    public Task<bool> NameExistsAsync(
        int userId, int? parentId, string name, int? excludeFolderId, CancellationToken ct) =>
        Task.FromResult(Items.Any(f =>
            f.UserId == userId
            && f.ParentFolderId == parentId
            && f.Id != excludeFolderId
            && string.Equals(f.Name, name, StringComparison.OrdinalIgnoreCase)));

    public Task DeleteAsync(Folder folder, CancellationToken ct)
    {
        // Mirrors the real repository: children move up to the parent before removal.
        foreach (var child in Items.Where(f => f.ParentFolderId == folder.Id))
            child.ParentFolderId = folder.ParentFolderId;

        Items.RemoveAll(f => f.Id == folder.Id);
        return Task.CompletedTask;
    }
}
