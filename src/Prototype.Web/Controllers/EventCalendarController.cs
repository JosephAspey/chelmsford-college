using System.Text;
using Microsoft.AspNetCore.Mvc;
using Prototype.Web.Cms;
using Prototype.Web.Site;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Core.Web;
using Umbraco.Extensions;

namespace Prototype.Web.Controllers;

/// <summary>"Add to calendar": an .ics file for one event, which phones and Outlook open directly.</summary>
public sealed class EventCalendarController(IUmbracoContextFactory umbracoContextFactory) : Controller
{
    [HttpGet]
    public IActionResult Download(Guid key)
    {
        // A plain MVC route, so it needs its own Umbraco context to read published content
        using var reference = umbracoContextFactory.EnsureUmbracoContext();
        var page = reference.UmbracoContext.Content?.GetById(key);
        if (page is null || page.ContentType.Alias != DocTypes.Event.Alias) return NotFound();

        var e = EventItem.From(page);
        var url = page.Url(mode: UrlMode.Absolute);
        var ics = Calendar.Event(page.Key, e.Name, e.Start, e.End ?? e.Start.AddHours(1), e.Location, e.Summary, url);
        return File(Encoding.UTF8.GetBytes(ics), "text/calendar", $"{page.UrlSegment}.ics");
    }
}
