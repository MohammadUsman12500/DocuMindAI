using System.Security.Claims;
using DocuMindAI.Api.DTOs;
using DocuMindAI.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DocuMindAI.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/[controller]")]
public class DocumentsController : ControllerBase
{
    private readonly IDocumentService _documentService;

    public DocumentsController(IDocumentService documentService)
    {
        _documentService = documentService;
    }

    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(50_000_000)]
    public async Task<ActionResult<DocumentUploadResponse>> Upload(IFormFile file)
    {
        if (file is null)
        {
            return BadRequest(new { message = "File is required." });
        }

        var userIdValue = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdValue, out var userId))
        {
            return Unauthorized(new { message = "Invalid user token." });
        }

        try
        {
            var response = await _documentService.UploadAsync(file, userId);
            return Ok(response);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
