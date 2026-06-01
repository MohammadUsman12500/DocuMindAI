namespace DocuMindAI.Api.DTOs;

public class SourceDto
{
    public int DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public int ChunkId { get; set; }
    public int PageNumber { get; set; }
    public double Score { get; set; }
    public string Preview { get; set; } = string.Empty;
}
