using System.Security.Claims;
using DocuMindAI.Api.DTOs;
using DocuMindAI.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMindAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IChatService _chatService;

    public ChatController(IChatService chatService)
    {
        _chatService = chatService;
    }

    [HttpPost("ask")]
    public async Task<ActionResult<ChatResponse>> Ask(ChatRequest request)
    {
        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        try
        {
            var response = await _chatService.AskAsync(request, userId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
