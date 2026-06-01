namespace DocuMindAI.Api.Entities;

public class DocumentChunk
{
    public int Id { get; set; }
    public int DocumentId { get; set; }
    public int ChunkIndex { get; set; }
    public int PageNumber { get; set; } = 1;
    public string Text { get; set; } = string.Empty;
    public int WordCount { get; set; }
    public Guid VectorPointId { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Document Document { get; set; } = null!;
}
