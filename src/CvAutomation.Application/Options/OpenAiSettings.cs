namespace CvAutomation.Application.Options;

public class OpenAiSettings
{
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-4o-mini";
    public string BaseUrl { get; set; } = "https://api.openai.com/v1";

    public string ChatApiKey { get; set; } = string.Empty;
    public string ChatModel { get; set; } = string.Empty;
    public string ChatBaseUrl { get; set; } = string.Empty;

    public string EmbeddingApiKey { get; set; } = string.Empty;
    public string EmbeddingModel { get; set; } = "text-embedding-3-small";
    public string EmbeddingBaseUrl { get; set; } = "https://api.openai.com/v1";
}
