using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NewsCollector;

var services = new ServiceCollection();
services.AddLogging(builder => builder
    .AddSimpleConsole(options => options.SingleLine = true)
    .AddFilter("System.Net.Http.HttpClient", LogLevel.Warning));
services.AddSingleton(TimeProvider.System);
services.AddTransient<SourceFetcher>();
services.AddTransient<IngestionService>();
services.AddHttpClient("sources", client =>
{
    client.Timeout = TimeSpan.FromSeconds(25);
    client.MaxResponseContentBufferSize = 10_000_000;
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AzureDotnetNewsAggregator/1.0");
});
services.AddHttpClient("github", client =>
{
    client.Timeout = TimeSpan.FromSeconds(25);
    client.MaxResponseContentBufferSize = 10_000_000;
    client.DefaultRequestHeaders.UserAgent.ParseAdd("AzureDotnetNewsAggregator/1.0");
    client.DefaultRequestHeaders.Accept.ParseAdd("application/vnd.github+json");
    var token = Environment.GetEnvironmentVariable("GITHUB_TOKEN");
    if (!string.IsNullOrWhiteSpace(token))
    {
        client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue(
            "Bearer",
            token);
    }
});
await using var provider = services.BuildServiceProvider();
var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Collector");
using var cancellation = new CancellationTokenSource();
Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

try
{
    var command = args.FirstOrDefault() ?? "ingest";
    var dataDirectory = Path.GetFullPath(args.ElementAtOrDefault(1) ?? "data");
    var service = provider.GetRequiredService<IngestionService>();
    if (command == "validate-sources")
    {
        var sources = await JsonStore.ReadAsync<SourceDefinition[]>(
            Path.Combine(dataDirectory, "sources.json"),
            cancellation.Token);
        var categories = await JsonStore.ReadAsync<CategoryDefinition[]>(
            Path.Combine(dataDirectory, "categories.json"),
            cancellation.Token);
        ConfigurationValidator.Validate(sources, categories);
        var includeDisabled = args.Contains("--include-disabled", StringComparer.Ordinal);
        foreach (var source in sources.Where(source => !source.Enabled))
        {
            logger.LogInformation("Disabled {SourceId}: {Reason}", source.Id, source.DisabledReason);
        }

        var results = await service.FetchSourcesAsync(sources.Where(source => source.Enabled
            || (includeDisabled && source.Url is not null)).ToArray(), cancellation.Token);
        foreach (var result in results)
        {
            logger.LogInformation("Validation {SourceId}: {Status}; {Count} dated entries; {Error}",
                result.SourceId, result.Success ? "PASS" : "FAIL", result.Articles.Length, result.Error);
        }

        return results.Length > 0 && results.All(result => result.Success) ? 0 : 1;
    }

    if (command != "ingest")
    {
        logger.LogError("Usage: NewsCollector [ingest|validate-sources] [data-directory] [--include-disabled]");
        return 2;
    }

    return await service.RunAsync(dataDirectory, cancellation.Token) ? 0 : 1;
}
catch (OperationCanceledException)
{
    logger.LogWarning("Ingestion cancelled.");
    return 130;
}
catch (Exception exception)
{
    logger.LogError(exception, "Collector failed. Inspect configuration and the last published data before retrying.");
    return 1;
}
