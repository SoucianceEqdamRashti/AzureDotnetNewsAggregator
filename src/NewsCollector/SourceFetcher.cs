using Microsoft.Extensions.Logging;
using System.Net;
using System.Xml;
using System.Text.Json;

namespace NewsCollector;

public sealed class SourceFetcher(IHttpClientFactory clients, ILogger<SourceFetcher> logger, TimeProvider? clock = null)
{
    public async Task<SourceResult> FetchAsync(SourceDefinition source, CancellationToken cancellationToken)
    {
        try
        {
            if (source.Type is not ("rss" or "atom" or "github-release")
                && !(source.Type == "custom" && source.Adapter == "msrc"))
            {
                throw new InvalidDataException("No reliable adapter configured for this source.");
            }

            using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            deadline.CancelAfter(TimeSpan.FromSeconds(90));
            var articles = new List<Article>();
            var next = source.Url;
            if (next is null || ArticlePipeline.NormalizeUrl(next) is null
                || (source.Type == "github-release"
                    && new Uri(next) is not { Scheme: "https", Host: "api.github.com" }))
            {
                throw new InvalidDataException("Source endpoint is missing or invalid for its adapter.");
            }

            for (var page = 0; next is not null; page++)
            {
                if (page >= 20)
                {
                    throw new InvalidDataException("Release pagination exceeded 20 pages; source requires attention.");
                }

                var client = clients.CreateClient(source.Type == "github-release" ? "github" : "sources");
                using var response = await GetWithRetryAsync(client, next, deadline.Token);
                response.EnsureSuccessStatusCode();
                var body = await response.Content.ReadAsStringAsync(deadline.Token);
                var parsed = FeedParser.Parse(body, source);
                articles.AddRange(parsed);
                var cutoff = (clock ?? TimeProvider.System).GetUtcNow().AddDays(-30);
                next = source.Type == "github-release" && parsed.Any(article => article.PublishedAt >= cutoff)
                    ? NextPage(response) : null;
            }

            logger.LogInformation("Source {SourceId} returned {ArticleCount} dated entries", source.Id, articles.Count);
            return new SourceResult(source.Id, true, articles.ToArray(), null);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException
            or InvalidDataException or XmlException or JsonException
            or InvalidOperationException or KeyNotFoundException)
        {
            logger.LogWarning(exception, "Source {SourceId} failed", source.Id);
            return new SourceResult(source.Id, false, [], exception.Message);
        }
    }

    private async Task<HttpResponseMessage> GetWithRetryAsync(
        HttpClient client,
        string url,
        CancellationToken cancellationToken)
    {
        for (var attempt = 0; ; attempt++)
        {
            HttpResponseMessage response;
            try
            {
                response = await client.GetAsync(url, cancellationToken);
            }
            catch (HttpRequestException) when (attempt < 2)
            {
                await Task.Delay(
                    TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                    clock ?? TimeProvider.System,
                    cancellationToken);
                continue;
            }

            if (attempt >= 2
                || (response.StatusCode != HttpStatusCode.TooManyRequests && (int)response.StatusCode < 500))
            {
                return response;
            }

            var delay = response.Headers.RetryAfter?.Delta
                ?? (response.Headers.RetryAfter?.Date - (clock ?? TimeProvider.System).GetUtcNow())
                ?? TimeSpan.FromSeconds(Math.Pow(2, attempt) + Random.Shared.NextDouble());
            response.Dispose();
            if (delay > TimeSpan.FromSeconds(30))
            {
                throw new HttpRequestException("Source requested a retry beyond the per-run budget.");
            }

            await Task.Delay(
                delay > TimeSpan.Zero ? delay : TimeSpan.Zero,
                clock ?? TimeProvider.System,
                cancellationToken);
        }
    }

    private static string? NextPage(HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues("Link", out var links))
        {
            return null;
        }

        var next = links.SelectMany(value => value.Split(','))
            .FirstOrDefault(link => link.Contains("rel=\"next\"", StringComparison.Ordinal));
        if (next is null)
        {
            return null;
        }

        var url = next.Trim().Split(';')[0].Trim('<', '>');
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme != "https" || uri.Host != "api.github.com")
        {
            throw new InvalidDataException("Unsafe release pagination URL.");
        }

        return url;
    }
}
