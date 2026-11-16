using Microsoft.Extensions.Logging;

namespace NewsCollector;

public sealed class IngestionService(SourceFetcher fetcher, TimeProvider clock, ILogger<IngestionService> logger)
{
    public async Task<bool> RunAsync(string dataDirectory, CancellationToken cancellationToken)
    {
        var sources = await JsonStore.ReadAsync<SourceDefinition[]>(
            Path.Combine(dataDirectory, "sources.json"),
            cancellationToken);
        var categories = await JsonStore.ReadAsync<CategoryDefinition[]>(
            Path.Combine(dataDirectory, "categories.json"),
            cancellationToken);
        ConfigurationValidator.Validate(sources, categories);
        var existingPath = Path.Combine(dataDirectory, "articles.json");
        var existing = File.Exists(existingPath)
            ? await JsonStore.ReadAsync<Article[]>(existingPath, cancellationToken) : [];
        var results = await FetchSourcesAsync(sources.Where(source => source.Enabled).ToArray(), cancellationToken);
        if (!results.Any(result => result.Success))
        {
            logger.LogError("No source succeeded. Existing data and metadata have been preserved.");
            return false;
        }

        var incoming = results.Where(result => result.Success).SelectMany(result =>
            Categorizer.Apply(result.Articles, sources.Single(source => source.Id == result.SourceId), categories));
        var now = clock.GetUtcNow();
        var reclassified = existing.GroupBy(article => article.Source).SelectMany(group =>
        {
            var source = sources.FirstOrDefault(source => source.Id == group.Key);
            return source is null ? group.ToArray() : Categorizer.Apply(group, source, categories);
        });
        var articles = ArticlePipeline.Merge(reclassified, incoming, now);
        var changed = await JsonStore.WriteAsync(existingPath, articles, cancellationToken);
        await JsonStore.WriteAsync(Path.Combine(dataDirectory, "metadata.json"), new
        {
            LastSuccessfulIngestion = now,
            ArticleCount = articles.Length,
            SuccessfulSources = results.Count(result => result.Success),
            FailedSources = results.Where(result => !result.Success)
                .Select(result => result.SourceId).Order(StringComparer.Ordinal).ToArray()
        }, cancellationToken);
        logger.LogInformation(
            "Published {ArticleCount} articles. Article data changed: {Changed}",
            articles.Length,
            changed);
        return true;
    }

    public async Task<SourceResult[]> FetchSourcesAsync(SourceDefinition[] sources, CancellationToken cancellationToken)
    {
        using var limiter = new SemaphoreSlim(4);
        return await Task.WhenAll(sources.Select(async source =>
        {
            await limiter.WaitAsync(cancellationToken);
            try
            {
                return await fetcher.FetchAsync(source, cancellationToken);
            }
            finally
            {
                limiter.Release();
            }
        }));
    }
}
