using Prototype.Core.Courses;

namespace Prototype.Core.Tests.Courses;

public class CourseMapperTests
{
    [Fact]
    public void Map_GivenACourseRowWithLinks_WhenMapped_ThenTheWebsiteCourseIsBuiltFromTheCollegeColumns()
    {
        // Given
        var row = new CourseRow
        {
            C_ID = 374,
            C_TITLE = "UAL Level 3 Diploma in Creative Practice: Fashion Design ",
            C_SECTION = 48,
            S_TITLE = "Creative Industries",
            C_MODE = "FT",
            C_DURATION = "2 years",
            C_STARTDATE = "2026-09-01",
            C_FEES = "<p>&pound;150</p>",
            C_ELIGIBILITY = "<p>4 GCSEs grades 9-4</p>",
            C_ELIGIBILITYSPECIFIC = "<p>&nbsp;</p>",
            C_COURSEFORME = "<p>Do you want to work in fashion?</p>"
        };
        CourseLevelRow[] levels = [new(374, 14, "Intermediate (Level Two)"), new(374, 15, "Advanced (Level Three)")];
        CourseFundingRow[] funding = [new(374, "16-18"), new(374, "16-18")];

        // When
        var course = CourseMapper.Map(row, levels, funding);

        // Then
        course.Title.Should().Be("UAL Level 3 Diploma in Creative Practice: Fashion Design");
        course.Subject.Should().Be(new Subject(48, "Creative Industries", "creative-industries"));
        course.Level!.Band.Should().Be(LevelBand.Level3);
        course.StudyMode.Should().Be(StudyMode.FullTime);
        course.StartDate.Should().Be("September 2026");
        course.Fee.Should().Be(150);
        course.Funding.Should().Equal("16-18");
        course.Content.EntryRequirements.Should().Be("<p>4 GCSEs grades 9-4</p>");
        course.Content.IsThisForMe.Should().Be("<p>Do you want to work in fashion?</p>");
        course.Content.Assessment.Should().BeNull();
    }

    [Fact]
    public void Map_GivenACourseWithNoSection_WhenMapped_ThenItIsFiledUnderOtherCourses()
    {
        // Given
        var data = new CourseData([new CourseRow { C_ID = 1, C_TITLE = "Life Skills" }], [], []);

        // When
        var courses = CourseMapper.Map(data);

        // Then
        courses.Single().Subject.Name.Should().Be("Other courses");
        courses.Single().Level.Should().BeNull();
    }
}
