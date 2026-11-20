using Docsum.Application.Abstractions;

namespace Docsum.Application.Tests.Fakes;

public class FakeFileStorage : IFileStorage
{
    public Dictionary<string, byte[]> Files { get; } = [];

    public async Task SaveAsync(string name, Stream content, CancellationToken ct)
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, ct);
        Files[name] = ms.ToArray();
    }
    public Task<Stream?> OpenReadAsync(string name, CancellationToken ct) =>
        Task.FromResult<Stream?>(Files.TryGetValue(name, out var b) ? new MemoryStream(b) : null);
    public Task DeleteAsync(string name, CancellationToken ct) { Files.Remove(name); return Task.CompletedTask; }
}