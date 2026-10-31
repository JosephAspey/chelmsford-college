namespace Prototype.Web.Cms;

/// <summary>
/// Starter events. The Showcase Open Evening is the sample the college supplied (Demo Event.docx); the others
/// show how the list filters by type and month, and can be edited or deleted in the CMS.
/// </summary>
public static class SampleEvents
{
    public sealed record SampleEvent(string Name, string Type, DateTime Start, DateTime End, string Location, string Summary, string Body, string Image, string? BookingLink, string? BookingText);

    public static readonly SampleEvent[] All =
    [
        new("Showcase Open Evening", "Open event",
            new DateTime(2026, 11, 4, 18, 0, 0), new DateTime(2026, 11, 4, 20, 30, 0),
            "Moulsham Street and Princes Road campuses",
            "See what Chelmsford College is like. Visit departments, tour the campuses and talk to the tutors who could be teaching you next year.",
            """
            <p>The Showcase event is an excellent opportunity to see what Chelmsford College is like. Visit the various departments, check out the campus and talk to the tutors who could be working with you next year. With our leisure centre recently refurbished, you will also have the chance to tour the facilities as well as talk to staff about careers, SEND support and student support (including travel and finance).</p>
            <p>Your booked time is the time you can arrive at the building. You can stay as long as you like and visit as many departments as you're interested in.</p>
            <h3>Presentations</h3>
            <ul>
            <li><strong>Apprenticeships:</strong> 6.30pm, 7pm and 7.30pm at both campuses</li>
            <li><strong>Electrical:</strong> 6.15pm, 6.45pm, 7.15pm and 7.45pm at Princes Road</li>
            <li><strong>Football Academy:</strong> 6.30pm (boys) and 7pm (girls) at Moulsham Street</li>
            <li><strong>Higher Qualifications:</strong> 7pm at both campuses</li>
            <li><strong>Performing Arts:</strong> 6.45pm, 7.15pm and 7.45pm at Moulsham Street</li>
            <li><strong>SEND support, transition to college, and stakeholders and inclusion partners:</strong> 6.15pm, 6.45pm, 7.15pm and 7.45pm at both campuses</li>
            <li><strong>T Levels:</strong> 6.30pm, 6.45pm, 7pm and 7.15pm at Moulsham Street; Construction and Engineering T Levels at 6.30pm and 7.30pm at Princes Road</li>
            <li><strong>Uniformed Services:</strong> 6.15pm, 6.45pm, 7.15pm and 7.45pm at Princes Road</li>
            </ul>
            <h3>Parking</h3>
            <p>Parking is available at both campuses, but it is limited. Street parking is available, but please check you are not parking in a residents-only zone.</p>
            <h3>More help or information</h3>
            <p>If you have any questions or need help reading or accessing this information, contact the Careers Advice Zone at <a href="mailto:careersadvicezone@chelmsford.ac.uk">careersadvicezone@chelmsford.ac.uk</a>. If you need help getting into the college, let us know before your visit.</p>
            """,
            "Group of Learners", "mailto:careersadvicezone@chelmsford.ac.uk?subject=Event%20booking%3A%20showcase-open-evening", "Book your arrival time"),

        new("Hair and Beauty taster day", "Taster day",
            new DateTime(2026, 12, 5, 10, 0, 0), new DateTime(2026, 12, 5, 13, 0, 0),
            "Moulsham Street campus, commercial salons",
            "Try cutting, styling and beauty treatments in our salons, with current students and tutors.",
            "<p>Spend a morning in our commercial salons. You'll try a cutting and styling session, a beauty treatment and a tour of the salons, then talk to current students about what the course is really like.</p><p>Suitable for Year 10 and 11 students. Parents and carers are welcome to stay.</p>",
            "Hairdressing", "mailto:careersadvicezone@chelmsford.ac.uk?subject=Event%20booking%3A%20hair-and-beauty-taster-day", "Book a taster place"),

        new("Construction workshop taster", "Taster day",
            new DateTime(2027, 1, 16, 10, 0, 0), new DateTime(2027, 1, 16, 13, 0, 0),
            "Princes Road campus, construction workshops",
            "Have a go at carpentry, plumbing and decorating in our construction workshops.",
            "<p>Make something to take home in the carpentry workshop, then try plumbing and decorating with our tutors. All tools and safety kit are provided.</p>",
            "Carpentry", "mailto:careersadvicezone@chelmsford.ac.uk?subject=Event%20booking%3A%20construction-workshop-taster", "Book a taster place"),

        new("Apprenticeships information evening", "Information evening",
            new DateTime(2027, 1, 21, 17, 30, 0), new DateTime(2027, 1, 21, 19, 0, 0),
            "Moulsham Street campus",
            "Find out how apprenticeships work, how to find an employer and what support the college gives you.",
            "<p>For students, parents and carers, and employers. Our apprenticeships team will explain how apprenticeships work, the levels available, and how we help you find an employer.</p>",
            "Students Learning", null, null),

        new("Fashion and textiles portfolio day", "Enrichment",
            new DateTime(2027, 2, 13, 10, 0, 0), new DateTime(2027, 2, 13, 15, 0, 0),
            "Moulsham Street campus, art and design studios",
            "Bring your sketchbook and get feedback on your portfolio from our creative tutors.",
            "<p>A day for anyone applying to our creative courses. Tutors will look at your work, suggest what to add before your interview, and show you round the studios.</p>",
            "Fashion Textiles", "mailto:careersadvicezone@chelmsford.ac.uk?subject=Event%20booking%3A%20fashion-and-textiles-portfolio-day", "Book a portfolio review")
    ];
}
