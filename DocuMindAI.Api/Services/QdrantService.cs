using System.Text;
using System.Text.Json;
using DocuMindAI.Api.Helpers;
using Microsoft.Extensions.Options;

namespace DocuMindAI.Api.Services;

public record QdrantPoint(
    Guid Id,
    float[] Vector,
    int DocumentId,
    string DocumentName,
    int ChunkId,
    string ChunkText,
    int PageNumber);

public record QdrantSearchResult(
    int DocumentId,
    string DocumentName,
    int ChunkId,
    string ChunkText,
    int PageNumber,
    double Score);

public interface IQdrantService
{
    Task EnsureCollectionAsync(int vectorSize);
    Task UpsertPointsAsync(IReadOnlyList<QdrantPoint> points);
    Task<IReadOnlyList<QdrantSearchResult>> SearchAsync(float[] vector, int limit);
}

public class QdrantService : IQdrantService
{
    private readonly HttpClient _httpClient;
    private readonly QdrantSettings _settings;

    public QdrantService(HttpClient httpClient, IOptions<QdrantSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
        _httpClient.BaseAddress = new Uri(_settings.Url.TrimEnd('/') + "/");
    }

    public async Task EnsureCollectionAsync(int vectorSize)
    {
        var collection = Uri.EscapeDataString(_settings.CollectionName);
        using var getResponse = await _httpClient.GetAsync($"collections/{collection}");
        if (getResponse.IsSuccessStatusCode)
        {
            return;
        }

        if (getResponse.StatusCode != System.Net.HttpStatusCode.NotFound)
        {
            var error = await getResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Qdrant collection check failed: {error}");
        }

        using var createResponse = await _httpClient.PutAsync(
            $"collections/{collection}",
            JsonContent(new
            {
                vectors = new
                {
                    size = vectorSize,
                    distance = "Cosine"
                }
            }));

        if (!createResponse.IsSuccessStatusCode)
        {
            var error = await createResponse.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Qdrant collection creation failed: {error}");
        }
    }

    public async Task UpsertPointsAsync(IReadOnlyList<QdrantPoint> points)
    {
        if (points.Count == 0)
        {
            return;
        }

        await EnsureCollectionAsync(points[0].Vector.Length);

        var collection = Uri.EscapeDataString(_settings.CollectionName);
        var body = new
        {
            points = points.Select(point => new
            {
                id = point.Id,
                vector = point.Vector,
                payload = new
                {
                    documentId = point.DocumentId,
                    documentName = point.DocumentName,
                    chunkId = point.ChunkId,
                    chunkText = point.ChunkText,
                    pageNumber = point.PageNumber
                }
            })
        };

        using var response = await _httpClient.PutAsync($"collections/{collection}/points?wait=true", JsonContent(body));
        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Qdrant upsert failed: {error}");
        }
    }

    public async Task<IReadOnlyList<QdrantSearchResult>> SearchAsync(float[] vector, int limit)
    {
        var collection = Uri.EscapeDataString(_settings.CollectionName);
        var body = new
        {
            vector,
            limit,
            with_payload = true
        };

        using var response = await _httpClient.PostAsync($"collections/{collection}/points/search", JsonContent(body));
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"Qdrant search failed: {content}");
        }

        using var document = JsonDocument.Parse(content);
        if (!document.RootElement.TryGetProperty("result", out var resultElement) || resultElement.ValueKind != JsonValueKind.Array)
        {
            return Array.Empty<QdrantSearchResult>();
        }

        return resultElement.EnumerateArray()
            .Select(ParseSearchResult)
            .Where(result => result is not null)
            .Cast<QdrantSearchResult>()
            .ToList();
    }

    private static QdrantSearchResult? ParseSearchResult(JsonElement element)
    {
        if (!element.TryGetProperty("payload", out var payload))
        {
            return null;
        }

        return new QdrantSearchResult(
            GetInt(payload, "documentId"),
            GetString(payload, "documentName"),
            GetInt(payload, "chunkId"),
            GetString(payload, "chunkText"),
            GetInt(payload, "pageNumber"),
            element.TryGetProperty("score", out var score) ? score.GetDouble() : 0);
    }

    private static int GetInt(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value) && value.TryGetInt32(out var result)
            ? result
            : 0;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        return element.TryGetProperty(propertyName, out var value)
            ? value.GetString() ?? string.Empty
            : string.Empty;
    }

    private static StringContent JsonContent(object body)
    {
        return new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json");
    }
}
