namespace NewsCollector;

public sealed record SourceResult(string SourceId, bool Success, Article[] Articles, string? Error);
