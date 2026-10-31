using Prototype.Core.CollegeData;
using Prototype.Core.Payments;

namespace Prototype.Core.Tests.Payments;

public class DateOfBirthTests
{
    private static readonly DateOnly Today = new(2026, 11, 2);

    [Fact]
    public void Parse_GivenARealDate_WhenParsed_ThenTheDateIsReturned()
    {
        // When
        var (date, error) = DateOfBirth.Parse("21", "5", "2009", Today);

        // Then
        error.Should().BeNull();
        date.Should().Be(new DateOnly(2009, 5, 21));
    }

    [Theory]
    [InlineData("", "", "", "Enter the student's date of birth")]
    [InlineData("31", "2", "2009", "Date of birth must be a real date, like 27 3 2009")]
    [InlineData("21", "5", "09", "Year must include 4 numbers")]
    [InlineData("1", "1", "2030", "Date of birth must be in the past")]
    [InlineData("x", "5", "2009", "Date of birth must be a real date, like 27 3 2009")]
    public void Parse_GivenAMissingOrImpossibleDate_WhenParsed_ThenTheErrorSaysHowToFixIt(string day, string month, string year, string expected)
    {
        // When
        var (date, error) = DateOfBirth.Parse(day, month, year, Today);

        // Then
        date.Should().BeNull();
        error.Should().Be(expected);
    }
}

public class CollegeDataSeederTests
{
    [Fact]
    public void Batches_GivenAScriptWithGoSeparators_WhenSplit_ThenEachBatchIsReturnedWithoutCommentOnlyBatches()
    {
        // Given
        var script = "-- header comment\n\nCREATE TABLE a (x int);\nGO\nINSERT INTO a VALUES (1);\ngo\n";

        // When
        var batches = CollegeDataSeeder.Batches(script).ToList();

        // Then
        batches.Should().Equal("-- header comment\n\nCREATE TABLE a (x int);", "INSERT INTO a VALUES (1);");
    }
}
