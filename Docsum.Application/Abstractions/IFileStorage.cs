namespace Docsum.Application.Abstractions;

public interface IFileStorage
{
    Task SaveAsync(string storedFileName, Stream content, CancellationToken ct);
    Task<Stream?> OpenReadAsync(string storedFileName, CancellationToken ct);
    Task DeleteAsync(string storedFileName, CancellationToken ct);
}