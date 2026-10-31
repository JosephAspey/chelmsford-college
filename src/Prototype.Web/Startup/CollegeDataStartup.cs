using Prototype.Core.CollegeData;

namespace Prototype.Web.Startup;

/// <summary>Builds the demo copy of the college's tables before the site takes requests.</summary>
public sealed class CollegeDataStartup(CollegeDataSeeder seeder) : IHostedService
{
    public Task StartAsync(CancellationToken cancellationToken) => seeder.SeedAsync(cancellationToken);

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public sealed class PrototypeOptions
{
    public const string Section = "Prototype";

    /// <summary>Shows the "How this page is built" notes for the demo. Off for a real launch.</summary>
    public bool ShowBuildNotes { get; init; } = true;
}
