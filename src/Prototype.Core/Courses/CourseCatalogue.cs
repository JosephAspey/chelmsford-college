using Dapper;
using Microsoft.Extensions.Caching.Memory;
using Prototype.Core.CollegeData;

namespace Prototype.Core.Courses;

public interface ICourseSource
{
    Task<CourseData> LoadAsync(CancellationToken cancellationToken);
}

/// <summary>Reads the published courses straight from the college's tables, read only.</summary>
public sealed class SqlCourseSource(CollegeDatabase database) : ICourseSource
{
    private const string CoursesSql = """
        SELECT c.C_ID, c.C_TITLE, c.C_SECTION, c.C_MODE, c.C_DURATION, c.C_DAYS, c.C_QUALIFICATION, c.C_ADDITIONALQUALS,
               c.C_COURSECODE, c.C_STARTDATE, c.C_STUDYOVERVIEW, c.C_EMPLOYABILITY, c.C_ELIGIBILITY, c.C_ELIGIBILITYSPECIFIC,
               c.C_ASSESSMENT, c.C_FEES, c.C_ADDITIONALFEES, c.C_FUNDING, c.C_CONTACT, c.C_METADESC, c.C_COURSEFORME,
               s.S_TITLE
        FROM dbo.courses c
        LEFT JOIN dbo.course_sections s ON s.S_ID = c.C_SECTION
        WHERE c.C_PUBLISHED = 1 AND c.C_DELETED IS NULL
        """;

    private const string LevelsSql = """
        SELECT ll.CL_COURSEID AS CourseId, l.L_ID AS LevelId, l.L_TITLE AS Title
        FROM dbo.course_level_link ll
        JOIN dbo.course_levels l ON l.L_ID = ll.CL_LEVELID
        WHERE ISNULL(l.L_DISABLED, 0) = 0
        """;

    private const string FundingSql = """
        SELECT fl.CF_COURSEID AS CourseId, f.F_TITLE AS Title
        FROM dbo.course_fund_link fl
        JOIN dbo.course_funding f ON f.F_ID = fl.CF_FUNDID
        """;

    public async Task<CourseData> LoadAsync(CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        var courses = await connection.QueryAsync<CourseRow>(new CommandDefinition(CoursesSql, cancellationToken: cancellationToken));
        var levels = await connection.QueryAsync<CourseLevelRow>(new CommandDefinition(LevelsSql, cancellationToken: cancellationToken));
        var funding = await connection.QueryAsync<CourseFundingRow>(new CommandDefinition(FundingSql, cancellationToken: cancellationToken));
        return new CourseData(courses.ToList(), levels.ToList(), funding.ToList());
    }
}

/// <summary>
/// The courses, read from the college's tables and held for a few minutes. A change the college makes in its
/// own system shows on the site within <see cref="CacheFor"/>, without anyone touching the CMS.
/// </summary>
public sealed class CourseCatalogue(ICourseSource source, IMemoryCache cache)
{
    public static readonly TimeSpan CacheFor = TimeSpan.FromMinutes(5);
    private const string CacheKey = "college-courses";

    public async Task<IReadOnlyList<Course>> AllAsync(CancellationToken cancellationToken) =>
        (await cache.GetOrCreateAsync(CacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheFor;
            return CourseMapper.Map(await source.LoadAsync(cancellationToken));
        }))!;

    public async Task<Course?> FindAsync(int id, CancellationToken cancellationToken) =>
        (await AllAsync(cancellationToken)).FirstOrDefault(c => c.Id == id);

    public async Task<CourseSearchResult> SearchAsync(CourseSearchQuery query, CancellationToken cancellationToken) =>
        CourseSearch.Search(await AllAsync(cancellationToken), query);

    public void Refresh() => cache.Remove(CacheKey);
}
