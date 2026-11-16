using System.Security.Cryptography;
using System.Text;

namespace NewsCollector;

public static class ArticlePipeline
{
    public static string? NormalizeUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || uri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(uri.UserInfo))
        {
            return null;
        }

        var builder = new UriBuilder(uri) { Fragment = "" };
        builder.Query = string.Join("&", uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
            .Where(part => !IsTrackingParameter(part.Split('=')[0]))
            .OrderBy(part => part.Split('=')[0], StringComparer.Ordinal));

        return builder.Uri.AbsoluteUri;
    }

    public static Article[] Merge(IEnumerable<Article> existing, IEnumerable<Article> incoming, DateTimeOffset now)
    {
        var normalized = existing.Concat(incoming)
            .Where(article => !string.IsNullOrWhiteSpace(article.Title)
                && article.PublishedAt >= now.AddDays(-30) && article.PublishedAt <= now)
            .Select(article => (Article: article, Url: NormalizeUrl(article.Url)))
            .Where(item => item.Url is not null);

        return normalized.GroupBy(item => item.Url!, StringComparer.Ordinal)
            .Select(group =>
            {
                var selected = group.Last().Article;
                var categories = group.SelectMany(item => item.Article.Categories).Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal).ToArray();

                return selected with
                {
                    Id = Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(group.Key))),
                    Url = group.Key,
                    PublishedAt = selected.PublishedAt.ToUniversalTime(),
                    Categories = categories
                };
            })
            .OrderByDescending(article => article.PublishedAt)
            .ThenBy(article => article.Id, StringComparer.Ordinal)
            .ToArray();
    }

    private static bool IsTrackingParameter(string key)
    {
        return key.StartsWith("utm_", StringComparison.OrdinalIgnoreCase)
            || key.Equals("fbclid", StringComparison.OrdinalIgnoreCase)
            || key.Equals("gclid", StringComparison.OrdinalIgnoreCase);
    }
}
