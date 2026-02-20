using System.Diagnostics;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Migration.Readers;

namespace MESv2.Migration;

public class MigrationValidator
{
    private readonly V1Reader _v1;
    private readonly Func<MesDbContext> _dbFactory;
    private readonly MigrationLogger _log;

    public MigrationValidator(V1Reader v1, Func<MesDbContext> dbFactory, MigrationLogger log)
    {
        _v1 = v1;
        _dbFactory = dbFactory;
        _log = log;
    }

    public async Task ValidateAsync()
    {
        Console.WriteLine("\n--- Row Count Verification ---");
        await VerifyRowCountsAsync();

        Console.WriteLine("\n--- FK Integrity Checks ---");
        await CheckForeignKeyIntegrityAsync();

        Console.WriteLine("\n--- Spot Check: Random Serial Numbers ---");
        await SpotCheckSerialNumbersAsync(5);
    }

    private async Task VerifyRowCountsAsync()
    {
        var checks = new (string V1Table, string V2Table, Func<MesDbContext, Task<int>> V2Count)[]
        {
            ("mesSite", "Plants", async db => await db.Plants.CountAsync()),
            ("mesProductType", "ProductTypes", async db => await db.ProductTypes.CountAsync()),
            ("mesProduct", "Products", async db => await db.Products.CountAsync()),
            ("mesUser", "Users", async db => await db.Users.CountAsync()),
            ("mesVendor", "Vendors", async db => await db.Vendors.CountAsync()),
            ("mesWorkCenter", "WorkCenters", async db => await db.WorkCenters.CountAsync()),
            ("mesAsset", "Assets", async db => await db.Assets.CountAsync()),
            ("mesProductionLine", "ProductionLines", async db => await db.ProductionLines.CountAsync()),
            ("mesCharacteristic", "Characteristics", async db => await db.Characteristics.CountAsync()),
            ("mesControlPlan", "ControlPlans", async db => await db.ControlPlans.CountAsync()),
            ("mesDefectMaster", "DefectCodes", async db => await db.DefectCodes.CountAsync()),
            ("mesDefectLocation", "DefectLocations", async db => await db.DefectLocations.CountAsync()),
            ("mesAnnotationType", "AnnotationTypes", async db => await db.AnnotationTypes.CountAsync()),
            ("mesKanbanCards", "BarcodeCards", async db => await db.BarcodeCards.CountAsync()),
            ("mesSerialNumberMaster", "SerialNumbers", async db => await db.SerialNumbers.CountAsync()),
            ("mesManufacturingLog", "ProductionRecords", async db => await db.ProductionRecords.CountAsync()),
            ("mesManufacturingLogWelder", "WelderLogs", async db => await db.WelderLogs.CountAsync()),
            ("mesManufacturingTraceLog", "TraceabilityLogs", async db => await db.TraceabilityLogs.CountAsync()),
            ("mesManufacturingInspectionsLog", "InspectionRecords", async db => await db.InspectionRecords.CountAsync()),
            ("mesDefectLog", "DefectLogs", async db => await db.DefectLogs.CountAsync()),
            ("mesAnnotation", "Annotations", async db => await db.Annotations.CountAsync()),
            ("mesChangeLog", "ChangeLogs", async db => await db.ChangeLogs.CountAsync()),
            ("mesSpotXrayIncrement", "SpotXrayIncrements", async db => await db.SpotXrayIncrements.CountAsync()),
            ("mesSiteSchedule", "SiteSchedules", async db => await db.SiteSchedules.CountAsync()),
        };

        Console.WriteLine($"{"V1 Table",-35} {"V1 Rows",10} {"V2 Rows",10} {"Status",10}");
        Console.WriteLine(new string('-', 70));

        using var db = _dbFactory();
        foreach (var (v1Table, v2Table, v2CountFn) in checks)
        {
            try
            {
                var v1Count = await _v1.CountAsync(v1Table);
                var v2Count = await v2CountFn(db);
                var status = v2Count >= v1Count * 0.9 ? "OK" : "MISMATCH";

                if (status == "MISMATCH")
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                }
                Console.WriteLine($"{v1Table,-35} {v1Count,10} {v2Count,10} {status,10}");
                Console.ResetColor();
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"{v1Table,-35} {"ERR",10} {"ERR",10} {ex.Message}");
                Console.ResetColor();
            }
        }
    }

    private async Task CheckForeignKeyIntegrityAsync()
    {
        using var db = _dbFactory();

        // Check ProductionRecords have valid SerialNumbers
        var orphanedPR = await db.ProductionRecords
            .Where(r => !db.SerialNumbers.Any(s => s.Id == r.SerialNumberId))
            .CountAsync();
        ReportIntegrity("ProductionRecord.SerialNumberId", orphanedPR);

        // Check WelderLogs have valid ProductionRecords
        var orphanedWL = await db.WelderLogs
            .Where(w => !db.ProductionRecords.Any(r => r.Id == w.ProductionRecordId))
            .CountAsync();
        ReportIntegrity("WelderLog.ProductionRecordId", orphanedWL);

        // Check DefectLogs have valid DefectCodes
        var orphanedDL = await db.DefectLogs
            .Where(d => !db.DefectCodes.Any(c => c.Id == d.DefectCodeId))
            .CountAsync();
        ReportIntegrity("DefectLog.DefectCodeId", orphanedDL);

        // Check Annotations have valid ProductionRecords
        var orphanedAn = await db.Annotations
            .Where(a => !db.ProductionRecords.Any(r => r.Id == a.ProductionRecordId))
            .CountAsync();
        ReportIntegrity("Annotation.ProductionRecordId", orphanedAn);

        // Check Users have valid DefaultSiteId
        var orphanedUser = await db.Users
            .Where(u => !db.Plants.Any(p => p.Id == u.DefaultSiteId))
            .CountAsync();
        ReportIntegrity("User.DefaultSiteId", orphanedUser);
    }

    private static void ReportIntegrity(string fkName, int orphanCount)
    {
        if (orphanCount == 0)
        {
            Console.WriteLine($"  {fkName}: OK");
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"  {fkName}: {orphanCount} orphaned records!");
            Console.ResetColor();
        }
    }

    private async Task SpotCheckSerialNumbersAsync(int sampleSize)
    {
        using var db = _dbFactory();
        var randomSNs = await db.SerialNumbers
            .OrderBy(_ => Guid.NewGuid())
            .Take(sampleSize)
            .ToListAsync();

        foreach (var sn in randomSNs)
        {
            var prCount = await db.ProductionRecords
                .Where(r => r.SerialNumberId == sn.Id)
                .CountAsync();
            var defectCount = await db.DefectLogs
                .Where(d => d.SerialNumber == sn.Serial)
                .CountAsync();
            var traceCount = await db.TraceabilityLogs
                .Where(t => t.FromSerialNumberId == sn.Id || t.ToSerialNumberId == sn.Id)
                .CountAsync();

            Console.WriteLine($"  SN '{sn.Serial}' (Site {sn.SiteCode}): {prCount} production records, {defectCount} defects, {traceCount} trace logs");
        }
    }
}
