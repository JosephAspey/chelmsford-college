using Microsoft.Data.SqlClient;

namespace Prototype.Core.CollegeData;

/// <summary>Opens connections to the college's data. In the live build this is a read-only login on their SQL Server.</summary>
public sealed class CollegeDatabase(string connectionString)
{
    /// <summary>The connection string name Aspire passes in (ConnectionStrings:collegedata).</summary>
    public const string ConnectionName = "collegedata";

    public async Task<SqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
