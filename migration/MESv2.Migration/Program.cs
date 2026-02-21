using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using MESv2.Api.Data;
using MESv2.Migration;
using MESv2.Migration.Readers;

var config = new ConfigurationBuilder()
    .SetBasePath(Directory.GetCurrentDirectory())
    .AddJsonFile("appsettings.json", optional: false)
    .AddCommandLine(args)
    .Build();

var v1ConnStr = config.GetConnectionString("V1")
    ?? throw new InvalidOperationException("V1 connection string is required. Set ConnectionStrings:V1 in appsettings.json or pass --ConnectionStrings:V1=\"...\"");
var v2ConnStr = config.GetConnectionString("V2")
    ?? throw new InvalidOperationException("V2 connection string is required. Set ConnectionStrings:V2 in appsettings.json or pass --ConnectionStrings:V2=\"...\"");

bool skipTestRows = config.GetValue("Migration:SkipTestRows", true);
string reportPath = config.GetValue("Migration:ReportPath", "migration-report.json")!;

Console.WriteLine("=== MES V1 -> V2 Data Migration Tool ===");
Console.WriteLine($"V1 source:  {MaskConnectionString(v1ConnStr)}");
Console.WriteLine($"V2 target:  {MaskConnectionString(v2ConnStr)}");
Console.WriteLine($"Skip test rows: {skipTestRows}");
Console.WriteLine();

MesDbContext CreateDbContext()
{
    var options = new DbContextOptionsBuilder<MesDbContext>()
        .UseSqlServer(v2ConnStr)
        .Options;
    return new MesDbContext(options);
}

// Apply EF Core migrations to ensure v2 database schema is up to date
using (var db = CreateDbContext())
{
    Console.WriteLine("Applying V2 database migrations...");
    await db.Database.MigrateAsync();
    Console.WriteLine("V2 schema ready.");
}

var logger = new MigrationLogger();

using var v1Reader = new V1Reader(v1ConnStr);
await v1Reader.OpenAsync();
Console.WriteLine("Connected to V1 database.");

var runner = new MigrationRunner(v1Reader, CreateDbContext, logger, skipTestRows);
await runner.RunAsync();

logger.SaveReport(reportPath);

// Phase 4: Validation
Console.WriteLine("\n=== Running Validation ===");
var validator = new MigrationValidator(v1Reader, CreateDbContext, logger);
await validator.ValidateAsync();

Console.WriteLine("\nMigration complete.");

static string MaskConnectionString(string connStr)
{
    var idx = connStr.IndexOf("Password=", StringComparison.OrdinalIgnoreCase);
    if (idx < 0) return connStr;
    var end = connStr.IndexOf(';', idx);
    return end > 0
        ? connStr[..idx] + "Password=***" + connStr[end..]
        : connStr[..idx] + "Password=***";
}
