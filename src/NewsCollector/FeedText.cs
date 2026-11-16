using System.Globalization;
using System.Net;
using System.Text.RegularExpressions;

namespace NewsCollector;

internal static partial class FeedText
{
    public static bool TryDate(string? value, out DateTimeOffset date)
    {
        return DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date);
    }

    public static string Plain(string value)
    {
        var text = WebUtility.HtmlDecode(Tags().Replace(value, " "));
        text = Whitespace().Replace(text, " ").Trim();
        return text.Length <= 1000 ? text : text[..1000];
    }

    [GeneratedRegex("<[^>]*>")]
    private static partial Regex Tags();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
