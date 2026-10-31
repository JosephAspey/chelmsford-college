namespace Prototype.Web.Startup;

/// <summary>
/// Pages that aren't CMS nodes: one course page per row in the college's courses table, and the steps of the
/// payment journey. Each still sits under a CMS page (Courses, Pay online) for its navigation and wording.
/// </summary>
public static class SiteRoutes
{
    public static void Map(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapControllerRoute("course", "courses/{id:int}/{slug?}", new { controller = "CourseDetail", action = "Index" });

        endpoints.MapControllerRoute("pay-start", "pay/{type:regex(^(deposit|materials|trips)$)}",
            new { controller = "PaymentJourney", action = "Find" });
        endpoints.MapControllerRoute("pay-choose", "pay/{type:regex(^(deposit|materials|trips)$)}/choose",
            new { controller = "PaymentJourney", action = "Choose" });
        endpoints.MapControllerRoute("pay-step", "pay/{id:guid}/{action:regex(^(details|review|return|receipt)$)}",
            new { controller = "PaymentJourney" });

        endpoints.MapControllerRoute("demo-provider", "demo-provider/checkout/{sessionId}/{action=Index}",
            new { controller = "DemoProvider" });
        endpoints.MapControllerRoute("event-calendar", "events/calendar/{key:guid}.ics",
            new { controller = "EventCalendar", action = "Download" });
    }
}
