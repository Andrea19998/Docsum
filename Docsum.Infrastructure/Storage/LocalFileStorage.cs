using Docsum.Application.Abstractions;

namespace Docsum.Infrastructure.Storage;

public class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(string rootPath)
    {
        _root = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_root);
    }

    public async Task SaveAsync(string storedFileName, Stream content, CancellationToken ct)
    {
        await using var file = File.Create(ResolvePath(storedFileName));
        await content.CopyToAsync(file, ct);
    }

    public Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken ct)
    {
        var path = ResolvePath(storedFileName);
        Stream? stream = File.Exists(path) ? File.OpenRead(path) : null;
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storedFileName, CancellationToken ct)
    {
        File.Delete(ResolvePath(storedFileName));
        return Task.CompletedTask;
    }

    private string ResolvePath(string storedFileName)
    {
        var full = Path.GetFullPath(Path.Combine(_root, storedFileName));
        if (!full.StartsWith(_root + Path.DirectorySeparatorChar))
            throw new InvalidOperationException("Invalid file name.");
        return full;
    }
}