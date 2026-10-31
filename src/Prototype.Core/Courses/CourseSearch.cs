using System.Text.RegularExpressions;

namespace Prototype.Core.Courses;

public sealed record CourseSearchQuery
{
    public string? Text { get; init; }
    public IReadOnlyCollection<string> Subjects { get; init; } = [];
    public IReadOnlyCollection<LevelBand> Levels { get; init; } = [];
    public IReadOnlyCollection<StudyMode> Modes { get; init; } = [];
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 12;

    public bool HasFilters => Subjects.Count > 0 || Levels.Count > 0 || Modes.Count > 0;
}

public sealed record FacetOption<T>(T Value, string Label, int Count, bool Selected);

public sealed record CourseSearchResult(
    IReadOnlyList<Course> Courses,
    int Total,
    int Page,
    int PageCount,
    IReadOnlyList<FacetOption<string>> Subjects,
    IReadOnlyList<FacetOption<LevelBand>> Levels,
    IReadOnlyList<FacetOption<StudyMode>> Modes);

/// <summary>
/// Course search. Text matches titles, subjects and course descriptions, and everyday words people type
/// ("chef", "nursing", "electrician") reach the subject they mean. Each filter's counts are worked out with the
/// other filters applied, so a count is always the number of courses you would get by ticking that box.
/// </summary>
public static class CourseSearch
{
    // Everyday words → words in the subject names the college uses
    private static readonly Dictionary<string, string[]> EverydayWords = new(StringComparer.OrdinalIgnoreCase)
    {
        ["chef"] = ["catering", "cookery"], ["cook"] = ["catering", "cookery"], ["cooking"] = ["catering", "cookery"], ["food"] = ["catering", "cookery"],
        ["hair"] = ["hair", "hairdressing"], ["hairdresser"] = ["hairdressing"], ["barber"] = ["hairdressing"], ["beauty"] = ["beauty"], ["makeup"] = ["beauty"], ["nails"] = ["beauty"],
        ["builder"] = ["construction"], ["building"] = ["construction"], ["joiner"] = ["carpentry", "joinery"], ["carpenter"] = ["carpentry"], ["plumber"] = ["plumbing"], ["decorator"] = ["decorating"],
        ["electrician"] = ["electrical"], ["sparky"] = ["electrical"], ["engineer"] = ["engineering"], ["mechanic"] = ["engineering"],
        ["computer"] = ["ict", "digital", "it"], ["computing"] = ["ict", "digital", "it"], ["coding"] = ["ict", "it"], ["programming"] = ["ict", "it"], ["games"] = ["ict", "digital", "media"],
        ["police"] = ["uniformed"], ["army"] = ["uniformed"], ["military"] = ["uniformed"], ["firefighter"] = ["uniformed"], ["forces"] = ["uniformed"],
        ["nurse"] = ["health", "care"], ["nursing"] = ["health", "care"], ["midwife"] = ["health"], ["paramedic"] = ["health"], ["carer"] = ["care"],
        ["nursery"] = ["childcare", "early years"], ["teacher"] = ["childcare", "early years"], ["teaching"] = ["childcare", "early years"], ["children"] = ["childcare", "early years"],
        ["acting"] = ["performing"], ["drama"] = ["performing"], ["dance"] = ["performing"], ["theatre"] = ["performing", "production"], ["music"] = ["performing"],
        ["art"] = ["creative", "art"], ["design"] = ["creative", "design"], ["fashion"] = ["fashion"], ["photography"] = ["creative", "media"], ["film"] = ["media"], ["graphics"] = ["graphic"],
        ["law"] = ["legal"], ["lawyer"] = ["legal"], ["accounting"] = ["business"], ["office"] = ["business", "administration"], ["marketing"] = ["marketing"],
        ["holiday"] = ["travel", "tourism"], ["airline"] = ["travel", "tourism"],
        ["football"] = ["sport"], ["fitness"] = ["sport"], ["gym"] = ["sport"],
        ["crime"] = ["forensic", "criminal"], ["forensics"] = ["forensic"], ["university"] = ["access", "higher education"], ["degree"] = ["access", "higher education"]
    };

    public static CourseSearchResult Search(IReadOnlyList<Course> all, CourseSearchQuery query)
    {
        var scored = all
            .Select(c => (Course: c, Score: Score(c, query.Text)))
            .Where(x => x.Score > 0)
            .ToList();

        bool InSubject(Course c) => query.Subjects.Count == 0 || query.Subjects.Contains(c.Subject.Slug);
        bool InLevel(Course c) => query.Levels.Count == 0 || (c.Level is not null && query.Levels.Contains(c.Level.Band));
        bool InMode(Course c) => query.Modes.Count == 0 || query.Modes.Contains(c.StudyMode);

        var matches = scored
            .Where(x => InSubject(x.Course) && InLevel(x.Course) && InMode(x.Course))
            .OrderByDescending(x => x.Score)
            .ThenBy(x => x.Course.Title, StringComparer.OrdinalIgnoreCase)
            .Select(x => x.Course)
            .ToList();

        var subjects = scored.Select(x => x.Course).Where(c => InLevel(c) && InMode(c))
            .GroupBy(c => c.Subject.Slug)
            .Select(g => new FacetOption<string>(g.Key, g.First().Subject.Name, g.Count(), query.Subjects.Contains(g.Key)))
            .Concat(query.Subjects.Where(s => scored.All(x => x.Course.Subject.Slug != s || !InLevel(x.Course) || !InMode(x.Course)))
                .Select(s => new FacetOption<string>(s, all.FirstOrDefault(c => c.Subject.Slug == s)?.Subject.Name ?? s, 0, true)))
            .OrderBy(f => f.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var levels = Enum.GetValues<LevelBand>()
            .Select(b => new FacetOption<LevelBand>(b, b.Label(),
                scored.Count(x => x.Course.Level?.Band == b && InSubject(x.Course) && InMode(x.Course)), query.Levels.Contains(b)))
            .Where(f => f.Count > 0 || f.Selected)
            .ToList();

        var modes = Enum.GetValues<StudyMode>()
            .Select(m => new FacetOption<StudyMode>(m, m.Label(),
                scored.Count(x => x.Course.StudyMode == m && InSubject(x.Course) && InLevel(x.Course)), query.Modes.Contains(m)))
            .Where(f => f.Count > 0 || f.Selected)
            .ToList();

        var pageSize = Math.Max(1, query.PageSize);
        var pageCount = Math.Max(1, (int)Math.Ceiling(matches.Count / (double)pageSize));
        var page = Math.Clamp(query.Page, 1, pageCount);

        return new CourseSearchResult(
            matches.Skip((page - 1) * pageSize).Take(pageSize).ToList(),
            matches.Count, page, pageCount, subjects, levels, modes);
    }

    /// <summary>0 means no match. With no search text every course matches equally.</summary>
    internal static int Score(Course course, string? text)
    {
        var words = Words(text);
        if (words.Count == 0) return 1;

        var score = 0;
        foreach (var word in words)
        {
            if (word.Equals("level", StringComparison.OrdinalIgnoreCase)) continue;

            // A number is a level: "level 3 hair" means level 3 hair courses
            if (int.TryParse(word, out var number))
            {
                if (!MatchesLevel(course, number)) return 0;
                score += 3;
                continue;
            }

            var wordScore = 0;
            if (Contains(course.Title, word)) wordScore = 6;
            else if (Contains(course.Subject.Name, word)) wordScore = 5;
            else if (EverydayWords.TryGetValue(word, out var meant) && meant.Any(m => Contains(course.Title, m) || Contains(course.Subject.Name, m))) wordScore = 4;
            else if (Contains(course.SearchText, word)) wordScore = 1;

            // Every word has to match somewhere, so "hair beauty" narrows rather than widens
            if (wordScore == 0) return 0;
            score += wordScore;
        }

        return Math.Max(score, 1);
    }

    private static List<string> Words(string? text) =>
        string.IsNullOrWhiteSpace(text)
            ? []
            : text.Split([' ', ',', '/', '-'], StringSplitOptions.RemoveEmptyEntries)
                .Select(w => w.Trim().Trim('"', '\'', '.', '?', '!'))
                .Where(w => (w.Length > 1 || char.IsDigit(w[0])) && !StopWords.Contains(w))
                .ToList();

    private static bool MatchesLevel(Course course, int number) => (course.Level?.Band, number) switch
    {
        (LevelBand.Level1, 1) or (LevelBand.Level2, 2) or (LevelBand.Level3, 3) => true,
        (LevelBand.Level4Plus, >= 4) => true,
        _ => false
    };

    // Short words ("it", "art") match whole words only, or "it" would match "City" and "digital"
    private static bool Contains(string haystack, string word) =>
        word.Length <= 3
            ? Regex.IsMatch(haystack, $"\\b{Regex.Escape(word)}\\b", RegexOptions.IgnoreCase)
            : haystack.Contains(word, StringComparison.OrdinalIgnoreCase);

    private static readonly HashSet<string> StopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "and", "the", "in", "of", "for", "to", "a", "an", "course", "courses", "with", "at", "on", "i", "want", "be", "become", "study"
    };
}
