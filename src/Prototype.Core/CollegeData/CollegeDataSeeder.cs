using System.Reflection;
using System.Text.RegularExpressions;
using Dapper;
using Microsoft.Extensions.Logging;

namespace Prototype.Core.CollegeData;

/// <summary>
/// Builds the demo copy of the college's course tables (from the demo course data they supplied) and the demo
/// payment tables, the first time the prototype runs against an empty database. The live site never does this:
/// it reads the college's own tables.
/// </summary>
public sealed partial class CollegeDataSeeder(CollegeDatabase database, ILogger<CollegeDataSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        await RunIfMissingAsync("courses", "college-data.sql", cancellationToken);
        await RunIfMissingAsync("web_payments", "demo-payments.sql", cancellationToken);
    }

    private async Task RunIfMissingAsync(string table, string script, CancellationToken cancellationToken)
    {
        await using var connection = await database.OpenAsync(cancellationToken);
        var exists = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(*) FROM sys.tables WHERE name = @table", new { table }, cancellationToken: cancellationToken));
        if (exists > 0) return;

        logger.LogInformation("Creating {Table} and its neighbours from {Script}", table, script);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        foreach (var batch in Batches(ReadScript(script)))
        {
            await connection.ExecuteAsync(new CommandDefinition(batch, transaction: transaction, cancellationToken: cancellationToken));
        }

        await transaction.CommitAsync(cancellationToken);
    }

    internal static IEnumerable<string> Batches(string script) =>
        GoLine().Split(script).Select(b => b.Trim()).Where(b => b.Length > 0 && !b.Split('\n').All(l => l.TrimStart().StartsWith("--") || l.Trim().Length == 0));

    private static string ReadScript(string name)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resource = assembly.GetManifestResourceNames().Single(n => n.EndsWith(name, StringComparison.Ordinal));
        using var stream = assembly.GetManifestResourceStream(resource)!;
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    [GeneratedRegex(@"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase)]
    private static partial Regex GoLine();
}
