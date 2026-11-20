using Docsum.Application.Abstractions;
using Docsum.Domain;
using Microsoft.EntityFrameworkCore;

namespace Docsum.Infrastructure.Persistence;

public class DocumentRepository(AppDbContext db) : IDocumentRepository
{
    public async Task AddAsync(Document document, CancellationToken ct)
    {
        db.Documents.Add(document);
        await db.SaveChangesAsync(ct);
    }

    public Task<Document?> GetAsync(int userId, int documentId, CancellationToken ct) =>
        db.Documents.FirstOrDefaultAsync(d => d.Id == documentId && d.UserId == userId, ct);

    public Task<List<Document>> ListAsync(int userId, CancellationToken ct) =>
        db.Documents
            .AsNoTracking()
            .Where(d => d.UserId == userId)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

    public Task SaveAsync(Document document, CancellationToken ct) => db.SaveChangesAsync(ct);

    public async Task DeleteAsync(Document document, CancellationToken ct)
    {
        db.Documents.Remove(document);
        await db.SaveChangesAsync(ct);
    }
}
