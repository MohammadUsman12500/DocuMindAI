namespace DocuMindAI.Api.DTOs;

public class ChatResponse
{
    public int ChatSessionId { get; set; }
    public string Answer { get; set; } = string.Empty;
    public IReadOnlyList<SourceDto> Sources { get; set; } = Array.Empty<SourceDto>();
}
