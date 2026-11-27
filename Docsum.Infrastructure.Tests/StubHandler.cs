using System.Net;
using System.Text;

namespace Docsum.Infrastructure.Tests;

/// <summary>Replays canned responses and counts how many times it was called.</summary>
internal sealed class StubHandler(params HttpResponseMessage[] responses) : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new(responses);

    public int Calls { get; private set; }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        Calls++;
        if (_responses.Count == 0)
            throw new InvalidOperationException("StubHandler received more calls than responses.");

        return Task.FromResult(_responses.Dequeue());
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string json) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    public static HttpResponseMessage RateLimited(TimeSpan retryAfter)
    {
        var response = Json(HttpStatusCode.TooManyRequests,
            """{"error":{"message":"Rate limit reached for requests"}}""");
        response.Headers.RetryAfter = new System.Net.Http.Headers.RetryConditionHeaderValue(retryAfter);
        return response;
    }

    public const string SuccessBody =
        """{"choices":[{"message":{"role":"assistant","content":"Pay 260 EUR by 15.12.2026."}}]}""";
}
