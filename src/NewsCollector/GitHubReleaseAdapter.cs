using System.Text.Json;

namespace NewsCollector;

public static class GitHubReleaseAdapter
{
    public static Article[] Parse(string content, SourceDefinition source)
    {
        using var document = JsonDocument.Parse(content);
        var articles = new List<Article>();
        foreach (var release in document.RootElement.EnumerateArray())
        {
            if ((release.TryGetProperty("draft", out var draft) && draft.GetBoolean())
                || !FeedText.TryDate(JsonString(release, "published_at"), out var publishedAt))
            {
                continue;
            }

            articles.Add(new Article(
                "",
                FeedText.Plain(JsonString(release, "name") ?? JsonString(release, "tag_name") ?? ""),
                JsonString(release, "html_url") ?? "",
                source.Id,
                publishedAt,
                FeedText.Plain(JsonString(release, "body") ?? ""),
                source.DefaultCategories));
        }

        return articles.ToArray();
    }

    private static string? JsonString(JsonElement element, string name)
    {
        return element.TryGetProperty(name, out var value) && value.ValueKind == JsonValueKind.String
            ? value.GetString() : null;
    }

}
