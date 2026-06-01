using DocuMindAI.Api.Data;
using DocuMindAI.Api.DTOs;
using DocuMindAI.Api.Entities;

namespace DocuMindAI.Api.Services;

public interface IDocumentService
{
    Task<DocumentUploadResponse> UploadAsync(IFormFile file, int userId);
}

public class DocumentService : IDocumentService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".txt",
        ".pdf",
        ".docx"
    };

    private readonly ApplicationDbContext _dbContext;
    private readonly IChunkingService _chunkingService;
    private readonly IEmbeddingService _embeddingService;
    private readonly IWebHostEnvironment _environment;
    private readonly IQdrantService _qdrantService;
    private readonly ITextExtractionService _textExtractionService;

    public DocumentService(
        ApplicationDbContext dbContext,
        IChunkingService chunkingService,
        IEmbeddingService embeddingService,
        IWebHostEnvironment environment,
        IQdrantService qdrantService,
        ITextExtractionService textExtractionService)
    {
        _dbContext = dbContext;
        _chunkingService = chunkingService;
        _embeddingService = embeddingService;
        _environment = environment;
        _qdrantService = qdrantService;
        _textExtractionService = textExtractionService;
    }

    public async Task<DocumentUploadResponse> UploadAsync(IFormFile file, int userId)
    {
        if (file.Length == 0)
        {
            throw new InvalidOperationException("Uploaded file is empty.");
        }

        var extension = Path.GetExtension(file.FileName);
        if (!AllowedExtensions.Contains(extension))
        {
            throw new InvalidOperationException("Only TXT, PDF, and DOCX files are supported.");
        }

        var uploadsPath = Path.Combine(_environment.ContentRootPath, "Uploads");
        Directory.CreateDirectory(uploadsPath);

        var safeOriginalName = Path.GetFileName(file.FileName);
        var storedFileName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var storedPath = Path.Combine(uploadsPath, storedFileName);

        await using (var stream = File.Create(storedPath))
        {
            await file.CopyToAsync(stream);
        }

        var pages = await _textExtractionService.ExtractAsync(storedPath);
        var chunks = _chunkingService.CreateChunks(pages);
        if (chunks.Count == 0)
        {
            throw new InvalidOperationException("No readable text was found in the document.");
        }

        var document = new Document
        {
            UserId = userId,
            Name = Path.GetFileNameWithoutExtension(safeOriginalName),
            OriginalFileName = safeOriginalName,
            ContentType = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType,
            FilePath = storedPath,
            FileSizeBytes = file.Length
        };

        foreach (var chunk in chunks)
        {
            document.Chunks.Add(new DocumentChunk
            {
                ChunkIndex = chunk.ChunkIndex,
                PageNumber = chunk.PageNumber,
                Text = chunk.Text,
                WordCount = chunk.WordCount
            });
        }

        _dbContext.Documents.Add(document);
        await _dbContext.SaveChangesAsync();

        var points = new List<QdrantPoint>();
        foreach (var chunk in document.Chunks.OrderBy(chunk => chunk.ChunkIndex))
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync(chunk.Text);
            points.Add(new QdrantPoint(
                chunk.VectorPointId,
                embedding,
                document.Id,
                document.Name,
                chunk.Id,
                chunk.Text,
                chunk.PageNumber));
        }

        await _qdrantService.UpsertPointsAsync(points);

        return new DocumentUploadResponse
        {
            DocumentId = document.Id,
            DocumentName = document.Name,
            OriginalFileName = document.OriginalFileName,
            FileSizeBytes = document.FileSizeBytes,
            ChunkCount = document.Chunks.Count,
            Message = "Document uploaded and chunked successfully."
        };
    }
}
