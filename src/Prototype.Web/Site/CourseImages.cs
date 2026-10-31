using Prototype.Core.Courses;
using Prototype.Web.Cms;
using Umbraco.Cms.Core.Models.PublishedContent;
using Umbraco.Extensions;

namespace Prototype.Web.Site;

/// <summary>
/// Course data comes from the college's SQL tables; the pictures and subject intros come from the CMS. Staff set
/// an image for each subject area (matched on the section ID) and every course in that subject uses it.
/// </summary>
public sealed class CourseImages
{
    private readonly Dictionary<int, IPublishedContent> _subjects;
    private readonly IPublishedContent? _default;

    public CourseImages(IPublishedContent coursesPage)
    {
        _default = coursesPage.Image(DocTypes.CoursesPage.DefaultImage);
        _subjects = (coursesPage.Children() ?? [])
            .Where(c => c.ContentType.Alias == DocTypes.SubjectArea.Alias)
            .GroupBy(c => c.Value<int>(DocTypes.SubjectArea.SectionId))
            .ToDictionary(g => g.Key, g => g.First());
    }

    public IPublishedContent? For(Course course) =>
        _subjects.TryGetValue(course.Subject.Id, out var subject) && subject.Image(DocTypes.SubjectArea.Image) is { } image
            ? image
            : _default;

    public string? IntroFor(Course course) =>
        _subjects.TryGetValue(course.Subject.Id, out var subject) ? subject.Text(DocTypes.SubjectArea.Intro) : null;

    public IPublishedContent? SubjectPage(Course course) => _subjects.GetValueOrDefault(course.Subject.Id);
}
