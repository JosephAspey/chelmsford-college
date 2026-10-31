using System.Net;
using System.Text.RegularExpressions;
using Ganss.Xss;

namespace Prototype.Core.Courses;

/// <summary>
/// Cleans the HTML the college stores in its course tables. The current site's editor has left inline styles,
/// Bootstrap 3 classes, icon spans and empty paragraphs; the new site shows only the structure (paragraphs,
/// lists, emphasis and links) in its own styles.
/// </summary>
public static partial class CourseHtml
{
    private const char NoBreakSpace = ' ';
    private static readonly HtmlSanitizer Sanitizer = CreateSanitizer();

    /// <summary>Cleaned HTML, or null when nothing readable is left.</summary>
    public static string? Clean(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return null;

        // Scripts and styles go entirely; other unknown tags keep their text
        var cleaned = Sanitizer.Sanitize(ScriptOrStyle().Replace(html, ""))
            .Replace(NoBreakSpace, ' ')
            .Replace("&nbsp;", " ");
        cleaned = EmptyParagraph().Replace(cleaned, "");
        cleaned = SpaceBeforeClose().Replace(cleaned, "$1");
        cleaned = cleaned.Trim();

        return string.IsNullOrWhiteSpace(PlainText(cleaned)) ? null : cleaned;
    }

    /// <summary>Joins several fields into one section, skipping the empty ones.</summary>
    public static string? Join(params string?[] html)
    {
        var parts = html.Select(Clean).Where(h => h is not null).ToList();
        return parts.Count == 0 ? null : string.Join("\n", parts);
    }

    public static string PlainText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html)) return "";
        var text = WebUtility.HtmlDecode(Tag().Replace(html, " "));
        return Whitespace().Replace(text.Replace(NoBreakSpace, ' '), " ").Trim();
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var options = new HtmlSanitizerOptions
        {
            AllowedTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "p", "ul", "ol", "li", "strong", "b", "em", "i", "a", "br", "h3", "h4" },
            AllowedAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href" },
            AllowedSchemes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "http", "https", "mailto", "tel" },
            UriAttributes = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "href" }
        };
        return new HtmlSanitizer(options) { KeepChildNodes = true };
    }

    [GeneratedRegex(@"<p>\s*(?:<br\s*/?>\s*)*</p>", RegexOptions.IgnoreCase)]
    private static partial Regex EmptyParagraph();

    [GeneratedRegex(@"\s+(</(?:p|li|strong|em|a)>)", RegexOptions.IgnoreCase)]
    private static partial Regex SpaceBeforeClose();

    [GeneratedRegex(@"<(script|style)\b[^>]*>.*?</\1\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptOrStyle();

    [GeneratedRegex("<[^>]+>")]
    private static partial Regex Tag();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
