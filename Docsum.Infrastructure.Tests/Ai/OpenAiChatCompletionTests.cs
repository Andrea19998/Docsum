using System.Net;
using Docsum.Application.Abstractions;
using Docsum.Infrastructure.Ai;
using Microsoft.Extensions.Options;

namespace Docsum.Infrastructure.Tests.Ai;

public class OpenAiChatCompletionTests
{
    private const string Model = "test-model";
    private const string BaseUrl = "https://example.test/v1";

    private static readonly IReadOnlyList<ChatTurn> Messages = [new("user", "What is the deadline?")];

    private static OpenAiChatCompletion Create(HttpMessageHandler handler, AiOptions? options = null) =>
        new(new HttpClient(handler), Options.Create(options ?? new AiOptions
        {
            BaseUrl = BaseUrl,
            Model = Model,
            ApiKey = "test-key"
        }));

    private static Task<string> Complete(OpenAiChatCompletion sut) =>
        sut.CompleteAsync("system", Messages, CancellationToken.None);

    [Fact]
    public async Task CompleteAsync_ReturnsContent_OnSuccess()
    {
        var stub = new StubHandler(StubHandler.Json(HttpStatusCode.OK, StubHandler.SuccessBody));

        var result = await Complete(Create(stub));

        Assert.Equal("Pay 260 EUR by 15.12.2026.", result);
        Assert.Equal(1, stub.Calls);
    }

    [Fact]
    public async Task CompleteAsync_RetriesOnce_WhenRateLimitedWithShortDelay()
    {
        var stub = new StubHandler(
            StubHandler.RateLimited(TimeSpan.Zero),
            StubHandler.Json(HttpStatusCode.OK, StubHandler.SuccessBody));

        var result = await Complete(Create(stub));

        Assert.Equal("Pay 260 EUR by 15.12.2026.", result);
        Assert.Equal(2, stub.Calls);
    }

    [Fact]
    public async Task CompleteAsync_DoesNotRetry_WhenRetryAfterIsTooLong()
    {
        var stub = new StubHandler(StubHandler.RateLimited(TimeSpan.FromSeconds(60)));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Complete(Create(stub)));

        Assert.Contains("429", ex.Message);
        Assert.Contains("Rate limit reached", ex.Message);
        Assert.Equal(1, stub.Calls);
    }

    [Fact]
    public async Task CompleteAsync_GivesUp_AfterSecondRateLimit()
    {
        var stub = new StubHandler(
            StubHandler.RateLimited(TimeSpan.Zero),
            StubHandler.RateLimited(TimeSpan.Zero));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Complete(Create(stub)));

        Assert.Contains("429", ex.Message);
        Assert.Equal(2, stub.Calls);
    }

    [Fact]
    public async Task CompleteAsync_ExplainsBadCredentials()
    {
        var stub = new StubHandler(StubHandler.Json(HttpStatusCode.Unauthorized,
            """{"error":{"message":"Invalid API Key"}}"""));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Complete(Create(stub)));

        Assert.Contains("401", ex.Message);
        Assert.Contains("Ai:ApiKey", ex.Message);
        Assert.Contains("Invalid API Key", ex.Message);
        Assert.Equal(1, stub.Calls);
    }

    [Fact]
    public async Task CompleteAsync_ExplainsUnknownModel()
    {
        var stub = new StubHandler(StubHandler.Json(HttpStatusCode.NotFound,
            """{"error":{"message":"model not found"}}"""));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Complete(Create(stub)));

        Assert.Contains(Model, ex.Message);
        Assert.Contains($"{BaseUrl}/models", ex.Message);
    }

    [Theory]
    [InlineData("not json at all")]
    [InlineData("""{"id":"x"}""")]
    [InlineData("""{"choices":[]}""")]
    public async Task CompleteAsync_Throws_WhenSuccessBodyIsUnreadable(string body)
    {
        var stub = new StubHandler(StubHandler.Json(HttpStatusCode.OK, body));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Complete(Create(stub)));

        Assert.Contains("could not read", ex.Message);
    }

    [Fact]
    public async Task CompleteAsync_FallsBackToRawBody_WhenErrorIsNotJson()
    {
        var stub = new StubHandler(StubHandler.Json(HttpStatusCode.InternalServerError,
            "Bad gateway from upstream"));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Complete(Create(stub)));

        Assert.Contains("500", ex.Message);
        Assert.Contains("Bad gateway from upstream", ex.Message);
    }

    [Fact]
    public async Task CompleteAsync_ReportsTimeout_WhenProviderDoesNotRespond()
    {
        var sut = Create(new TimeoutHandler());

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Complete(sut));

        Assert.Contains("did not respond within 120 seconds", ex.Message);
    }

    [Fact]
    public async Task CompleteAsync_PropagatesCancellation_WhenCallerCancels()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var sut = Create(new TimeoutHandler());

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => sut.CompleteAsync("system", Messages, cts.Token));
    }

    [Theory]
    [InlineData("", Model, "key")]
    [InlineData(BaseUrl, "", "key")]
    [InlineData(BaseUrl, Model, " ")]
    public void IsConfigured_IsFalse_WhenAnySettingIsMissing(string baseUrl, string model, string apiKey)
    {
        var sut = Create(new StubHandler(), new AiOptions { BaseUrl = baseUrl, Model = model, ApiKey = apiKey });

        Assert.False(sut.IsConfigured);
    }

    [Fact]
    public void IsConfigured_IsTrue_WhenAllSettingsArePresent()
    {
        Assert.True(Create(new StubHandler()).IsConfigured);
    }

    /// <summary>Simulates HttpClient's own timeout, which surfaces as TaskCanceledException.</summary>
    private sealed class TimeoutHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            throw new TaskCanceledException("The request was canceled due to the configured HttpClient.Timeout.");
    }
}
