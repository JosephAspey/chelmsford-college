using Prototype.Core.Courses;

namespace Prototype.Core.Tests.Courses;

public class CourseLabelsTests
{
    [Theory]
    [InlineData("2026-09-01", "September 2026")]
    [InlineData("Flexible", "Flexible")]
    [InlineData("October 2026, January 2027 and April 2027.", "October 2026, January 2027 and April 2027")]
    [InlineData(null, "To be confirmed")]
    public void StartDate_GivenAStoredValue_WhenShown_ThenDatesReadAsMonthAndYearAndTextIsKept(string? stored, string expected)
    {
        // When
        var shown = CourseLabels.StartDate(stored);

        // Then
        shown.Should().Be(expected);
    }

    [Theory]
    [InlineData("£150", 150)]
    [InlineData("Approximately £200", 200)]
    [InlineData("£1,250.50", 1250.50)]
    public void SingleAmount_GivenAFeeThatIsOneAmount_WhenRead_ThenTheAmountIsReturned(string text, decimal expected)
    {
        // When
        var amount = CourseLabels.SingleAmount(text);

        // Then
        amount.Should().Be(expected);
    }

    [Theory]
    [InlineData("Email studentfees@chelmsford.ac.uk for details")]
    [InlineData("£150 for kit and £50 for trips")]
    [InlineData("")]
    public void SingleAmount_GivenAFeeThatIsNotOneAmount_WhenRead_ThenThereIsNoAmount(string text)
    {
        // When
        var amount = CourseLabels.SingleAmount(text);

        // Then
        amount.Should().BeNull();
    }

    [Theory]
    [InlineData("Entry Level Two", LevelBand.Entry)]
    [InlineData("Foundation (Level One)", LevelBand.Level1)]
    [InlineData("Intermediate (Level Two)", LevelBand.Level2)]
    [InlineData("Advanced (Level Three)", LevelBand.Level3)]
    [InlineData("Higher (Level Four and above)", LevelBand.Level4Plus)]
    [InlineData("RARPA (Recognising & Recording Progress & Achievement)", LevelBand.Supported)]
    public void Band_GivenACollegeLevelTitle_WhenBanded_ThenItLandsInTheBandLearnersRecognise(string title, LevelBand expected)
    {
        // When
        var band = CourseLabels.Band(title);

        // Then
        band.Should().Be(expected);
    }

    [Fact]
    public void Slug_GivenATitleWithPunctuationAndAnAmpersand_WhenSlugged_ThenItIsLowerCaseWordsJoinedByHyphens()
    {
        // When
        var slug = CourseLabels.Slug("City & Guilds Diploma in Professional Cookery, Level 2 ");

        // Then
        slug.Should().Be("city-and-guilds-diploma-in-professional-cookery-level-2");
    }
}
