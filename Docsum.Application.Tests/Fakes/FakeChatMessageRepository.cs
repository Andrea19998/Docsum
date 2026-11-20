using Docsum.Application.Abstractions;
using Docsum.Domain;

namespace Docsum.Application.Tests.Fakes;

public class FakeChatMessageRepository : IChatMessageRepository
{
    public List<ChatMessage> Items { get; } = [];

    private int _nextId = 1;

    public Task<List<ChatMessage>> ListAsync(int documentId, CancellationToken ct) =>
        Task.FromResult(Items.Where(m => m.DocumentId == documentId).ToList());

    public Task AddAsync(ChatMessage message, CancellationToken ct)
    {
        message.Id = _nextId++;
        Items.Add(message);
        return Task.CompletedTask;
    }
}
