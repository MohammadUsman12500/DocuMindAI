using System.Text.RegularExpressions;

namespace DocuMindAI.Api.Services;

public record TextChunk(int ChunkIndex, int PageNumber, string Text, int WordCount);

public interface IChunkingService
{
    IReadOnlyList<TextChunk> CreateChunks(IReadOnlyList<ExtractedTextPage> pages, int chunkSize = 800, int overlap = 100);
}

public class ChunkingService : IChunkingService
{
    public IReadOnlyList<TextChunk> CreateChunks(IReadOnlyList<ExtractedTextPage> pages, int chunkSize = 800, int overlap = 100)
    {
        if (chunkSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkSize), "Chunk size must be greater than zero.");
        }

        if (overlap < 0 || overlap >= chunkSize)
        {
            throw new ArgumentOutOfRangeException(nameof(overlap), "Overlap must be zero or greater and smaller than chunk size.");
        }

        var chunks = new List<TextChunk>();
        var chunkIndex = 0;

        foreach (var page in pages)
        {
            var words = Regex.Matches(page.Text, @"\S+")
                .Select(match => match.Value)
                .ToArray();

            if (words.Length == 0)
            {
                continue;
            }

            var start = 0;
            while (start < words.Length)
            {
                var count = Math.Min(chunkSize, words.Length - start);
                var chunkText = string.Join(' ', words.Skip(start).Take(count));

                chunks.Add(new TextChunk(chunkIndex, page.PageNumber, chunkText, count));
                chunkIndex++;

                if (start + count >= words.Length)
                {
                    break;
                }

                start += chunkSize - overlap;
            }
        }

        return chunks;
    }
}
