namespace DocuMindAI.Api.Entities;

public class ChatMessage
{
    public int Id { get; set; }
    public int ChatSessionId { get; set; }
    public string Role { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ChatSession ChatSession { get; set; } = null!;
}
