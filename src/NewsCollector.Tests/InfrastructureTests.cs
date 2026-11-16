using System.Net;
using Microsoft.Extensions.Logging.Abstractions;

namespace NewsCollector.Tests;

public sealed class InfrastructureTests
{
    [Fact(DisplayName = "Given a failing source, when fetched independently, then other sources succeed")]
    public async Task Given_FailingSource_When_Fetched_Then_OtherSourcesSucceed()
    {
        using var client = new HttpClient(new StubHandler());
        var fetcher = new SourceFetcher(new StubFactory(client), NullLogger<SourceFetcher>.Instance);
        var source = new SourceDefinition("test", "Test", "https://example.com", "https://example.com/good",
            "rss", true, []);

        var results = await Task.WhenAll(
            fetcher.FetchAsync(source, TestContext.Current.CancellationToken),
            fetcher.FetchAsync(source with
            {
                Url = "https://example.com/bad"
            }, TestContext.Current.CancellationToken));

        Assert.True(results[0].Success);
        Assert.False(results[1].Success);
        Assert.NotNull(results[1].Error);
    }

    [Fact(DisplayName = "Given identical JSON, when persisted twice, then leave the second write unchanged")]
    public async Task Given_IdenticalJson_When_WrittenTwice_Then_SkipsSecondWrite()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"news-{Guid.NewGuid():N}");
        var path = Path.Combine(directory, "articles.json");
        try
        {
            Assert.True(await JsonStore.WriteAsync(path, new[] { "test" }, TestContext.Current.CancellationToken));
            var timestamp = File.GetLastWriteTimeUtc(path);

            Assert.False(await JsonStore.WriteAsync(path, new[] { "test" }, TestContext.Current.CancellationToken));
            Assert.Equal(timestamp, File.GetLastWriteTimeUtc(path));
            Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    private sealed class StubFactory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(request.RequestUri!.AbsolutePath == "/bad"
                ? HttpStatusCode.NotFound : HttpStatusCode.OK)
            {
                Content = new StringContent("<rss><channel /></rss>")
            });
        }
    }
}
