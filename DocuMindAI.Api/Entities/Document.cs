namespace DocuMindAI.Api.Entities;

public class Document
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    public User User { get; set; } = null!;
    public ICollection<DocumentChunk> Chunks { get; set; } = new List<DocumentChunk>();
}
