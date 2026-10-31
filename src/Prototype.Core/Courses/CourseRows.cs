namespace Prototype.Core.Courses;

/// <summary>A row of the college's <c>courses</c> table joined to its section, with the college's column names.</summary>
public sealed class CourseRow
{
    public int C_ID { get; init; }
    public string? C_TITLE { get; init; }
    public int? C_SECTION { get; init; }
    public string? C_MODE { get; init; }
    public string? C_DURATION { get; init; }
    public string? C_DAYS { get; init; }
    public string? C_QUALIFICATION { get; init; }
    public string? C_ADDITIONALQUALS { get; init; }
    public string? C_COURSECODE { get; init; }
    public string? C_STARTDATE { get; init; }
    public string? C_STUDYOVERVIEW { get; init; }
    public string? C_EMPLOYABILITY { get; init; }
    public string? C_ELIGIBILITY { get; init; }
    public string? C_ELIGIBILITYSPECIFIC { get; init; }
    public string? C_ASSESSMENT { get; init; }
    public string? C_FEES { get; init; }
    public string? C_ADDITIONALFEES { get; init; }
    public string? C_FUNDING { get; init; }
    public string? C_CONTACT { get; init; }
    public string? C_METADESC { get; init; }
    public string? C_COURSEFORME { get; init; }
    public string? S_TITLE { get; init; }
}

public sealed record CourseLevelRow(int CourseId, int LevelId, string Title);

public sealed record CourseFundingRow(int CourseId, string Title);

/// <summary>Everything the site reads from the college's course tables in one go.</summary>
public sealed record CourseData(
    IReadOnlyList<CourseRow> Courses,
    IReadOnlyList<CourseLevelRow> Levels,
    IReadOnlyList<CourseFundingRow> Funding);
