using System.Globalization;
using Microsoft.AspNetCore.Html;
using Prototype.Web.Cms;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;

namespace Prototype.Web.Site;

/// <summary>Reading CMS content in views: the home page, images at the right size, rich text and events.</summary>
public static class SiteContent
{
    public static readonly CultureInfo Uk = CultureInfo.GetCultureInfo("en-GB");

    public static IPublishedContent Home(this IPublishedContent page) => page.Root();

    public static IPublishedContent? FirstChildOfType(this IPublishedContent page, string alias) =>
        page.Home().Children()?.FirstOrDefault(c => c.ContentType.Alias == alias);

    public static string? Text(this IPublishedContent page, string alias) =>
        page.Value<string>(alias) is { Length: > 0 } value ? value.Trim() : null;

    public static IHtmlContent? Rich(this IPublishedContent page, string alias) =>
        page.Value<IHtmlEncodedString>(alias) is { } html && !string.IsNullOrWhiteSpace(html.ToHtmlString())
            ? new HtmlString(html.ToHtmlString())
            : null;

    public static IPublishedContent? Image(this IPublishedContent page, string alias) =>
        page.Value<IPublishedContent>(alias);

    /// <summary>A resized, cropped image URL, served as WebP. ImageSharp does the work and caches the result.</summary>
    public static string? Crop(this IPublishedContent? image, int width, int height) =>
        image?.GetCropUrl(width: width, height: height, imageCropMode: ImageCropMode.Crop, furtherOptions: "&format=webp&quality=75");

    public static string Description(this IPublishedContent page) =>
        page.Text(DocTypes.MetaDescription) ?? "Chelmsford College: courses, apprenticeships and adult learning in Chelmsford, Essex.";

    public static string Money(decimal amount) =>
        amount % 1 == 0 ? amount.ToString("C0", Uk) : amount.ToString("C2", Uk);

    public static IReadOnlyList<EventItem> Events(this IPublishedContent page) =>
        page.FirstChildOfType(DocTypes.EventsPage.Alias)?.Children()?
            .Where(c => c.ContentType.Alias == DocTypes.Event.Alias)
            .Select(EventItem.From)
            .OrderBy(e => e.Start)
            .ToList() ?? [];
}

public sealed record EventItem(IPublishedContent Page, string Name, string Type, DateTime Start, DateTime? End, string? Location, string? Summary, IPublishedContent? Image)
{
    public static EventItem From(IPublishedContent page) => new(
        page,
        page.Name,
        EventType(page),
        page.Value<DateTime>(DocTypes.Event.Start),
        page.Value<DateTime?>(DocTypes.Event.End) is { } end && end > DateTime.MinValue ? end : null,
        page.Text(DocTypes.Event.Location),
        page.Text(DocTypes.Event.Summary),
        page.Image(DocTypes.Event.Image));

    public string Url => Page.Url();

    /// <summary>e.g. "Wednesday 4 November 2026"</summary>
    public string Date => Start.ToString("dddd d MMMM yyyy", SiteContent.Uk);

    /// <summary>e.g. "6pm to 8:30pm"</summary>
    public string Times => End is { } end ? $"{Time(Start)} to {Time(end)}" : Time(Start);

    public string TypeSlug => Prototype.Core.Courses.CourseLabels.Slug(Type);

    private static string Time(DateTime t) =>
        (t.Minute == 0 ? t.ToString("%h", SiteContent.Uk) : t.ToString("h:mm", SiteContent.Uk)) + (t.Hour < 12 ? "am" : "pm");

    private static string EventType(IPublishedContent page)
    {
        var value = page.Value(DocTypes.Event.EventType);
        return value switch
        {
            string s when s.Length > 0 => s,
            IEnumerable<string> list => list.FirstOrDefault() ?? "Event",
            _ => "Event"
        };
    }
}
