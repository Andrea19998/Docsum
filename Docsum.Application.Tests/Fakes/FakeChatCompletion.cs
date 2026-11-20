using Docsum.Application.Abstractions;

namespace Docsum.Application.Tests.Fakes;

public class FakeChatCompletion : IChatCompletion
{
    public bool IsConfigured { get; set; } = true;
    public string Answer { get; set; } = "The deadline is 30.11.2026.";

    public List<(string SystemPrompt, IReadOnlyList<ChatTurn> Messages)> Calls { get; } = [];

    public Task<string> CompleteAsync(
        string systemPrompt, IReadOnlyList<ChatTurn> messages, CancellationToken ct)
    {
        Calls.Add((systemPrompt, messages));
        return Task.FromResult(Answer);
    }
}
