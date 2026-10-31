using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Prototype.Core.Courses;

/// <summary>Turns the codes and free text in the college's tables into what learners read.</summary>
public static partial class CourseLabels
{
    private static readonly CultureInfo UkCulture = CultureInfo.GetCultureInfo("en-GB");

    public static string Label(this LevelBand band) => band switch
    {
        LevelBand.Entry => "Entry level",
        LevelBand.Level1 => "Level 1",
        LevelBand.Level2 => "Level 2",
        LevelBand.Level3 => "Level 3",
        LevelBand.Level4Plus => "Level 4 and above",
        LevelBand.Supported => "Supported learning",
        _ => "Various levels"
    };

    /// <summary>What the level is roughly equivalent to, for parents comparing options.</summary>
    public static string? Equivalent(this LevelBand band) => band switch
    {
        LevelBand.Level1 => "Similar to GCSE grades 3 to 1",
        LevelBand.Level2 => "Similar to GCSE grades 9 to 4",
        LevelBand.Level3 => "Similar to A levels",
        LevelBand.Level4Plus => "Higher education",
        _ => null
    };

    public static string Label(this StudyMode mode) => mode switch
    {
        StudyMode.PartTime => "Part time",
        StudyMode.Evening => "Evening",
        StudyMode.Online => "Online",
        _ => "Full time"
    };

    /// <summary>The college's C_MODE codes. Anything unknown is treated as full time.</summary>
    public static StudyMode ParseMode(string? code) => code?.Trim().ToUpperInvariant() switch
    {
        "PT" => StudyMode.PartTime,
        "EV" or "EVE" => StudyMode.Evening,
        "OL" or "ONLINE" => StudyMode.Online,
        _ => StudyMode.FullTime
    };

    /// <summary>
    /// The course_levels rows in use (L_DISABLED = 0) map to bands by title, so a renamed level still lands in
    /// the right place.
    /// </summary>
    public static LevelBand Band(string levelTitle)
    {
        var t = levelTitle.ToLowerInvariant();
        if (t.Contains("rarpa") || t.Contains("recognising")) return LevelBand.Supported;
        if (t.Contains("various")) return LevelBand.Various;
        if (t.Contains("entry")) return LevelBand.Entry;
        if (t.Contains("level one") || t.Contains("level 1") || t.Contains("foundation")) return LevelBand.Level1;
        if (t.Contains("level two") || t.Contains("level 2") || t.Contains("intermediate")) return LevelBand.Level2;
        if (t.Contains("level three") || t.Contains("level 3") || t.Contains("advanced")) return LevelBand.Level3;
        if (t.Contains("level four") || t.Contains("level 4") || t.Contains("higher")) return LevelBand.Level4Plus;
        return LevelBand.Various;
    }

    /// <summary>
    /// C_STARTDATE holds either an ISO date (from a date column) or free text. Dates show as "September 2026";
    /// text is shown as written.
    /// </summary>
    public static string StartDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "To be confirmed";
        var text = value.Trim();
        return DateOnly.TryParseExact(text, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date)
            ? date.ToString("MMMM yyyy", UkCulture)
            : text.TrimEnd('.');
    }

    /// <summary>A single amount in a fee field, such as "&lt;p&gt;&amp;pound;150&lt;/p&gt;". Null for anything else.</summary>
    public static decimal? SingleAmount(string? plainText)
    {
        if (string.IsNullOrWhiteSpace(plainText)) return null;
        var match = SingleAmountPattern().Match(plainText.Trim());
        return match.Success ? decimal.Parse(match.Groups[1].Value.Replace(",", ""), CultureInfo.InvariantCulture) : null;
    }

    public static string Slug(string text)
    {
        var builder = new StringBuilder(text.Length);
        foreach (var c in text.Normalize(NormalizationForm.FormD).ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(c)) builder.Append(c);
            else if (c == '&') builder.Append(" and ");
            else if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark) builder.Append(' ');
        }

        return string.Join('-', builder.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    [GeneratedRegex(@"^(?:approximately\s+)?£\s?(\d{1,3}(?:,\d{3})*(?:\.\d{2})?)$", RegexOptions.IgnoreCase)]
    private static partial Regex SingleAmountPattern();
}
