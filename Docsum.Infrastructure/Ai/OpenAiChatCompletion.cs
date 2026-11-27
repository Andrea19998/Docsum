using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Docsum.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace Docsum.Infrastructure.Ai;

/// <summary>
/// Works with any OpenAI-compatible chat-completions endpoint
/// (OpenAI, Groq, Azure OpenAI gateways, OpenRouter, Gemini's OpenAI-compatible endpoint, ...).
/// </summary>
public class OpenAiChatCompletion(HttpClient http, IOptions<AiOptions> options) : IChatCompletion
{
    private const int MaxAttempts = 2;

    /// <summary>A longer provider delay is not worth blocking a request for.</summary>
    private static readonly TimeSpan MaxRetryDelay = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan DefaultRetryDelay = TimeSpan.FromSeconds(1);

    private AiOptions Options => options.Value;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Options.BaseUrl) &&
        !string.IsNullOrWhiteSpace(Options.Model) &&
        !string.IsNullOrWhiteSpace(Options.ApiKey);

    public async Task<string> CompleteAsync(
        string systemPrompt, IReadOnlyList<ChatTurn> messages, CancellationToken ct)
    {
        for (var attempt = 1; ; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                using var request = BuildRequest(systemPrompt, messages);
                response = await http.SendAsync(request, ct);
            }
            catch (TaskCanceledException) when (!ct.IsCancellationRequested)
            {
                throw new InvalidOperationException(
                    $"The AI provider did not respond within {Options.TimeoutSeconds} seconds.");
            }

            using (response)
            {
                var body = await response.Content.ReadAsStringAsync(ct);

                if (response.IsSuccessStatusCode) return ParseContent(body);

                // Free tiers throttle aggressively; one short retry keeps summaries from being lost.
                if (response.StatusCode == HttpStatusCode.TooManyRequests && attempt < MaxAttempts)
                {
                    var delay = RetryDelay(response);
                    if (delay <= MaxRetryDelay)
                    {
                        await Task.Delay(delay, ct);
                        continue;
                    }
                }

                throw new InvalidOperationException(Describe(response.StatusCode, body));
            }
        }
    }

    private HttpRequestMessage BuildRequest(
        string systemPrompt, IReadOnlyList<ChatTurn> messages)
    {
        var payload = new
        {
            model = Options.Model,
            temperature = 0.2,
            messages = new[] { new { role = "system", content = systemPrompt } }
                .Concat(messages.Select(m => new { role = m.Role, content = m.Content }))
                .ToArray()
        };

        var request = new HttpRequestMessage(
            HttpMethod.Post, $"{Options.BaseUrl.TrimEnd('/')}/chat/completions");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", Options.ApiKey);
        request.Content = JsonContent.Create(payload);
        return request;
    }

    private static string ParseContent(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            var content = json.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            return content ?? string.Empty;
        }
        catch (Exception ex) when (ex is JsonException or KeyNotFoundException
                                       or InvalidOperationException or IndexOutOfRangeException)
        {
            throw new InvalidOperationException(
                "The AI provider returned a response Docsum could not read.", ex);
        }
    }

    private static TimeSpan RetryDelay(HttpResponseMessage response)
    {
        var retryAfter = response.Headers.RetryAfter;
        if (retryAfter?.Delta is { } delta) return delta;
        if (retryAfter?.Date is { } date)
        {
            var wait = date - DateTimeOffset.UtcNow;
            return wait > TimeSpan.Zero ? wait : TimeSpan.Zero;
        }

        return DefaultRetryDelay;
    }

    private string Describe(HttpStatusCode status, string body)
    {
        var detail = ProviderMessage(body);
        var suffix = string.IsNullOrWhiteSpace(detail) ? string.Empty : $" Provider said: {detail}";

        return status switch
        {
            HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden =>
                $"The AI provider rejected the credentials ({(int)status}). " +
                $"Check Ai:ApiKey.{suffix}",
            HttpStatusCode.NotFound =>
                $"The AI model '{Options.Model}' is not available (404). Check Ai:Model — " +
                $"GET {Options.BaseUrl.TrimEnd('/')}/models lists the models your key can use.{suffix}",
            HttpStatusCode.TooManyRequests =>
                $"The AI provider is rate-limiting requests (429). Try again shortly.{suffix}",
            HttpStatusCode.BadRequest =>
                $"The AI provider rejected the request (400).{suffix}",
            _ => $"The AI provider returned {(int)status}.{suffix}"
        };
    }

    private static string ProviderMessage(string body)
    {
        try
        {
            using var json = JsonDocument.Parse(body);
            if (json.RootElement.TryGetProperty("error", out var error) &&
                error.TryGetProperty("message", out var message) &&
                message.GetString() is { Length: > 0 } text)
                return Truncate(text, 300);
        }
        catch (JsonException)
        {
            // not JSON; fall back to the raw body
        }

        return Truncate(body, 300);
    }

    private static string Truncate(string value, int max) =>
        value.Length <= max ? value : value[..max] + "...";
}
