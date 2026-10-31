var builder = DistributedApplication.CreateBuilder(args);

// One SQL Server in Docker, standing in for the college's SQL Server 2022 Standard. Persistent with a data
// volume, so CMS edits made during the demo survive restarts.
var sql = builder.AddSqlServer("sql")
    .WithLifetime(ContainerLifetime.Persistent)
    .WithDataVolume("chelmsford-prototype-sql");

// The CMS's own database, and a copy of the college's course tables built from the demo course data
var umbracoDb = sql.AddDatabase("umbraco", "Umbraco");
var collegeDb = sql.AddDatabase("collegedata", "CollegeData");

// Backoffice login for the demo, generated once and kept in the AppHost's user secrets
var adminPassword = builder.AddParameter("umbraco-admin-password",
    new GenerateParameterDefault { MinLength = 16, Special = false }, secret: true, persist: true);

builder.AddProject<Projects.Prototype_Web>("web")
    .WithReference(umbracoDb, connectionName: "umbracoDbDSN")
    .WithReference(collegeDb)
    .WaitFor(umbracoDb)
    .WaitFor(collegeDb)
    .WithEnvironment("ConnectionStrings__umbracoDbDSN_ProviderName", "Microsoft.Data.SqlClient")
    .WithEnvironment("Umbraco__CMS__Unattended__InstallUnattended", "true")
    .WithEnvironment("Umbraco__CMS__Unattended__UnattendedUserName", "Zim Digital")
    .WithEnvironment("Umbraco__CMS__Unattended__UnattendedUserEmail", "joseph@zimdigital.co.uk")
    .WithEnvironment("Umbraco__CMS__Unattended__UnattendedUserPassword", adminPassword)
    .WithExternalHttpEndpoints();

builder.Build().Run();
