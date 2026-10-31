using System.Text.Json;
using Umbraco.Cms.Core;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.IO;
using Umbraco.Cms.Core.Models;
using Umbraco.Cms.Core.Notifications;
using Umbraco.Cms.Core.PropertyEditors;
using Umbraco.Cms.Core.Serialization;
using Umbraco.Cms.Core.Services;
using Umbraco.Cms.Core.Strings;
using Umbraco.Extensions;
using File = System.IO.File;

namespace Prototype.Web.Cms;

/// <summary>
/// Sets up the CMS the first time the prototype runs: page types, templates, the college's images in the media
/// library, and starter content for every page. After that the content belongs to whoever edits it in the
/// backoffice; the seeder never runs again once a home page exists.
/// </summary>
public sealed class ContentSeeder(
    IRuntimeState runtime,
    IContentTypeService contentTypes,
    IContentService content,
    IDocumentUrlService documentUrls,
    IDataTypeService dataTypes,
    ITemplateService templates,
    IMediaService media,
    MediaFileManager mediaFiles,
    MediaUrlGeneratorCollection mediaUrlGenerators,
    IContentTypeBaseServiceProvider contentTypeProviders,
    IShortStringHelper shortStrings,
    PropertyEditorCollection propertyEditors,
    IConfigurationEditorJsonSerializer configurationSerializer,
    IWebHostEnvironment environment,
    ILogger<ContentSeeder> logger) : INotificationAsyncHandler<UmbracoApplicationStartedNotification>
{
    private static readonly Guid SuperUser = Constants.Security.SuperUserKey;
    private static readonly string[] AllCultures = ["*"];

    public static readonly string[] EventTypes = ["Open event", "Taster day", "Information evening", "Enrichment"];

    public async Task HandleAsync(UmbracoApplicationStartedNotification notification, CancellationToken cancellationToken)
    {
        if (runtime.Level != RuntimeLevel.Run) return;
        if (content.GetRootContent().Any()) return;

        logger.LogInformation("Empty CMS: creating the prototype's page types, media and content");
        var types = await CreatePageTypesAsync();
        var images = CreateMedia();
        var home = CreateContent(types, images);

        // Pages published during start-up miss Umbraco's URL index, so build their URLs now
        await documentUrls.CreateOrUpdateUrlSegmentsWithDescendantsAsync(home.Key);
    }

    private sealed record PageTypes(IContentType Home, IContentType Courses, IContentType Subject, IContentType Events, IContentType Event, IContentType Payments);

    private async Task<PageTypes> CreatePageTypesAsync()
    {
        var text = await DataTypeAsync(Constants.DataTypes.Guids.TextstringGuid);
        var textarea = await DataTypeAsync(Constants.DataTypes.Guids.TextareaGuid);
        var richText = await DataTypeAsync(Constants.DataTypes.Guids.RichtextEditorGuid);
        var number = await DataTypeAsync(Constants.DataTypes.Guids.NumericGuid);
        var dateTime = await DataTypeAsync(Constants.DataTypes.Guids.DatePickerWithTimeGuid);
        var image = (await dataTypes.GetAllAsync()).First(d => d.Name == "Image Media Picker");
        var eventType = await EventTypeDropdownAsync() ?? text;
        await TemplateAsync("master", "Master");

        // Children first, so the parents can allow them
        var subject = await PageTypeAsync(DocTypes.SubjectArea.Alias, "Subject area", "icon-book-alt", template: false, t =>
        {
            Add(t, number, DocTypes.SubjectArea.SectionId, "Section ID", "content", "Content", "The S_ID of this subject in the college's course_sections table. Courses in this section use the image and intro below.", mandatory: true);
            Add(t, image, DocTypes.SubjectArea.Image, "Image", "content", "Content", "Shown at the top of every course page in this subject");
            Add(t, textarea, DocTypes.SubjectArea.Intro, "Intro", "content", "Content", "A sentence or two about the subject, shown on course pages");
        });

        var ev = await PageTypeAsync(DocTypes.Event.Alias, "Event", "icon-calendar-alt", template: true, t =>
        {
            Add(t, eventType, DocTypes.Event.EventType, "Event type", "details", "Details", "Used to filter the events list", mandatory: true);
            Add(t, dateTime, DocTypes.Event.Start, "Starts", "details", "Details", mandatory: true);
            Add(t, dateTime, DocTypes.Event.End, "Ends", "details", "Details");
            Add(t, text, DocTypes.Event.Location, "Location", "details", "Details", "Campus and room, e.g. Moulsham Street campus");
            Add(t, textarea, DocTypes.Event.Summary, "Summary", "details", "Details", "One or two sentences for the events list", mandatory: true);
            Add(t, image, DocTypes.Event.Image, "Image", "details", "Details");
            Add(t, richText, DocTypes.Event.Body, "Description", "description", "Description");
            Add(t, text, DocTypes.Event.BookingLink, "Booking link", "booking", "Booking", "Where to book. Leave empty for drop-in events.");
            Add(t, text, DocTypes.Event.BookingText, "Booking button text", "booking", "Booking", "e.g. Book your place");
            AddSeo(t, textarea);
        });

        var courses = await PageTypeAsync(DocTypes.CoursesPage.Alias, "Course search", "icon-search", template: true, t =>
        {
            Add(t, text, DocTypes.CoursesPage.Heading, "Heading", "content", "Content", mandatory: true);
            Add(t, textarea, DocTypes.CoursesPage.Intro, "Intro", "content", "Content");
            Add(t, image, DocTypes.CoursesPage.DefaultImage, "Default course image", "content", "Content", "Used for courses whose subject has no image of its own");
            AddSeo(t, textarea);
        }, children: [subject]);

        var events = await PageTypeAsync(DocTypes.EventsPage.Alias, "Events", "icon-calendar", template: true, t =>
        {
            Add(t, text, DocTypes.EventsPage.Heading, "Heading", "content", "Content", mandatory: true);
            Add(t, textarea, DocTypes.EventsPage.Intro, "Intro", "content", "Content");
            AddSeo(t, textarea);
        }, children: [ev]);

        var payments = await PageTypeAsync(DocTypes.PaymentsPage.Alias, "Pay online", "icon-credit-card", template: true, t =>
        {
            Add(t, text, DocTypes.PaymentsPage.Heading, "Heading", "content", "Content", mandatory: true);
            Add(t, textarea, DocTypes.PaymentsPage.Intro, "Intro", "content", "Content");
            Add(t, richText, DocTypes.PaymentsPage.HelpText, "Help text", "content", "Content", "Shown beside every step of the payment journey");
            Add(t, text, DocTypes.UrlName, "URL name", "seo", "SEO", "The last part of the page address. Umbraco uses this instead of the page name.");
            AddSeo(t, textarea);
        });

        var home = await PageTypeAsync(DocTypes.Home.Alias, "Home", "icon-home", template: true, t =>
        {
            t.AllowedAsRoot = true;
            Add(t, text, DocTypes.Home.NoticeText, "Notice text", "notice", "Notice bar", "A short message in the bar across the top of every page. Leave empty to hide it.");
            Add(t, text, DocTypes.Home.NoticeLink, "Notice link", "notice", "Notice bar");
            Add(t, text, DocTypes.Home.CampaignLabel, "Label", "campaign", "Campaign", "A few words above the heading, e.g. Applications open");
            Add(t, text, DocTypes.Home.CampaignHeading, "Heading", "campaign", "Campaign", mandatory: true);
            Add(t, textarea, DocTypes.Home.CampaignText, "Text", "campaign", "Campaign");
            Add(t, text, DocTypes.Home.CampaignButtonText, "Button text", "campaign", "Campaign");
            Add(t, text, DocTypes.Home.CampaignButtonLink, "Button link", "campaign", "Campaign");
            Add(t, image, DocTypes.Home.CampaignImage, "Image", "campaign", "Campaign");
            for (var n = 1; n <= 3; n++)
            {
                Add(t, text, DocTypes.Home.MessageTitle(n), $"Message {n} title", "messages", "Key messages");
                Add(t, textarea, DocTypes.Home.MessageText(n), $"Message {n} text", "messages", "Key messages");
                Add(t, text, DocTypes.Home.MessageLink(n), $"Message {n} link", "messages", "Key messages");
                Add(t, image, DocTypes.Home.MessageImage(n), $"Message {n} image", "messages", "Key messages");
            }
            Add(t, text, DocTypes.Home.VideoHeading, "Heading", "video", "Video");
            Add(t, textarea, DocTypes.Home.VideoText, "Text", "video", "Video");
            Add(t, text, DocTypes.Home.VideoUrl, "YouTube link", "video", "Video", "Paste the video's YouTube address. It only loads when someone presses play, so no cookies are set before then.");
            Add(t, image, DocTypes.Home.VideoImage, "Cover image", "video", "Video");
            AddSeo(t, textarea);
        }, children: [courses, events, payments]);

        return new PageTypes(home, courses, subject, events, ev, payments);
    }

    private async Task<IContentType> PageTypeAsync(string alias, string name, string icon, bool template, Action<ContentType> configure, IContentType[]? children = null)
    {
        var type = new ContentType(shortStrings, Constants.System.Root) { Alias = alias, Name = name, Icon = icon };
        configure(type);

        if (template && await TemplateAsync(alias, name) is { } view)
        {
            type.AllowedTemplates = [view];
            type.SetDefaultTemplate(view);
        }

        if (children is { Length: > 0 })
        {
            type.AllowedContentTypes = children.Select((c, i) => new ContentTypeSort(c.Key, i, c.Alias)).ToList();
        }

        var saved = await contentTypes.CreateAsync(type, SuperUser);
        if (!saved.Success) throw new InvalidOperationException($"Couldn't create the {alias} page type: {saved.Result}");
        return type;
    }

    /// <summary>
    /// Registers a view file in the repo as a CMS template. Templates name their layout ("master.cshtml"), so the
    /// master template has to be registered first.
    /// </summary>
    private async Task<ITemplate?> TemplateAsync(string alias, string name)
    {
        if (await templates.GetAsync(alias) is { } existing) return existing;

        var viewPath = Path.Combine(environment.ContentRootPath, "Views", $"{alias}.cshtml");
        var markup = File.Exists(viewPath) ? await File.ReadAllTextAsync(viewPath) : null;
        var created = await templates.CreateAsync(name, alias, markup, SuperUser);
        if (created.Success) return created.Result;

        logger.LogWarning("Couldn't create the {Alias} template: {Status}", alias, created.Status);
        return null;
    }

    private void Add(ContentType type, IDataType dataType, string alias, string name, string groupAlias, string groupName, string? description = null, bool mandatory = false)
    {
        var property = new PropertyType(shortStrings, dataType, alias) { Name = name, Description = description, Mandatory = mandatory };
        type.AddPropertyType(property, groupAlias, groupName);
    }

    private void AddSeo(ContentType type, IDataType textarea) =>
        Add(type, textarea, DocTypes.MetaDescription, "Meta description", "seo", "SEO", "What search engines show under the page title. About 150 characters.");

    private async Task<IDataType> DataTypeAsync(Guid key) =>
        await dataTypes.GetAsync(key) ?? throw new InvalidOperationException($"Umbraco's default data type {key} is missing");

    private async Task<IDataType?> EventTypeDropdownAsync()
    {
        if (!propertyEditors.TryGet(Constants.PropertyEditors.Aliases.DropDownListFlexible, out var editor)) return null;

        var dropdown = new DataType(editor, configurationSerializer)
        {
            Name = "Event type",
            EditorUiAlias = "Umb.PropertyEditorUi.Dropdown",
            ConfigurationData = new Dictionary<string, object> { ["items"] = EventTypes, ["multiple"] = false }
        };
        var created = await dataTypes.CreateAsync(dropdown, SuperUser);
        if (created.Success) return created.Result;

        logger.LogWarning("Couldn't create the Event type dropdown ({Status}); using a text box", created.Status);
        return null;
    }

    private Dictionary<string, IMedia> CreateMedia()
    {
        var folder = media.CreateMedia("College images", Constants.System.Root, Constants.Conventions.MediaTypes.Folder);
        media.Save(folder);

        var images = new Dictionary<string, IMedia>();
        foreach (var path in Directory.GetFiles(Path.Combine(environment.ContentRootPath, "SeedMedia"), "*.jpg").Order())
        {
            var name = Path.GetFileNameWithoutExtension(path).Replace("Carpentiry", "Carpentry");
            var item = media.CreateMedia(name, folder, Constants.Conventions.MediaTypes.Image);
            using (var stream = File.OpenRead(path))
            {
                item.SetValue(mediaFiles, mediaUrlGenerators, shortStrings, contentTypeProviders,
                    Constants.Conventions.Media.File, CourseImageFileName(path), stream);
            }

            media.Save(item);
            images[name] = item;
        }

        return images;
    }

    private static string CourseImageFileName(string path) =>
        Path.GetFileName(path).Replace(' ', '-').Replace("Carpentiry", "Carpentry").ToLowerInvariant();

    private IContent CreateContent(PageTypes types, IReadOnlyDictionary<string, IMedia> images)
    {
        string Pick(string name) => images.TryGetValue(name, out var m)
            ? JsonSerializer.Serialize(new[] { new { key = Guid.NewGuid(), mediaKey = m.Key } })
            : "";

        var home = Page("Home", Constants.System.Root, types.Home, new()
        {
            [DocTypes.Home.NoticeText] = "Showcase Open Evening: Wednesday 4 November, 6pm to 8.30pm. Come and meet our tutors.",
            [DocTypes.Home.NoticeLink] = "/events/showcase-open-evening/",
            [DocTypes.Home.CampaignLabel] = "Applications open for September 2027",
            [DocTypes.Home.CampaignHeading] = "Find the course that gets you there",
            [DocTypes.Home.CampaignText] = "Full-time courses, T Levels and apprenticeships for school leavers, and courses for adults returning to learning. Search by subject, level or the job you want.",
            [DocTypes.Home.CampaignButtonText] = "Come to an open event",
            [DocTypes.Home.CampaignButtonLink] = "/events/",
            [DocTypes.Home.CampaignImage] = Pick("Group of Learners"),
            [DocTypes.Home.MessageTitle(1)] = "Learn in real workshops",
            [DocTypes.Home.MessageText(1)] = "Train in professional kitchens, salons, studios and construction workshops, taught by people who have worked in the industry.",
            [DocTypes.Home.MessageLink(1)] = "/courses/",
            [DocTypes.Home.MessageImage(1)] = Pick("Catering"),
            [DocTypes.Home.MessageTitle(2)] = "Support when you need it",
            [DocTypes.Home.MessageText(2)] = "Help with SEND, travel, finance and careers advice from your first visit to your last day.",
            [DocTypes.Home.MessageLink(2)] = "/events/",
            [DocTypes.Home.MessageImage(2)] = Pick("Students Learning"),
            [DocTypes.Home.MessageTitle(3)] = "Pay fees and trips online",
            [DocTypes.Home.MessageText(3)] = "Deposits, materials fees and trips, paid securely by students or by parents and carers.",
            [DocTypes.Home.MessageLink(3)] = "/pay/",
            [DocTypes.Home.MessageImage(3)] = Pick("Example News Story"),
            [DocTypes.Home.VideoHeading] = "See college life for yourself",
            [DocTypes.Home.VideoText] = "Take a look around our campuses, workshops and studios, and hear from students about their first year.",
            [DocTypes.Home.VideoImage] = Pick("Students Learning"),
            [DocTypes.MetaDescription] = "Chelmsford College: full-time courses, T Levels, apprenticeships and adult learning in Chelmsford, Essex."
        });

        var courses = Page("Courses", home, types.Courses, new()
        {
            [DocTypes.CoursesPage.Heading] = "Find a course",
            [DocTypes.CoursesPage.Intro] = "Search by subject, level or the job you'd like to do. Every course here comes straight from the college's course database, so dates and fees are always up to date.",
            [DocTypes.CoursesPage.DefaultImage] = Pick("Students Learning"),
            [DocTypes.MetaDescription] = "Search full-time courses, T Levels and apprenticeships at Chelmsford College."
        });

        (int Section, string Name, string Image, string Intro)[] subjects =
        [
            (48, "Creative Industries", "Fashion Textiles", "Studios, workshops and industry briefs across fashion, graphics, media and performance."),
            (14, "Catering", "Catering", "Train in our professional kitchens and restaurant, cooking for real customers."),
            (12, "Hair & Beauty", "Hairdressing", "Work on real clients in our commercial salons from your first term."),
            (6, "Construction", "Carpentry", "Learn the trade in workshops built for carpentry, plumbing and decorating.")
        ];
        foreach (var s in subjects)
        {
            Page(s.Name, courses, types.Subject, new()
            {
                [DocTypes.SubjectArea.SectionId] = s.Section,
                [DocTypes.SubjectArea.Image] = Pick(s.Image),
                [DocTypes.SubjectArea.Intro] = s.Intro
            });
        }

        var events = Page("Events", home, types.Events, new()
        {
            [DocTypes.EventsPage.Heading] = "Open events and taster days",
            [DocTypes.EventsPage.Intro] = "Come and see the college for yourself. Meet tutors, look round the workshops and find out about support, travel and finance.",
            [DocTypes.MetaDescription] = "Open evenings, taster days and information events at Chelmsford College."
        });

        foreach (var e in SampleEvents.All)
        {
            Page(e.Name, events, types.Event, new()
            {
                [DocTypes.Event.EventType] = types.Event.PropertyTypes.First(p => p.Alias == DocTypes.Event.EventType).PropertyEditorAlias == Constants.PropertyEditors.Aliases.DropDownListFlexible
                    ? JsonSerializer.Serialize(new[] { e.Type })
                    : e.Type,
                [DocTypes.Event.Start] = e.Start,
                [DocTypes.Event.End] = e.End,
                [DocTypes.Event.Location] = e.Location,
                [DocTypes.Event.Summary] = e.Summary,
                [DocTypes.Event.Body] = RichText(e.Body),
                [DocTypes.Event.Image] = Pick(e.Image),
                [DocTypes.Event.BookingLink] = e.BookingLink,
                [DocTypes.Event.BookingText] = e.BookingText
            });
        }

        Page("Pay online", home, types.Payments, new()
        {
            [DocTypes.PaymentsPage.Heading] = "Pay online",
            [DocTypes.UrlName] = "pay",
            [DocTypes.PaymentsPage.Intro] = "Pay course deposits, materials fees and trips securely. Parents and carers can pay on a student's behalf: you'll just need the student's reference and date of birth.",
            [DocTypes.PaymentsPage.HelpText] = RichText("<p><strong>Need help paying?</strong></p><p>Email <a href=\"mailto:studentfees@chelmsford.ac.uk\">studentfees@chelmsford.ac.uk</a>. If cost is a barrier, ask about our bursaries and payment plans: we never want money to stop you starting a course.</p>"),
            [DocTypes.MetaDescription] = "Pay course deposits, materials fees and trips to Chelmsford College online."
        });

        content.PublishBranch(home, PublishBranchFilter.IncludeUnpublished, AllCultures);
        return home;
    }

    private IContent Page(string name, int parentId, IContentType type, Dictionary<string, object?> values)
    {
        var page = content.Create(name, parentId, type);
        return Save(page, values);
    }

    private IContent Page(string name, IContent parent, IContentType type, Dictionary<string, object?> values)
    {
        var page = content.Create(name, parent, type.Alias);
        return Save(page, values);
    }

    private IContent Save(IContent page, Dictionary<string, object?> values)
    {
        foreach (var (alias, value) in values)
        {
            if (value is not null and not "") page.SetValue(alias, value);
        }

        content.Save(page);
        return page;
    }

    /// <summary>The rich text editor stores its markup as JSON.</summary>
    private static string RichText(string html) => JsonSerializer.Serialize(new { markup = html, blocks = (object?)null });
}
