using Prototype.Core.Courses;

namespace Prototype.Core.Tests.Courses;

public class CourseHtmlTests
{
    [Fact]
    public void Clean_GivenHtmlFromTheCurrentSitesEditor_WhenCleaned_ThenOnlyTheStructureIsKept()
    {
        // Given: inline styles, Bootstrap classes and an icon span, as found in course_sections
        var html = """<p style="text-align: justify">Earn while you learn.</p><p><a class="btn btn-info" href="https://www.gov.uk/become-apprentice">Find out more <span class="glyphicon glyphicon-new-window" aria-hidden="true"></span></a></p>""";

        // When
        var cleaned = CourseHtml.Clean(html);

        // Then
        cleaned.Should().Be("""<p>Earn while you learn.</p><p><a href="https://www.gov.uk/become-apprentice">Find out more</a></p>""");
    }

    [Fact]
    public void Clean_GivenOnlyEmptyParagraphs_WhenCleaned_ThenNothingIsLeft()
    {
        // When
        var cleaned = CourseHtml.Clean("<p>&nbsp;</p>\n<p> </p>");

        // Then
        cleaned.Should().BeNull();
    }

    [Fact]
    public void Clean_GivenAScript_WhenCleaned_ThenTheScriptIsRemoved()
    {
        // When
        var cleaned = CourseHtml.Clean("<p>Hello</p><script>alert(1)</script>");

        // Then
        cleaned.Should().Be("<p>Hello</p>");
    }

    [Fact]
    public void PlainText_GivenEncodedEntities_WhenConverted_ThenTheyAreDecoded()
    {
        // When
        var text = CourseHtml.PlainText("<p>&pound;150&nbsp;for kit &amp; uniform</p>");

        // Then
        text.Should().Be("£150 for kit & uniform");
    }
}
