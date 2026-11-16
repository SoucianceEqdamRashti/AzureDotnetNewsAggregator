using System.Globalization;
using System.Text.Json;

namespace NewsCollector;

public static class MsrcAdapter
{
    public static Article[] Parse(string content, SourceDefinition source)
    {
        using var document = JsonDocument.Parse(content);
        var articles = new List<Article>();
        foreach (var update in document.RootElement.GetProperty("value").EnumerateArray())
        {
            var date = update.GetProperty("InitialReleaseDate").GetString();
            if (!DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var publishedAt))
            {
                continue;
            }

            articles.Add(new Article("", update.GetProperty("DocumentTitle").GetString() ?? "",
                update.GetProperty("CvrfUrl").GetString() ?? "", source.Id, publishedAt,
                $"Microsoft security bulletin {update.GetProperty("ID").GetString()}.", source.DefaultCategories));
        }

        return articles.ToArray();
    }
}
