namespace Prototype.Core.Courses;

/// <summary>Builds the website's courses from the rows in the college's tables.</summary>
public static class CourseMapper
{
    public static IReadOnlyList<Course> Map(CourseData data)
    {
        var levels = data.Levels.ToLookup(l => l.CourseId);
        var funding = data.Funding.ToLookup(f => f.CourseId);

        return data.Courses
            .Where(r => !string.IsNullOrWhiteSpace(r.C_TITLE))
            .Select(r => Map(r, levels[r.C_ID], funding[r.C_ID]))
            .OrderBy(c => c.Title, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    internal static Course Map(CourseRow row, IEnumerable<CourseLevelRow> levels, IEnumerable<CourseFundingRow> funding)
    {
        var title = row.C_TITLE!.Trim();
        var subjectName = string.IsNullOrWhiteSpace(row.S_TITLE) ? "Other courses" : row.S_TITLE.Trim();
        // A course can be linked to several levels; the highest one describes it best
        var level = levels
            .Select(l => new CourseLevel(l.LevelId, l.Title.Trim(), CourseLabels.Band(l.Title)))
            .OrderByDescending(l => l.Band is LevelBand.Supported or LevelBand.Various ? -1 : (int)l.Band)
            .FirstOrDefault();

        var content = new CourseContent
        {
            IsThisForMe = CourseHtml.Clean(row.C_COURSEFORME),
            WhatYouWillStudy = CourseHtml.Clean(row.C_STUDYOVERVIEW),
            EntryRequirements = CourseHtml.Join(row.C_ELIGIBILITY, row.C_ELIGIBILITYSPECIFIC),
            Assessment = CourseHtml.Clean(row.C_ASSESSMENT),
            Qualification = CourseHtml.Join(row.C_QUALIFICATION, row.C_ADDITIONALQUALS),
            WhereItLeads = CourseHtml.Clean(row.C_EMPLOYABILITY),
            Fees = CourseHtml.Join(row.C_FEES, row.C_FUNDING),
            AdditionalCosts = CourseHtml.Clean(row.C_ADDITIONALFEES),
            Contact = CourseHtml.Clean(row.C_CONTACT)
        };

        return new Course
        {
            Id = row.C_ID,
            Title = title,
            Slug = CourseLabels.Slug(title),
            Subject = new Subject(row.C_SECTION ?? 0, subjectName, CourseLabels.Slug(subjectName)),
            Level = level,
            StudyMode = CourseLabels.ParseMode(row.C_MODE),
            Duration = Tidy(row.C_DURATION),
            Days = Tidy(row.C_DAYS),
            CourseCode = Tidy(row.C_COURSECODE),
            StartDate = CourseLabels.StartDate(row.C_STARTDATE),
            Funding = funding.Select(f => f.Title.Trim()).Distinct().ToList(),
            Fee = CourseLabels.SingleAmount(CourseHtml.PlainText(row.C_FEES)),
            MetaDescription = Tidy(CourseHtml.PlainText(row.C_METADESC)),
            Content = content,
            SearchText = string.Join(' ', title, subjectName, CourseHtml.PlainText(row.C_QUALIFICATION),
                CourseHtml.PlainText(row.C_COURSEFORME), CourseHtml.PlainText(row.C_STUDYOVERVIEW))
        };
    }

    private static string? Tidy(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
