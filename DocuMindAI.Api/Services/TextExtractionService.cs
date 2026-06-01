using DocumentFormat.OpenXml.Packaging;
using UglyToad.PdfPig;

namespace DocuMindAI.Api.Services;

public record ExtractedTextPage(int PageNumber, string Text);

public interface ITextExtractionService
{
    Task<IReadOnlyList<ExtractedTextPage>> ExtractAsync(string filePath);
}

public class TextExtractionService : ITextExtractionService
{
    public async Task<IReadOnlyList<ExtractedTextPage>> ExtractAsync(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();

        return extension switch
        {
            ".txt" => await ExtractTxtAsync(filePath),
            ".pdf" => ExtractPdf(filePath),
            ".docx" => ExtractDocx(filePath),
            _ => throw new InvalidOperationException("Unsupported file type.")
        };
    }

    private static async Task<IReadOnlyList<ExtractedTextPage>> ExtractTxtAsync(string filePath)
    {
        var text = await File.ReadAllTextAsync(filePath);
        return new[] { new ExtractedTextPage(1, text) };
    }

    private static IReadOnlyList<ExtractedTextPage> ExtractPdf(string filePath)
    {
        using var document = PdfDocument.Open(filePath);
        return document.GetPages()
            .Select(page => new ExtractedTextPage(page.Number, page.Text))
            .ToList();
    }

    private static IReadOnlyList<ExtractedTextPage> ExtractDocx(string filePath)
    {
        using var document = WordprocessingDocument.Open(filePath, false);
        var text = document.MainDocumentPart?.Document.Body?.InnerText ?? string.Empty;
        return new[] { new ExtractedTextPage(1, text) };
    }
}
