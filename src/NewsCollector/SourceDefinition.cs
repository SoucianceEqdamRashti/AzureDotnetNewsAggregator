namespace NewsCollector;

public sealed record SourceDefinition(
    string Id,
    string Name,
    string Homepage,
    string? Url,
    string Type,
    bool Enabled,
    string[] DefaultCategories,
    string[]? IncludeKeywords = null,
    string? DisabledReason = null,
    string? Adapter = null);
