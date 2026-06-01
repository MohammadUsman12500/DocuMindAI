using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DocuMindAI.Api.Helpers;
using Microsoft.Extensions.Options;

namespace DocuMindAI.Api.Services;

public interface IEmbeddingService
{
    Task<float[]> GenerateEmbeddingAsync(string text);
}

public class OpenAiEmbeddingService : IEmbeddingService
{
    private readonly HttpClient _httpClient;
    private readonly OpenAiSettings _settings;

    public OpenAiEmbeddingService(HttpClient httpClient, IOptions<OpenAiSettings> options)
    {
        _httpClient = httpClient;
        _settings = options.Value;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/embeddings");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        request.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                model = _settings.EmbeddingModel,
                input = text
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(request);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI embedding request failed: {content}");
        }

        var embeddingResponse = JsonSerializer.Deserialize<EmbeddingResponse>(content, JsonOptions);
        return embeddingResponse?.Data.FirstOrDefault()?.Embedding
            ?? throw new InvalidOperationException("OpenAI embedding response did not include an embedding.");
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class EmbeddingResponse
    {
        public List<EmbeddingItem> Data { get; set; } = new();
    }

    private sealed class EmbeddingItem
    {
        public float[] Embedding { get; set; } = Array.Empty<float>();
    }
}
