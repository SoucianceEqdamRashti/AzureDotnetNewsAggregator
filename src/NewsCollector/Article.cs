namespace NewsCollector;

public sealed record Article(
    string Id,
    string Title,
    string Url,
    string Source,
    DateTimeOffset PublishedAt,
    string Summary,
    string[] Categories);
