namespace Prototype.Web.Cms;

/// <summary>Aliases of the CMS's page types and their properties, shared by the seeder and the views.</summary>
public static class DocTypes
{
    public static class Home
    {
        public const string Alias = "home";
        public const string NoticeText = "noticeText";
        public const string NoticeLink = "noticeLink";
        public const string CampaignLabel = "campaignLabel";
        public const string CampaignHeading = "campaignHeading";
        public const string CampaignText = "campaignText";
        public const string CampaignButtonText = "campaignButtonText";
        public const string CampaignButtonLink = "campaignButtonLink";
        public const string CampaignImage = "campaignImage";
        public const string VideoHeading = "videoHeading";
        public const string VideoText = "videoText";
        public const string VideoUrl = "videoUrl";
        public const string VideoImage = "videoImage";
        public static string MessageTitle(int n) => $"message{n}Title";
        public static string MessageText(int n) => $"message{n}Text";
        public static string MessageLink(int n) => $"message{n}Link";
        public static string MessageImage(int n) => $"message{n}Image";
    }

    public static class CoursesPage
    {
        public const string Alias = "coursesPage";
        public const string Heading = "heading";
        public const string Intro = "intro";
        public const string DefaultImage = "defaultImage";
    }

    public static class SubjectArea
    {
        public const string Alias = "subjectArea";
        public const string SectionId = "sectionId";
        public const string Image = "image";
        public const string Intro = "intro";
    }

    public static class EventsPage
    {
        public const string Alias = "eventsPage";
        public const string Heading = "heading";
        public const string Intro = "intro";
    }

    public static class Event
    {
        public const string Alias = "event";
        public const string EventType = "eventType";
        public const string Start = "startDate";
        public const string End = "endDate";
        public const string Location = "location";
        public const string Summary = "summary";
        public const string Body = "body";
        public const string Image = "image";
        public const string BookingLink = "bookingLink";
        public const string BookingText = "bookingText";
    }

    public static class PaymentsPage
    {
        public const string Alias = "paymentsPage";
        public const string Heading = "heading";
        public const string Intro = "intro";
        public const string HelpText = "helpText";
    }

    /// <summary>Every page type has these, on an SEO tab.</summary>
    public const string MetaDescription = "metaDescription";

    /// <summary>Umbraco's own convention: overrides the URL segment, so "Pay online" lives at /pay/.</summary>
    public const string UrlName = "umbracoUrlName";
}
