namespace NewsCollector.Tests;

public sealed class MsrcAdapterTests
{
    [Fact(DisplayName = "Given revised security bulletins, when parsed, then retain their initial publication date")]
    public void Given_RevisedBulletins_When_Parsed_Then_UsesInitialDate()
    {
        const string content = """
            {"value":[{"ID":"2026-Nov","DocumentTitle":"November security updates",
            "InitialReleaseDate":"2026-11-10T08:00:00Z","CurrentReleaseDate":"2026-11-16T08:00:00Z",
            "CvrfUrl":"https://api.msrc.microsoft.com/cvrf/v3.0/cvrf/2026-Nov"}]}
            """;
        var source = new SourceDefinition("msrc", "MSRC", "https://msrc.microsoft.com/update-guide", null,
            "custom", false, ["security-identity"]);

        var article = Assert.Single(MsrcAdapter.Parse(content, source));

        Assert.Equal(DateTimeOffset.Parse("2026-11-10T08:00:00Z"), article.PublishedAt);
        Assert.Equal("November security updates", article.Title);
    }
}
