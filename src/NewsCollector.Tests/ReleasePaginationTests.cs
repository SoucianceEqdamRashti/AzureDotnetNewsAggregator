using System.Net;
using Microsoft.Extensions.Logging.Abstractions;

namespace NewsCollector.Tests;

public sealed class ReleasePaginationTests
{
    [Fact(DisplayName = "Given only old releases on a page, when fetched, then stop requesting older pages")]
    public async Task Given_OldReleasePage_When_Fetched_Then_StopsPagination()
    {
        using var handler = new OldReleaseHandler();
        using var client = new HttpClient(handler);
        var fetcher = new SourceFetcher(new Factory(client), NullLogger<SourceFetcher>.Instance);
        var source = new SourceDefinition(
            "test", "Test", "https://github.com/a/b", "https://api.github.com/repos/a/b/releases",
            "github-release", true, []);

        var result = await fetcher.FetchAsync(source, TestContext.Current.CancellationToken);

        Assert.True(result.Success);
        Assert.Equal(1, handler.RequestCount);
    }

    private sealed class Factory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class OldReleaseHandler : HttpMessageHandler
    {
        public int RequestCount
        {
            get; private set;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            RequestCount++;
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""
                    [{"name":"old","published_at":"2020-01-01T00:00:00Z","html_url":"https://github.com/a/b/releases/tag/old"}]
                    """)
            };
            response.Headers.Add("Link", "<https://api.github.com/repos/a/b/releases?page=2>; rel=\"next\"");
            return Task.FromResult(response);
        }
    }
}
