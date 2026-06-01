namespace DocuMindAI.Api.Helpers;

public class QdrantSettings
{
    public string Url { get; set; } = "http://localhost:6333";
    public string CollectionName { get; set; } = "documindai_chunks";
}
