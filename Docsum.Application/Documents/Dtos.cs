using Docsum.Domain;

namespace Docsum.Application.Documents;

public record DocumentListItemDto(
    int Id, string FileName, int? FolderId, DocumentStatus Status, DateTime CreatedAt);

public record DocumentDetailDto(
    int Id, string FileName, int? FolderId, DocumentStatus Status, DateTime CreatedAt,
    string? Summary, string? ErrorMessage);

public record MoveDocumentRequest(int? FolderId);
