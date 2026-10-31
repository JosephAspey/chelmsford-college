using Prototype.Core.CollegeData;
using Prototype.Core.Courses;
using Prototype.Core.Payments;
using Prototype.Web.Startup;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

#if DEBUG
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);
#endif

builder.AddServiceDefaults();

// The college's data: course tables (read only) and the payment records the site writes
var collegeData = builder.Configuration.GetConnectionString(CollegeDatabase.ConnectionName)
    ?? throw new InvalidOperationException($"Missing connection string '{CollegeDatabase.ConnectionName}'. Run the site through the AppHost.");
builder.Services.AddSingleton(new CollegeDatabase(collegeData));
builder.Services.AddSingleton<CollegeDataSeeder>();
builder.Services.AddHostedService<CollegeDataStartup>();

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICourseSource, SqlCourseSource>();
builder.Services.AddSingleton<CourseCatalogue>();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<ILearnerRecords, SqlLearnerRecords>();
builder.Services.AddSingleton<IPaymentStore, SqlPaymentStore>();
builder.Services.AddSingleton<DemoPaymentProvider>();
builder.Services.AddSingleton<IPaymentProvider>(sp => sp.GetRequiredService<DemoPaymentProvider>());
builder.Services.AddScoped<PaymentService>();

builder.Services.Configure<PrototypeOptions>(builder.Configuration.GetSection(PrototypeOptions.Section));

builder.CreateUmbracoBuilder()
    .AddBackOffice()
    .AddWebsite()
    .AddComposers()
    .Build();

WebApplication app = builder.Build();

await app.BootUmbracoAsync();

app.MapDefaultEndpoints();

app.UseUmbraco()
    .WithMiddleware(u =>
    {
        u.UseBackOffice();
        u.UseWebsite();
    })
    .WithEndpoints(u =>
    {
        SiteRoutes.Map(u.EndpointRouteBuilder);
        u.UseBackOfficeEndpoints();
        u.UseWebsiteEndpoints();
    });

await app.RunAsync();
