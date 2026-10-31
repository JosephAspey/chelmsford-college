using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ViewEngines;
using Microsoft.Extensions.Options;
using Umbraco.Cms.Core;
using Prototype.Core.Courses;
using Prototype.Web.Cms;
using Prototype.Web.Site;
using Prototype.Web.Startup;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Cms.Web.Common.Controllers;
using Umbraco.Extensions;

namespace Prototype.Web.Controllers;

/// <summary>
/// One template for every course: /courses/{C_ID}/{slug}. The course comes from the college's courses table; the
/// page sits under the CMS's Courses page for its navigation, image and wording.
/// </summary>
public sealed class CourseDetailController(
    ILogger<CourseDetailController> logger,
    ICompositeViewEngine viewEngine,
    IPublishedContentQuery contentQuery,
    CourseCatalogue catalogue,
    IOptions<PrototypeOptions> options) : UmbracoPageController(logger, viewEngine), IVirtualPageController
{
    public IPublishedContent? FindContent(ActionExecutingContext actionExecutingContext) =>
        contentQuery.FindPage(DocTypes.CoursesPage.Alias);

    public async Task<IActionResult> Index(int id, string? slug, CancellationToken cancellationToken)
    {
        var course = await catalogue.FindAsync(id, cancellationToken);
        if (course is null) return NotFound();

        // One address per course, so search engines don't see duplicates when a title changes
        if (slug != course.Slug) return RedirectPermanent($"/courses/{course.Id}/{course.Slug}/");

        var all = await catalogue.AllAsync(cancellationToken);
        var related = all.Where(c => c.Subject.Id == course.Subject.Id && c.Id != course.Id).Take(3).ToList();
        return View("CourseDetail", new CourseViewModel(CurrentPage!, course, new CourseImages(CurrentPage!), related, options.Value.ShowBuildNotes));
    }
}

public static class ContentQueryExtensions
{
    /// <summary>The first page of the given type under the home page.</summary>
    public static IPublishedContent? FindPage(this IPublishedContentQuery query, string alias) =>
        query.ContentAtRoot().FirstOrDefault()?.Children()?.FirstOrDefault(c => c.ContentType.Alias == alias);
}
