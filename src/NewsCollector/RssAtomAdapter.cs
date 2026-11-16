using System.Xml;
using System.Xml.Linq;

namespace NewsCollector;

public static class RssAtomAdapter
{
    public static Article[] Parse(string content, SourceDefinition source)
    {
        using var input = new StringReader(content);
        using var reader = XmlReader.Create(input, new XmlReaderSettings
        {
            DtdProcessing = DtdProcessing.Prohibit,
            XmlResolver = null,
            MaxCharactersInDocument = 10_000_000
        });
        var document = XDocument.Load(reader);
        if (document.Root?.Name.LocalName is not ("rss" or "feed" or "RDF"))
        {
            throw new InvalidDataException("Response is not an RSS or Atom feed.");
        }

        var articles = new List<Article>();
        foreach (var entry in document.Descendants().Where(element => element.Name.LocalName is "item" or "entry"))
        {
            var date = Value(entry, "published") ?? Value(entry, "pubDate") ?? Value(entry, "date");
            if (!FeedText.TryDate(date, out var publishedAt))
            {
                continue;
            }

            var link = entry.Elements().FirstOrDefault(element => element.Name.LocalName == "link"
                && (element.Attribute("rel")?.Value is null or "alternate"));
            var url = link?.Attribute("href")?.Value ?? link?.Value ?? "";
            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            if (Uri.TryCreate(new Uri(source.Homepage), url, out var resolved))
            {
                url = resolved.AbsoluteUri;
            }

            articles.Add(new Article("", FeedText.Plain(Value(entry, "title") ?? ""), url, source.Id, publishedAt,
                FeedText.Plain(Value(entry, "summary") ?? Value(entry, "description") ?? Value(entry, "content") ?? ""),
                source.DefaultCategories));
        }

        return articles.ToArray();
    }

    private static string? Value(XElement element, string name)
    {
        return element.Elements().FirstOrDefault(child => child.Name.LocalName == name)?.Value;
    }

}
