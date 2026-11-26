using Docsum.Domain;

namespace Docsum.Application.Documents;

public record DocumentListItemDto(
    int Id, string FileName, int? FolderId, DocumentStatus Status, DateTime CreatedAt);

public record DocumentDetailDto(
    int Id, string FileName, int? FolderId, DocumentStatus Status, DateTime CreatedAt,
    string? Summary, string? ErrorMessage);

public record MoveDocumentRequest(int? FolderId);

public enum SummarizeError { None, NotFound, NotReady, AiUnavailable, Failed }

public record SummarizeOutcome(SummarizeError Error, string? Message, DocumentDetailDto? Value)
{
    public bool Success => Error == SummarizeError.None;

    public static SummarizeOutcome Ok(DocumentDetailDto value) =>
        new(SummarizeError.None, null, value);
    public static SummarizeOutcome NotFound() =>
        new(SummarizeError.NotFound, "Document not found.", null);
    public static SummarizeOutcome NotReady(string message) =>
        new(SummarizeError.NotReady, message, null);
    public static SummarizeOutcome AiUnavailable(string message) =>
        new(SummarizeError.AiUnavailable, message, null);
    public static SummarizeOutcome Failed(string message) =>
        new(SummarizeError.Failed, message, null);
}
