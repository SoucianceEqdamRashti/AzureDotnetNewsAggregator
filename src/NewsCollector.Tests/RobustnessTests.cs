using System.Text.Json;

namespace NewsCollector.Tests;

public sealed class RobustnessTests
{
    [Fact(DisplayName = "Given missing required article fields, when loaded, then reject malformed stored data")]
    public void Given_MissingArticleFields_When_Loaded_Then_Rejects()
    {
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<Article>("{}", JsonStore.Options));
    }

    [Fact(DisplayName = "Given a missing feed link, when parsed, then exclude the entry")]
    public void Given_MissingFeedLink_When_Parsed_Then_Excludes()
    {
        var source = new SourceDefinition("test", "Test", "https://example.com", null, "rss", false, []);
        const string feed = """
            <rss><channel><item><title>Missing URL</title><pubDate>Mon, 16 Nov 2026 08:00:00 GMT</pubDate>
            </item></channel></rss>
            """;

        Assert.Empty(FeedParser.Parse(feed, source));
    }

    [Fact(DisplayName = "Given encoded tracking parameter names, when normalized, then remove tracking")]
    public void Given_EncodedTracking_When_Normalized_Then_Removes()
    {
        Assert.Equal("https://example.com/", ArticlePipeline.NormalizeUrl("https://example.com/?%75tm_source=feed"));
    }
}
