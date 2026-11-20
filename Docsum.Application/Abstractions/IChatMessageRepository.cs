using Docsum.Domain;

namespace Docsum.Application.Abstractions;

public interface IChatMessageRepository
{
    Task<List<ChatMessage>> ListAsync(int documentId, CancellationToken ct);
    Task AddAsync(ChatMessage message, CancellationToken ct);
}
