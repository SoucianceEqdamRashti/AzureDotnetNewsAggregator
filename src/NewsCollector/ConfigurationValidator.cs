namespace NewsCollector;

public static class ConfigurationValidator
{
    public static void Validate(SourceDefinition[] sources, CategoryDefinition[] categories)
    {
        if (sources.Select(source => source.Id).Distinct(StringComparer.Ordinal).Count() != sources.Length
            || categories.Select(category => category.Id).Distinct(StringComparer.Ordinal).Count() != categories.Length)
        {
            throw new InvalidDataException("Source and category IDs must be unique.");
        }

        var categoryIds = categories.Select(category => category.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var source in sources)
        {
            if (string.IsNullOrWhiteSpace(source.Id) || string.IsNullOrWhiteSpace(source.Name)
                || ArticlePipeline.NormalizeUrl(source.Homepage) is null
                || source.DefaultCategories.Any(category => !categoryIds.Contains(category))
                || (source.Enabled && (source.Url is null || ArticlePipeline.NormalizeUrl(source.Url) is null
                    || (source.Type is not ("rss" or "atom" or "github-release")
                        && !(source.Type == "custom" && source.Adapter == "msrc")))))
            {
                throw new InvalidDataException($"Invalid configuration for source '{source.Id}'.");
            }
        }
    }
}
