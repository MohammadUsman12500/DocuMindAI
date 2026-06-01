using System.ComponentModel.DataAnnotations;

namespace DocuMindAI.Api.DTOs;

public class ChatRequest
{
    public int? ChatSessionId { get; set; }

    [Required]
    public string Question { get; set; } = string.Empty;
}
