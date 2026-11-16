namespace NewsCollector;

public static class FeedParser
{
    public static Article[] Parse(string content, SourceDefinition source)
    {
        return source.Type switch
        {
            "github-release" => GitHubReleaseAdapter.Parse(content, source),
            "custom" when source.Adapter == "msrc" => MsrcAdapter.Parse(content, source),
            "rss" or "atom" => RssAtomAdapter.Parse(content, source),
            _ => throw new InvalidDataException("Unsupported ingestion adapter.")
        };
    }

}
