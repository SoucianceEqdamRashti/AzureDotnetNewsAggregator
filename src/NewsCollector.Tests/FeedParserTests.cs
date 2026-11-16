namespace NewsCollector.Tests;

public sealed class FeedParserTests
{
    [Fact(DisplayName = "Given AI topic filtering, when a title mentions email, then exclude the unrelated article")]
    public void Given_AiTopicFilter_When_TitleMentionsEmail_Then_ExcludesUnrelated()
    {
        var article = new Article("", "Email prices rise", "https://example.com/a", "test",
            DateTimeOffset.Parse("2026-11-16T08:00:00Z"), "", []);

        Assert.Empty(Categorizer.Apply([article], Source with
        {
            IncludeKeywords = ["AI"]
        }, []));
    }

    private static readonly SourceDefinition Source = new(
        "test", "Test", "https://example.com", "https://example.com/feed", "rss", true, ["azure"]);

    [Theory(DisplayName = "Given RSS or Atom, when parsed, then use publication dates and plain summaries")]
    [InlineData("<rss><channel><item><title>Azure news</title><link>https://example.com/a</link><pubDate>Mon, 16 Nov 2026 08:00:00 GMT</pubDate><description>&lt;p&gt;Hello&lt;/p&gt;</description></item></channel></rss>")]
    [InlineData("<feed xmlns='http://www.w3.org/2005/Atom'><entry><title>Azure news</title><link href='https://example.com/a'/><published>2026-11-16T08:00:00Z</published><summary>&lt;p&gt;Hello&lt;/p&gt;</summary></entry></feed>")]
    public void Given_Feed_When_Parsed_Then_Normalizes(string content)
    {
        var article = Assert.Single(FeedParser.Parse(content, Source));

        Assert.Equal("Azure news", article.Title);
        Assert.Equal("Hello", article.Summary);
        Assert.Equal(DateTimeOffset.Parse("2026-11-16T08:00:00Z"), article.PublishedAt);
    }

    [Fact(DisplayName = "Given undated entries, when parsed, then exclude them")]
    public void Given_UndatedEntries_When_Parsed_Then_Excludes()
    {
        Assert.Empty(FeedParser.Parse("<rss><channel><item><title>Title</title></item></channel></rss>", Source));
    }

    [Fact(DisplayName = "Given GitHub releases, when parsed, then exclude drafts and undated entries")]
    public void Given_Releases_When_Parsed_Then_ExcludesUnpublished()
    {
        const string content = """
            [{"name":"v1", "html_url":"https://github.com/a/b/releases/tag/v1", "published_at":"2026-11-16T08:00:00Z", "body":"Release", "draft":false},
             {"name":"draft", "draft":true}, {"name":"undated"}]
            """;

        var article = Assert.Single(FeedParser.Parse(content, Source with
        {
            Type = "github-release"
        }));

        Assert.Equal("v1", article.Title);
    }

    [Fact(DisplayName = "Given malformed content, when parsed, then report failure")]
    public void Given_MalformedContent_When_Parsed_Then_Throws()
    {
        Assert.Throws<InvalidDataException>(() => FeedParser.Parse("<html>not a feed</html>", Source));
    }

    [Fact(DisplayName = "Given broad sources, when categorized, then filter topics and assign multiple categories")]
    public void Given_BroadSources_When_Categorized_Then_FiltersAndAssigns()
    {
        var article = new Article("", "Azure agent news", "https://example.com/a", "test",
            DateTimeOffset.Parse("2026-11-16T08:00:00Z"), "", []);
        var unrelated = article with
        {
            Title = "Cooking"
        };
        var categories = new[] { new CategoryDefinition("ai-agents", "AI & Agents", ["agent"]) };

        var result = Categorizer.Apply([article, unrelated], Source with
        {
            IncludeKeywords = ["azure"]
        }, categories);

        Assert.Equal(new[] { "ai-agents", "azure" }, Assert.Single(result).Categories);
    }
}
