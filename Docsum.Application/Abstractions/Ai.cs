namespace Docsum.Application.Abstractions;

public record ChatTurn(string Role, string Content);

public interface IChatCompletion
{
    /// <summary>False when no API key/model/base URL is configured; callers should degrade gracefully.</summary>
    bool IsConfigured { get; }

    Task<string> CompleteAsync(
        string systemPrompt, IReadOnlyList<ChatTurn> messages, CancellationToken ct);
}

public interface IDocumentTextExtractor
{
    Task<string> ExtractAsync(Stream pdf, CancellationToken ct);
}

public interface IDocumentSummarizer
{
    /// <summary>False when no provider is configured; callers should skip instead of calling.</summary>
    bool IsConfigured { get; }

    /// <summary>Returns null when AI is not configured; extraction still succeeds.</summary>
    Task<string?> SummarizeAsync(string text, CancellationToken ct);
}
