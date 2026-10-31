using System.Text.RegularExpressions;

namespace Prototype.Web.Site;

public static partial class YouTube
{
    /// <summary>The video ID from any of the usual YouTube addresses, or null.</summary>
    public static string? Id(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return null;
        var match = VideoId().Match(url);
        return match.Success ? match.Groups[1].Value : null;
    }

    [GeneratedRegex(@"(?:youtu\.be/|youtube(?:-nocookie)?\.com/(?:watch\?(?:.*&)?v=|embed/|shorts/|live/))([A-Za-z0-9_-]{11})")]
    private static partial Regex VideoId();
}
