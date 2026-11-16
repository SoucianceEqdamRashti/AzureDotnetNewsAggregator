using NewsCollector;

namespace NewsCollector.Tests;

public sealed class ArticlePipelineTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.Parse("2026-11-16T12:00:00Z");

    [Fact(DisplayName = "Given repeated query parameters, when normalized, then preserve their value order")]
    public void Given_RepeatedParameters_When_Normalized_Then_PreservesValueOrder()
    {
        Assert.Equal(
            "https://example.com/?a=2&a=1&b=2",
            ArticlePipeline.NormalizeUrl("https://example.com/?b=2&a=2&a=1"));
    }

    [Fact(DisplayName = "Given tracked duplicates, when merged, then combine categories under a stable URL")]
    public void Given_TrackedDuplicates_When_Merged_Then_CombinesCategories()
    {
        var existing = new Article("old", "Title", "https://example.com/post?utm_source=rss#top", "a",
            Now.AddDays(-1), "Summary", ["azure"]);
        var incoming = existing with
        {
            Url = "https://example.com/post",
            Categories = ["ai-agents"]
        };

        var result = ArticlePipeline.Merge([existing], [incoming], Now);

        var article = Assert.Single(result);
        Assert.Equal("https://example.com/post", article.Url);
        Assert.Equal(new[] { "ai-agents", "azure" }, article.Categories);
        Assert.NotEqual("old", article.Id);
    }

    [Fact(DisplayName = "Given publication boundaries, when merged, then retain only the inclusive rolling window")]
    public void Given_PublicationBoundaries_When_Merged_Then_RetainsWindow()
    {
        var dates = new[] { Now.AddDays(-30), Now.AddDays(-30).AddTicks(-1), Now.AddTicks(1), Now };
        var articles = dates.Select((date, index) => new Article("", "Title", $"https://example.com/{index}",
            "a", date, "", []));

        var result = ArticlePipeline.Merge([], articles, Now);

        Assert.Equal(new[] { Now, Now.AddDays(-30) }, result.Select(article => article.PublishedAt));
    }

    [Theory(DisplayName = "Given unsafe article URLs, when normalized, then reject them")]
    [InlineData("javascript:alert(1)")]
    [InlineData("file:///tmp/article")]
    [InlineData("not a url")]
    public void Given_UnsafeUrls_When_Normalized_Then_Rejects(string url)
    {
        Assert.Null(ArticlePipeline.NormalizeUrl(url));
    }

    [Fact(DisplayName = "Given meaningful query parameters, when normalized, then preserve them")]
    public void Given_QueryParameters_When_Normalized_Then_PreservesMeaning()
    {
        Assert.Equal("https://example.com/?a=1&b=2", ArticlePipeline.NormalizeUrl(
            "https://EXAMPLE.com/?b=2&utm_medium=feed&a=1"));
    }
}
