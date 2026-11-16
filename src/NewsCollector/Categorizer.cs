using System.Text.RegularExpressions;

namespace NewsCollector;

public static class Categorizer
{
    public static Article[] Apply(
        IEnumerable<Article> articles,
        SourceDefinition source,
        CategoryDefinition[] categories)
    {
        var keywords = categories.SelectMany(category => category.Keywords).Concat(source.IncludeKeywords ?? []);
        var patterns = keywords.Where(keyword => !string.IsNullOrWhiteSpace(keyword))
            .Distinct(StringComparer.OrdinalIgnoreCase).ToDictionary(keyword => keyword, keyword => new Regex(
                $@"(?<![\p{{L}}\p{{N}}]){Regex.Escape(keyword.Trim())}"
                    + (char.IsLetterOrDigit(keyword.Trim()[^1]) ? @"(?![\p{L}\p{N}])" : ""),
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant,
                TimeSpan.FromMilliseconds(100)), StringComparer.OrdinalIgnoreCase);

        bool Matches(Article article, string keyword)
        {
            return patterns.TryGetValue(keyword, out var pattern)
                && (pattern.IsMatch(article.Title) || pattern.IsMatch(article.Summary));
        }

        return articles.Where(article => source.IncludeKeywords is not { Length: > 0 }
                || source.IncludeKeywords.Any(keyword => Matches(article, keyword)))
            .Select(article => article with
            {
                Categories = source.DefaultCategories.Concat(categories
                        .Where(category => category.Keywords.Any(keyword => Matches(article, keyword)))
                        .Select(category => category.Id))
                    .DefaultIfEmpty("general-technology").Distinct(StringComparer.Ordinal)
                    .Order(StringComparer.Ordinal).ToArray()
            }).ToArray();
    }

}
