using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using DocuMindAI.Api.Data;
using DocuMindAI.Api.DTOs;
using DocuMindAI.Api.Entities;
using DocuMindAI.Api.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace DocuMindAI.Api.Services;

public interface IChatService
{
    Task<ChatResponse> AskAsync(ChatRequest request, int userId);
}

public class ChatService : IChatService
{
    private const string UnknownAnswer = "I don't know based on the uploaded documents.";

    private readonly ApplicationDbContext _dbContext;
    private readonly IEmbeddingService _embeddingService;
    private readonly HttpClient _httpClient;
    private readonly IQdrantService _qdrantService;
    private readonly OpenAiSettings _settings;

    public ChatService(
        ApplicationDbContext dbContext,
        IEmbeddingService embeddingService,
        HttpClient httpClient,
        IQdrantService qdrantService,
        IOptions<OpenAiSettings> options)
    {
        _dbContext = dbContext;
        _embeddingService = embeddingService;
        _httpClient = httpClient;
        _qdrantService = qdrantService;
        _settings = options.Value;
    }

    public async Task<ChatResponse> AskAsync(ChatRequest request, int userId)
    {
        var question = request.Question.Trim();
        if (string.IsNullOrWhiteSpace(question))
        {
            throw new InvalidOperationException("Question is required.");
        }

        var questionVector = await _embeddingService.GenerateEmbeddingAsync(question);
        var searchResults = await _qdrantService.SearchAsync(questionVector, 5);
        var sources = searchResults.Select(ToSourceDto).ToList();

        var answer = searchResults.Count == 0
            ? UnknownAnswer
            : await AskOpenAiAsync(question, searchResults);

        var session = await GetOrCreateSessionAsync(request.ChatSessionId, userId, question);
        _dbContext.ChatMessages.Add(new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = "user",
            Content = question
        });
        _dbContext.ChatMessages.Add(new ChatMessage
        {
            ChatSessionId = session.Id,
            Role = "assistant",
            Content = answer
        });
        session.UpdatedAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync();

        return new ChatResponse
        {
            ChatSessionId = session.Id,
            Answer = answer,
            Sources = sources
        };
    }

    private async Task<ChatSession> GetOrCreateSessionAsync(int? chatSessionId, int userId, string question)
    {
        if (chatSessionId.HasValue)
        {
            var existing = await _dbContext.ChatSessions
                .SingleOrDefaultAsync(session => session.Id == chatSessionId.Value && session.UserId == userId);

            if (existing is null)
            {
                throw new InvalidOperationException("Chat session was not found.");
            }

            return existing;
        }

        var session = new ChatSession
        {
            UserId = userId,
            Title = question.Length <= 80 ? question : question[..80]
        };

        _dbContext.ChatSessions.Add(session);
        await _dbContext.SaveChangesAsync();
        return session;
    }

    private async Task<string> AskOpenAiAsync(string question, IReadOnlyList<QdrantSearchResult> searchResults)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || _settings.ApiKey.StartsWith("YOUR_", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("OpenAI API key is not configured.");
        }

        var context = string.Join(
            "\n\n",
            searchResults.Select((result, index) =>
                $"Source {index + 1} (Document: {result.DocumentName}, Page: {result.PageNumber}, ChunkId: {result.ChunkId})\n{result.ChunkText}"));

        using var httpRequest = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
        httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _settings.ApiKey);
        httpRequest.Content = new StringContent(
            JsonSerializer.Serialize(new
            {
                model = _settings.ChatModel,
                temperature = 0.2,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are DocuMindAI, a knowledge base assistant. Answer only using the provided context. If the answer is not in the context, say exactly: \"I don't know based on the uploaded documents.\""
                    },
                    new
                    {
                        role = "user",
                        content = $"Context:\n{context}\n\nQuestion:\n{question}"
                    }
                }
            }),
            Encoding.UTF8,
            "application/json");

        using var response = await _httpClient.SendAsync(httpRequest);
        var content = await response.Content.ReadAsStringAsync();
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"OpenAI chat request failed: {content}");
        }

        var chatResponse = JsonSerializer.Deserialize<OpenAiChatResponse>(content, JsonOptions);
        return chatResponse?.Choices.FirstOrDefault()?.Message.Content?.Trim()
            ?? UnknownAnswer;
    }

    private static SourceDto ToSourceDto(QdrantSearchResult result)
    {
        return new SourceDto
        {
            DocumentId = result.DocumentId,
            DocumentName = result.DocumentName,
            ChunkId = result.ChunkId,
            PageNumber = result.PageNumber,
            Score = result.Score,
            Preview = result.ChunkText.Length <= 300 ? result.ChunkText : result.ChunkText[..300]
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class OpenAiChatResponse
    {
        public List<OpenAiChoice> Choices { get; set; } = new();
    }

    private sealed class OpenAiChoice
    {
        public OpenAiMessage Message { get; set; } = new();
    }

    private sealed class OpenAiMessage
    {
        public string Content { get; set; } = string.Empty;
    }
}
