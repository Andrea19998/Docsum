namespace Docsum.Domain;

public class Folder
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public User User { get; set; } = null!;

    public int? ParentFolderId { get; set; }
    public Folder? ParentFolder { get; set; }

    public string Name { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public List<Folder> Children { get; set; } = [];
    public List<Document> Documents { get; set; } = [];
}
