namespace Prototype.Core.Courses;

/// <summary>A course as the website shows it, built from one row of the college's <c>courses</c> table and its links.</summary>
public sealed record Course
{
    public required int Id { get; init; }
    public required string Title { get; init; }
    public required string Slug { get; init; }
    public required Subject Subject { get; init; }
    public CourseLevel? Level { get; init; }
    public required StudyMode StudyMode { get; init; }
    public string? Duration { get; init; }
    public string? Days { get; init; }
    public string? CourseCode { get; init; }

    /// <summary>The start date as the college wrote it, e.g. "September 2026" or "Flexible".</summary>
    public required string StartDate { get; init; }

    public IReadOnlyList<string> Funding { get; init; } = [];

    /// <summary>The course fee, when <c>C_FEES</c> holds a single amount such as "£150".</summary>
    public decimal? Fee { get; init; }

    public string? MetaDescription { get; init; }
    public required CourseContent Content { get; init; }

    /// <summary>Plain text of the main fields, for search.</summary>
    public required string SearchText { get; init; }
}

/// <summary>The course's long-form sections, as cleaned HTML. Null when the college has left a field empty.</summary>
public sealed record CourseContent
{
    public string? IsThisForMe { get; init; }
    public string? WhatYouWillStudy { get; init; }
    public string? EntryRequirements { get; init; }
    public string? Assessment { get; init; }
    public string? Qualification { get; init; }
    public string? WhereItLeads { get; init; }
    public string? Fees { get; init; }
    public string? AdditionalCosts { get; init; }
    public string? Contact { get; init; }
}

public sealed record Subject(int Id, string Name, string Slug);

/// <summary>A course level, grouped into the bands learners recognise (Entry, Level 1, Level 2 and so on).</summary>
public sealed record CourseLevel(int Id, string Title, LevelBand Band);

public enum LevelBand
{
    Entry,
    Level1,
    Level2,
    Level3,
    Level4Plus,
    Supported,
    Various
}

public enum StudyMode
{
    FullTime,
    PartTime,
    Evening,
    Online
}
