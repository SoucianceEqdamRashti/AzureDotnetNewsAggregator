using System.Net;
using Microsoft.Extensions.Logging.Abstractions;

namespace NewsCollector.Tests;

public sealed class IngestionTests
{
    [Theory(DisplayName = "Given partial success, when ingested, then merge retained articles using current source rules")]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Given_PartialSuccess_When_Ingested_Then_MergesAndUpdatesMetadata(bool filterExisting)
    {
        var directory = Path.Combine(Path.GetTempPath(), $"news-{Guid.NewGuid():N}");
        var token = TestContext.Current.CancellationToken;
        var now = DateTimeOffset.Parse("2026-11-16T12:00:00Z");
        try
        {
            var source = new SourceDefinition("good", "Good", "https://example.com", "https://example.com/good",
                "rss", true, ["azure"]);
            await JsonStore.WriteAsync(Path.Combine(directory, "sources.json"), new[]
            {
                source, source with
                {
                    Id = "bad",
                    Url = "https://example.com/bad",
                    IncludeKeywords = filterExisting ? ["Azure"] : null
                }
            }, token);
            await JsonStore.WriteAsync(Path.Combine(directory, "categories.json"), new[]
            {
                new CategoryDefinition("azure", "Azure", ["Azure"])
            }, token);
            var existing = new Article(
                "", "Existing", "https://example.com/old", "bad", now.AddDays(-1), "", ["azure"]);
            await JsonStore.WriteAsync(Path.Combine(directory, "articles.json"), new[]
            {
                existing, existing with { Url = "https://example.com/expired", PublishedAt = now.AddDays(-31) }
            }, token);
            using var client = new HttpClient(new PartialHandler());
            var fetcher = new SourceFetcher(new Factory(client), NullLogger<SourceFetcher>.Instance);
            var service = new IngestionService(fetcher, new FixedClock(now), NullLogger<IngestionService>.Instance);

            Assert.True(await service.RunAsync(directory, token));

            var articles = await JsonStore.ReadAsync<Article[]>(Path.Combine(directory, "articles.json"), token);
            var expectedTitles = filterExisting ? new[] { "New Azure" } : ["New Azure", "Existing"];
            Assert.Equal(expectedTitles, articles.Select(article => article.Title));
            using var metadata = System.Text.Json.JsonDocument.Parse(
                await File.ReadAllTextAsync(Path.Combine(directory, "metadata.json"), token));
            Assert.Equal(now, metadata.RootElement.GetProperty("lastSuccessfulIngestion").GetDateTimeOffset());
            var failedSource = Assert.Single(metadata.RootElement.GetProperty("failedSources").EnumerateArray());
            Assert.Equal("bad", failedSource.GetString());
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact(DisplayName = "Given all sources fail, when ingested, then preserve the existing dataset and timestamp")]
    public async Task Given_TotalFailure_When_Ingested_Then_PreservesData()
    {
        var directory = Path.Combine(Path.GetTempPath(), $"news-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var token = TestContext.Current.CancellationToken;
        try
        {
            await JsonStore.WriteAsync(Path.Combine(directory, "sources.json"), new[]
            {
                new SourceDefinition("bad", "Bad", "https://example.com", "https://example.com/feed", "rss", true, [])
            }, token);
            await JsonStore.WriteAsync(
                Path.Combine(directory, "categories.json"),
                Array.Empty<CategoryDefinition>(),
                token);
            await File.WriteAllTextAsync(Path.Combine(directory, "articles.json"), "[]", token);
            await File.WriteAllTextAsync(Path.Combine(directory, "metadata.json"), "{}", token);
            using var client = new HttpClient(new FailingHandler());
            var fetcher = new SourceFetcher(new Factory(client), NullLogger<SourceFetcher>.Instance);
            var service = new IngestionService(fetcher, TimeProvider.System, NullLogger<IngestionService>.Instance);

            Assert.False(await service.RunAsync(directory, token));
            Assert.Equal("[]", await File.ReadAllTextAsync(Path.Combine(directory, "articles.json"), token));
            Assert.Equal("{}", await File.ReadAllTextAsync(Path.Combine(directory, "metadata.json"), token));
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }

    private sealed class Factory(HttpClient client) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) => client;
    }

    private sealed class FailingHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
        }
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class PartialHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(new HttpResponseMessage(request.RequestUri!.AbsolutePath == "/good"
                ? HttpStatusCode.OK : HttpStatusCode.NotFound)
            {
                Content = new StringContent("""
                    <rss><channel><item><title>New Azure</title><link>https://example.com/new</link>
                    <pubDate>Mon, 16 Nov 2026 08:00:00 GMT</pubDate></item></channel></rss>
                    """)
            });
        }
    }
}
