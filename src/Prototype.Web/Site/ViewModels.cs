using Prototype.Core.Courses;
using Prototype.Core.Payments;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace Prototype.Web.Site;

public sealed class HomeViewModel(IPublishedContent content, IReadOnlyList<FacetOption<string>> subjects, IReadOnlyList<EventItem> upcoming)
    : ContentModel(content)
{
    public IReadOnlyList<FacetOption<string>> Subjects { get; } = subjects;
    public IReadOnlyList<EventItem> Upcoming { get; } = upcoming;
}

public sealed class CoursesViewModel(IPublishedContent content, CourseSearchQuery query, CourseSearchResult result, CourseImages images)
    : ContentModel(content)
{
    public CourseSearchQuery Query { get; } = query;
    public CourseSearchResult Result { get; } = result;
    public CourseImages Images { get; } = images;
}

public sealed class CourseViewModel(IPublishedContent coursesPage, Course course, CourseImages images, IReadOnlyList<Course> related, bool showBuildNotes)
    : ContentModel(coursesPage)
{
    public Course Course { get; } = course;
    public CourseImages Images { get; } = images;
    public IReadOnlyList<Course> Related { get; } = related;
    public bool ShowBuildNotes { get; } = showBuildNotes;
}

public sealed class EventsViewModel(IPublishedContent content, IReadOnlyList<EventItem> events, IReadOnlyList<string> types, string? type, bool past)
    : ContentModel(content)
{
    public IReadOnlyList<EventItem> Events { get; } = events;
    public IReadOnlyList<string> Types { get; } = types;
    public string? Type { get; } = type;
    public bool Past { get; } = past;
}

/// <summary>Any step of the payment journey. Each step's view reads the parts it needs.</summary>
public sealed class PaymentStepViewModel(IPublishedContent paymentsPage, PaymentType type) : ContentModel(paymentsPage)
{
    public PaymentType Type { get; } = type;
    public int Step { get; init; }
    public Dictionary<string, string> Errors { get; init; } = [];
    public PaymentForm Form { get; init; } = new();
    public LearnerAccount? Account { get; init; }
    public Payment? Payment { get; init; }
    public string? CourseTitle { get; init; }
    public string? Message { get; init; }

    public const int StepCount = 4;
}

/// <summary>Everything the payment forms post. Model binding fills it; each step uses some of it.</summary>
public sealed class PaymentForm
{
    public string? Reference { get; set; }
    public string? DobDay { get; set; }
    public string? DobMonth { get; set; }
    public string? DobYear { get; set; }
    public List<string> Items { get; set; } = [];
    public PayerType? Payer { get; set; }
    public string? PayerName { get; set; }
    public string? PayerEmail { get; set; }
}
