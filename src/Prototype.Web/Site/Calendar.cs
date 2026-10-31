using System.Text;

namespace Prototype.Web.Site;

/// <summary>Writes a single-event iCalendar (RFC 5545) file. Times are UK local time.</summary>
public static class Calendar
{
    public static string Event(Guid id, string title, DateTime start, DateTime end, string? location, string? description, string url)
    {
        var ics = new StringBuilder();
        ics.Append("BEGIN:VCALENDAR\r\n");
        ics.Append("VERSION:2.0\r\n");
        ics.Append("PRODID:-//Chelmsford College//Website//EN\r\n");
        ics.Append("BEGIN:VEVENT\r\n");
        ics.Append($"UID:{id}@chelmsford.ac.uk\r\n");
        ics.Append($"DTSTAMP:{DateTime.UtcNow:yyyyMMdd'T'HHmmss'Z'}\r\n");
        ics.Append($"DTSTART;TZID=Europe/London:{start:yyyyMMdd'T'HHmmss}\r\n");
        ics.Append($"DTEND;TZID=Europe/London:{end:yyyyMMdd'T'HHmmss}\r\n");
        ics.Append($"SUMMARY:{Escape(title)}\r\n");
        if (location is not null) ics.Append($"LOCATION:{Escape(location)}\r\n");
        ics.Append($"DESCRIPTION:{Escape($"{description}\n\n{url}".Trim())}\r\n");
        ics.Append($"URL:{url}\r\n");
        ics.Append("END:VEVENT\r\n");
        ics.Append("END:VCALENDAR\r\n");
        return ics.ToString();
    }

    private static string Escape(string text) =>
        text.Replace("\\", "\\\\").Replace(";", "\\;").Replace(",", "\\,").Replace("\r", "").Replace("\n", "\\n");
}
