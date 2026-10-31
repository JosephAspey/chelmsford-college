using Prototype.Core.Courses;

namespace Prototype.Core.Tests.Courses;

public class CourseSearchTests
{
    private static readonly IReadOnlyList<Course> Courses =
    [
        GivenCourse(1, "City & Guilds Diploma in Professional Cookery, Level 2", "Catering", LevelBand.Level2),
        GivenCourse(2, "VTCT Diploma in Hairdressing, Level 1", "Hair & Beauty", LevelBand.Level1),
        GivenCourse(3, "VTCT Diploma in Beauty Therapy, Level 3", "Hair & Beauty", LevelBand.Level3),
        GivenCourse(4, "Diploma Electrical Installations, Level 2", "Electrical Installation", LevelBand.Level2),
        GivenCourse(5, "BTEC National Extended Diploma in IT, Level 3", "ICT & Digital Skills", LevelBand.Level3, StudyMode.PartTime)
    ];

    [Fact]
    public void Search_GivenNoTextOrFilters_WhenSearched_ThenEveryCourseIsReturnedInTitleOrder()
    {
        // When
        var result = CourseSearch.Search(Courses, new CourseSearchQuery());

        // Then
        result.Total.Should().Be(5);
        result.Courses.Select(c => c.Id).Should().Equal(5, 1, 4, 3, 2);
    }

    [Theory]
    [InlineData("chef", 1)]
    [InlineData("electrician", 4)]
    [InlineData("coding", 5)]
    public void Search_GivenAnEverydayWord_WhenSearched_ThenTheCourseItMeansIsFound(string text, int expectedId)
    {
        // When
        var result = CourseSearch.Search(Courses, new CourseSearchQuery { Text = text });

        // Then
        result.Courses.Select(c => c.Id).Should().Equal(expectedId);
    }

    [Fact]
    public void Search_GivenALevelNumberInTheText_WhenSearched_ThenOnlyThatLevelIsReturned()
    {
        // When
        var result = CourseSearch.Search(Courses, new CourseSearchQuery { Text = "level 3 hair" });

        // Then
        result.Courses.Select(c => c.Id).Should().Equal(3);
    }

    [Fact]
    public void Search_GivenAShortWord_WhenSearched_ThenItOnlyMatchesWholeWords()
    {
        // Given: "it" is inside "City" and "Diploma", but only course 5 is about IT
        var query = new CourseSearchQuery { Text = "it" };

        // When
        var result = CourseSearch.Search(Courses, query);

        // Then
        result.Courses.Select(c => c.Id).Should().Equal(5);
    }

    [Fact]
    public void Search_GivenASubjectFilter_WhenSearched_ThenLevelCountsAreForThatSubjectButSubjectCountsIgnoreIt()
    {
        // Given
        var query = new CourseSearchQuery { Subjects = ["hair-and-beauty"] };

        // When
        var result = CourseSearch.Search(Courses, query);

        // Then
        result.Total.Should().Be(2);
        result.Levels.Should().BeEquivalentTo(new[]
        {
            new FacetOption<LevelBand>(LevelBand.Level1, "Level 1", 1, false),
            new FacetOption<LevelBand>(LevelBand.Level3, "Level 3", 1, false)
        });
        result.Subjects.Single(s => s.Value == "catering").Count.Should().Be(1);
        result.Subjects.Single(s => s.Value == "hair-and-beauty").Selected.Should().BeTrue();
    }

    [Fact]
    public void Search_GivenAPageBeyondTheEnd_WhenSearched_ThenTheLastPageIsReturned()
    {
        // When
        var result = CourseSearch.Search(Courses, new CourseSearchQuery { Page = 9, PageSize = 2 });

        // Then
        result.Page.Should().Be(3);
        result.PageCount.Should().Be(3);
        result.Courses.Should().HaveCount(1);
    }

    [Fact]
    public void Search_GivenTextThatMatchesNothing_WhenSearched_ThenNoCoursesAndNoFacetsAreReturned()
    {
        // When
        var result = CourseSearch.Search(Courses, new CourseSearchQuery { Text = "astrophysics" });

        // Then
        result.Total.Should().Be(0);
        result.Subjects.Should().BeEmpty();
    }

    private static Course GivenCourse(int id, string title, string subject, LevelBand band, StudyMode mode = StudyMode.FullTime) => new()
    {
        Id = id,
        Title = title,
        Slug = CourseLabels.Slug(title),
        Subject = new Subject(id * 10, subject, CourseLabels.Slug(subject)),
        Level = new CourseLevel(band switch { LevelBand.Level1 => 13, LevelBand.Level2 => 14, _ => 15 }, band.Label(), band),
        StudyMode = mode,
        StartDate = "September 2026",
        Content = new CourseContent(),
        SearchText = $"{title} {subject}"
    };
}
