using Docsum.Application.Abstractions;
using Docsum.Domain;
using Microsoft.EntityFrameworkCore;

namespace Docsum.Infrastructure.Persistence;

public class ChatMessageRepository(AppDbContext db) : IChatMessageRepository
{
    public Task<List<ChatMessage>> ListAsync(int documentId, CancellationToken ct) =>
        db.ChatMessages
            .AsNoTracking()
            .Where(m => m.DocumentId == documentId)
            .OrderBy(m => m.CreatedAt)
            .ThenBy(m => m.Id)
            .ToListAsync(ct);

    public async Task AddAsync(ChatMessage message, CancellationToken ct)
    {
        db.ChatMessages.Add(message);
        await db.SaveChangesAsync(ct);
    }
}
