namespace DocuMindAI.Api.Entities;

public class User
{
    public int Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Document> Documents { get; set; } = new List<Document>();
    public ICollection<ChatSession> ChatSessions { get; set; } = new List<ChatSession>();
}
