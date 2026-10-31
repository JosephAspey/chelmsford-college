using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Prototype.Core.Courses;
using Prototype.Web.Cms;
using Prototype.Web.Site;
using Umbraco.Cms.Core.Web;
using Umbraco.Cms.Web.Common.Controllers;

namespace Prototype.Web.Controllers;

/// <summary>Home page: adds the subject list (from the course data) and the next few events (from the CMS).</summary>
public sealed class HomeController(ILogger<HomeController> logger, ICompositeViewEngine viewEngine, IUmbracoContextAccessor umbracoContext, CourseCatalogue catalogue, TimeProvider time)
    : RenderController(logger, viewEngine, umbracoContext)
{
    public async Task<IActionResult> Home(CancellationToken cancellationToken)
    {
        var all = await catalogue.SearchAsync(new CourseSearchQuery(), cancellationToken);
        var now = time.GetLocalNow().DateTime;
        var upcoming = CurrentPage!.Events().Where(e => (e.End ?? e.Start) >= now).Take(3).ToList();
        return CurrentTemplate(new HomeViewModel(CurrentPage!, all.Subjects, upcoming));
    }
}

/// <summary>Course search: the CMS page holds the wording, the results come from the college's course tables.</summary>
public sealed class CoursesPageController(ILogger<CoursesPageController> logger, ICompositeViewEngine viewEngine, IUmbracoContextAccessor umbracoContext, CourseCatalogue catalogue)
    : RenderController(logger, viewEngine, umbracoContext)
{
    public async Task<IActionResult> CoursesPage(
        [FromQuery] string? q, [FromQuery] string[]? subject, [FromQuery] string[]? level, [FromQuery] string[]? mode, [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var query = new CourseSearchQuery
        {
            Text = q?.Trim(),
            Subjects = subject ?? [],
            Levels = (level ?? []).Select(l => Enum.TryParse<LevelBand>(l, true, out var b) ? b : (LevelBand?)null).OfType<LevelBand>().ToList(),
            Modes = (mode ?? []).Select(m => Enum.TryParse<StudyMode>(m, true, out var s) ? s : (StudyMode?)null).OfType<StudyMode>().ToList(),
            Page = page
        };
        var result = await catalogue.SearchAsync(query, cancellationToken);
        return CurrentTemplate(new CoursesViewModel(CurrentPage!, query, result, new CourseImages(CurrentPage!)));
    }
}

/// <summary>Events list, filtered by type, upcoming by default.</summary>
public sealed class EventsPageController(ILogger<EventsPageController> logger, ICompositeViewEngine viewEngine, IUmbracoContextAccessor umbracoContext, TimeProvider time)
    : RenderController(logger, viewEngine, umbracoContext)
{
    public IActionResult EventsPage([FromQuery] string? type, [FromQuery] bool past = false)
    {
        var now = time.GetLocalNow().DateTime;
        var all = CurrentPage!.Events();
        var inRange = all.Where(e => past ? (e.End ?? e.Start) < now : (e.End ?? e.Start) >= now).ToList();
        var types = inRange.Select(e => e.Type).Distinct().Order().ToList();
        var shown = inRange.Where(e => type is null || e.TypeSlug == type).ToList();
        if (past) shown.Reverse();
        return CurrentTemplate(new EventsViewModel(CurrentPage!, shown, types, type, past));
    }
}
