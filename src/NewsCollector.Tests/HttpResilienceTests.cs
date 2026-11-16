using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging.Abstractions;

namespace NewsCollector.Tests;

public sealed class HttpResilienceTests
{
    private static readonly SourceDefinition Source = new("test", "Test", "https://example.com",
        "https://example.com/feed", "rss", true, []);

    [Fact(DisplayName = "Given transient throttling, when retry is allowed immediately, then recover the source")]
    public async Task Given_Throttling_When_Retried_Then_Recovers()
    {
        using var handler = new ThrottledHandler();
        using var client = new HttpClient(handler);
        var fetcher = new SourceFetcher(new Factory(client), NullLogger<SourceFetcher>.Instance);

        var result = await fetcher.FetchAsync(Source, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(2, handler.RequestCount);
    }

    [Fact(DisplayName = "Given caller cancellation, when fetching, then propagate cancellation")]
    public async Task Given_Cancellation_When_Fetching_Then_Propagates()
    {
        using var handler = new ThrottledHandler();
        using var client = new HttpClient(handler);
        var fetcher = new SourceFetcher(new Factory(client), NullLogger<SourceFetcher>.Instance);
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fetcher.FetchAsync(Source, cancellation.Token));
    }

    private sealed class Factory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class ThrottledHandler : HttpMessageHandler
    {
        public int RequestCount
        {
            get; private set;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();
            RequestCount++;
            var statusCode = RequestCount == 1 ? HttpStatusCode.TooManyRequests : HttpStatusCode.OK;
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("<rss><channel /></rss>")
            };
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.Zero);
            return Task.FromResult(response);
        }
    }
}
