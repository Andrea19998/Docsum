using Docsum.Application.Abstractions;
using Docsum.Application.Common;
using Docsum.Domain;

namespace Docsum.Application.Folders;

public record FolderDto(int Id, string Name, int? ParentId);
public record CreateFolderRequest(string Name, int? ParentId);
public record UpdateFolderRequest(string Name, int? ParentId);

public class FolderService(IFolderRepository folders)
{
    public const int MaxNameLength = 100;

    public async Task<List<FolderDto>> ListAsync(int userId, CancellationToken ct) =>
        (await folders.ListAsync(userId, ct))
            .OrderBy(f => f.Name, StringComparer.OrdinalIgnoreCase)
            .Select(ToDto)
            .ToList();

    public async Task<Result<FolderDto>> CreateAsync(
        int userId, CreateFolderRequest req, CancellationToken ct)
    {
        var name = req.Name.Trim();
        if (name.Length is 0 or > MaxNameLength)
            return Result<FolderDto>.Fail($"Folder name must be 1-{MaxNameLength} characters.");

        if (req.ParentId is not null && await folders.GetAsync(userId, req.ParentId.Value, ct) is null)
            return Result<FolderDto>.Fail("Parent folder not found.");

        if (await folders.NameExistsAsync(userId, req.ParentId, name, null, ct))
            return Result<FolderDto>.Fail("A folder with this name already exists here.");

        var folder = new Folder
        {
            UserId = userId,
            ParentFolderId = req.ParentId,
            Name = name
        };
        await folders.AddAsync(folder, ct);

        return Result<FolderDto>.Ok(ToDto(folder));
    }

    public async Task<Result<FolderDto>> UpdateAsync(
        int userId, int folderId, UpdateFolderRequest req, CancellationToken ct)
    {
        var folder = await folders.GetAsync(userId, folderId, ct);
        if (folder is null) return Result<FolderDto>.Fail("Folder not found.");

        var name = req.Name.Trim();
        if (name.Length is 0 or > MaxNameLength)
            return Result<FolderDto>.Fail($"Folder name must be 1-{MaxNameLength} characters.");

        if (req.ParentId is not null)
        {
            if (req.ParentId == folderId)
                return Result<FolderDto>.Fail("A folder cannot be moved inside itself.");

            if (await folders.GetAsync(userId, req.ParentId.Value, ct) is null)
                return Result<FolderDto>.Fail("Parent folder not found.");

            // Walk ancestors: moving a folder under its own descendant would create a cycle.
            var cursor = await folders.GetAsync(userId, req.ParentId.Value, ct);
            while (cursor is not null)
            {
                if (cursor.Id == folderId)
                    return Result<FolderDto>.Fail("A folder cannot be moved inside itself.");
                cursor = cursor.ParentFolderId is null
                    ? null
                    : await folders.GetAsync(userId, cursor.ParentFolderId.Value, ct);
            }
        }

        if (await folders.NameExistsAsync(userId, req.ParentId, name, folderId, ct))
            return Result<FolderDto>.Fail("A folder with this name already exists here.");

        folder.Name = name;
        folder.ParentFolderId = req.ParentId;
        await folders.SaveAsync(folder, ct);

        return Result<FolderDto>.Ok(ToDto(folder));
    }

    public async Task<bool> DeleteAsync(int userId, int folderId, CancellationToken ct)
    {
        var folder = await folders.GetAsync(userId, folderId, ct);
        if (folder is null) return false;

        // Contents move up to the parent (root when the folder is top-level) so nothing is lost.
        await folders.DeleteAsync(folder, ct);
        return true;
    }

    private static FolderDto ToDto(Folder f) => new(f.Id, f.Name, f.ParentFolderId);
}
