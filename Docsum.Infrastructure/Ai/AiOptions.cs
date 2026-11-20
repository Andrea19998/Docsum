namespace Docsum.Infrastructure.Ai;

public class AiOptions
{
    public const string SectionName = "Ai";

    /// <summary>OpenAI-compatible base URL, e.g. https://api.openai.com/v1</summary>
    public string BaseUrl { get; set; } = string.Empty;
    public string Model { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public int TimeoutSeconds { get; set; } = 120;
}
