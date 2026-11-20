using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Docsum.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Docsum.Infrastructure.Ai;

/// <summary>
/// Works with any OpenAI-compatible chat-completions endpoint
/// (OpenAI, Azure OpenAI gateways, OpenRouter, Gemini's OpenAI-compatible endpoint, ...).
/// </summary>
public class OpenAiChatCompletion(HttpClient http, IOptions<AiOptions> options) : IChatCompletion
{
    private AiOptions Options => options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Options.BaseUrl) &&
        !string.IsNullOrWhiteSpace(Options.Model) &&
        !string.IsNullOrWhiteSpace(Options.ApiKey);

    public async Task<string> CompleteAsync(
        string systemPrompt, IReadOnlyList<ChatTurn> messages, CancellationToken ct)
    {
        var payload = new
        {
            model = Options.Model,
            temperature = 0.2,
            messages = new[] { new { role = "system", content = systemPrompt } }
                .Concat(messages.Select(m => new { role = m.Role, content = m.Content }))
                .ToArray()
        };

        using var request = new HttpRequestMessage(
            HttpMethod.Post, $"{Options.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiKey);
        request.Content = JsonContent.Create(payload);

        using var response = await http.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
            throw new InvalidOperationException(
                $"Provider returned {(int)response.StatusCode}: {Truncate(body, 400)}");

        using var json = JsonDocument.Parse(body);
        var content = json.RootElement
            .GetProperty("choices")[0]
            .GetProperty("message")
            .GetProperty("content")
            .GetString();

        return content ?? string.Empty;
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
