namespace Prototype.Core.Payments;

/// <summary>Reads a date of birth entered as separate day, month and year boxes, as the GOV.UK pattern does.</summary>
public static class DateOfBirth
{
    public static (DateOnly? Date, string? Error) Parse(string? day, string? month, string? year, DateOnly today)
    {
        if (string.IsNullOrWhiteSpace(day) && string.IsNullOrWhiteSpace(month) && string.IsNullOrWhiteSpace(year))
            return (null, "Enter the student's date of birth");

        if (!int.TryParse(day?.Trim(), out var d) || !int.TryParse(month?.Trim(), out var m) || !int.TryParse(year?.Trim(), out var y))
            return (null, "Date of birth must be a real date, like 27 3 2009");

        if (y < 100) return (null, "Year must include 4 numbers");
        if (m is < 1 or > 12 || d < 1 || d > DateTime.DaysInMonth(Math.Clamp(y, 1, 9999), m))
            return (null, "Date of birth must be a real date, like 27 3 2009");

        var date = new DateOnly(y, m, d);
        if (date >= today) return (null, "Date of birth must be in the past");
        if (date < today.AddYears(-120)) return (null, "Date of birth must be a real date, like 27 3 2009");
        return (date, null);
    }
}
