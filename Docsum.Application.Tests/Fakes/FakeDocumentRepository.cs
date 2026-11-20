using Docsum.Application.Abstractions;
using Docsum.Domain;

namespace Docsum.Application.Tests.Fakes;

public class FakeDocumentRepository : IDocumentRepository
{
    public List<Document> Items { get; } = [];
    private int _nextId = 1;

    public Task AddAsync(Document d, CancellationToken ct) { d.Id = _nextId++; Items.Add(d); return Task.CompletedTask; }
    public Task<Document?> GetAsync(int userId, int id, CancellationToken ct) =>
        Task.FromResult(Items.FirstOrDefault(d => d.Id == id && d.UserId == userId));
    public Task<List<Document>> ListAsync(int userId, CancellationToken ct) =>
        Task.FromResult(Items.Where(d => d.UserId == userId).ToList());
    public Task SaveAsync(Document d, CancellationToken ct) => Task.CompletedTask;
    public Task DeleteAsync(Document d, CancellationToken ct) { Items.Remove(d); return Task.CompletedTask; }
}
