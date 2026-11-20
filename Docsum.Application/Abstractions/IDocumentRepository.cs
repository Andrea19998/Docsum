using Docsum.Domain;

namespace Docsum.Application.Abstractions;

public interface IDocumentRepository
{
    Task AddAsync(Document document, CancellationToken ct);
    Task<Document?> GetAsync(int userId, int documentId, CancellationToken ct);
    Task<List<Document>> ListAsync(int userId, CancellationToken ct);
    Task SaveAsync(Document document, CancellationToken ct);
    Task DeleteAsync(Document document, CancellationToken ct);
}
