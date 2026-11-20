namespace Docsum.Domain;

public enum DocumentStatus { Uploaded, Processing, Done, Failed }

public class Document
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? FolderId { get; set; }
    public Folder? Folder { get; set; }

    public string FileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public long SizeBytes { get; set; }

    public string? ExtractedText { get; set; }
    public string? Summary { get; set; }
    public DocumentStatus Status { get; set; } = DocumentStatus.Uploaded;
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}