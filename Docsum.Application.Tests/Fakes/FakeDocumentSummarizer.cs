using Docsum.Application.Abstractions;

namespace Docsum.Application.Tests.Fakes;

public class FakeDocumentSummarizer : IDocumentSummarizer
{
    public bool IsConfigured { get; set; } = true;
    public string? Summary { get; set; } = "- Pay 120 EUR by 30.11.2026.";
    public Exception? Error { get; set; }

    public List<string> Calls { get; } = [];

    public Task<string?> SummarizeAsync(string text, CancellationToken ct)
    {
        Calls.Add(text);
        return Error is null
            ? Task.FromResult(Summary)
            : Task.FromException<string?>(Error);
    }
}
