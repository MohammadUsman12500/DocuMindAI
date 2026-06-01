namespace DocuMindAI.Api.DTOs;

public class DocumentUploadResponse
{
    public int DocumentId { get; set; }
    public string DocumentName { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public int ChunkCount { get; set; }
    public string Message { get; set; } = string.Empty;
}
