using Microsoft.EntityFrameworkCore;
using MESv2.Api.Models;

namespace MESv2.Api.Data;

public class MesDbContext : DbContext
{
    public MesDbContext(DbContextOptions<MesDbContext> options) : base(options) { }

    public DbSet<Plant> Plants => Set<Plant>();
    public DbSet<ProductionLine> ProductionLines => Set<ProductionLine>();
    public DbSet<WorkCenter> WorkCenters => Set<WorkCenter>();
    public DbSet<WorkCenterType> WorkCenterTypes => Set<WorkCenterType>();
    public DbSet<Asset> Assets => Set<Asset>();
    public DbSet<User> Users => Set<User>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<ProductType> ProductTypes => Set<ProductType>();
    public DbSet<SerialNumber> SerialNumbers => Set<SerialNumber>();
    public DbSet<ProductionRecord> ProductionRecords => Set<ProductionRecord>();
    public DbSet<WelderLog> WelderLogs => Set<WelderLog>();
    public DbSet<TraceabilityLog> TraceabilityLogs => Set<TraceabilityLog>();
    public DbSet<InspectionRecord> InspectionRecords => Set<InspectionRecord>();
    public DbSet<ControlPlan> ControlPlans => Set<ControlPlan>();
    public DbSet<DefectLog> DefectLogs => Set<DefectLog>();
    public DbSet<DefectCode> DefectCodes => Set<DefectCode>();
    public DbSet<DefectLocation> DefectLocations => Set<DefectLocation>();
    public DbSet<Characteristic> Characteristics => Set<Characteristic>();
    public DbSet<CharacteristicWorkCenter> CharacteristicWorkCenters => Set<CharacteristicWorkCenter>();
    public DbSet<DefectWorkCenter> DefectWorkCenters => Set<DefectWorkCenter>();
    public DbSet<MaterialQueueItem> MaterialQueueItems => Set<MaterialQueueItem>();
    public DbSet<BarcodeCard> BarcodeCards => Set<BarcodeCard>();
    public DbSet<Assembly> Assemblies => Set<Assembly>();
    public DbSet<Annotation> Annotations => Set<Annotation>();
    public DbSet<AnnotationType> AnnotationTypes => Set<AnnotationType>();
    public DbSet<PlantGear> PlantGears => Set<PlantGear>();
    public DbSet<ChangeLog> ChangeLogs => Set<ChangeLog>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<XrayQueueItem> XrayQueueItems => Set<XrayQueueItem>();
    public DbSet<RoundSeamSetup> RoundSeamSetups => Set<RoundSeamSetup>();
    public DbSet<NameplateRecord> NameplateRecords => Set<NameplateRecord>();
    public DbSet<HydroRecord> HydroRecords => Set<HydroRecord>();
    public DbSet<ActiveSession> ActiveSessions => Set<ActiveSession>();
    public DbSet<SpotXrayIncrement> SpotXrayIncrements => Set<SpotXrayIncrement>();
    public DbSet<SiteSchedule> SiteSchedules => Set<SiteSchedule>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ----- Relationships -----

        modelBuilder.Entity<Plant>()
            .HasOne(p => p.CurrentPlantGear)
            .WithMany()
            .HasForeignKey(p => p.CurrentPlantGearId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ActiveSession>()
            .HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ActiveSession>()
            .HasOne(s => s.WorkCenter)
            .WithMany()
            .HasForeignKey(s => s.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ActiveSession>()
            .HasOne(s => s.ProductionLine)
            .WithMany()
            .HasForeignKey(s => s.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ActiveSession>()
            .HasOne(s => s.Asset)
            .WithMany()
            .HasForeignKey(s => s.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ActiveSession>()
            .HasIndex(s => s.UserId);
        modelBuilder.Entity<ActiveSession>()
            .HasIndex(s => s.SiteCode);

        modelBuilder.Entity<ProductionLine>()
            .HasOne(p => p.Plant)
            .WithMany(p => p.ProductionLines)
            .HasForeignKey(p => p.PlantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkCenter>()
            .HasOne(w => w.Plant)
            .WithMany(p => p.WorkCenters)
            .HasForeignKey(w => w.PlantId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkCenter>()
            .HasOne(w => w.WorkCenterType)
            .WithMany(t => t.WorkCenters)
            .HasForeignKey(w => w.WorkCenterTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkCenter>()
            .HasOne(w => w.ProductionLine)
            .WithMany(l => l.WorkCenters)
            .HasForeignKey(w => w.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Asset>()
            .HasOne(a => a.WorkCenter)
            .WithMany(w => w.Assets)
            .HasForeignKey(a => a.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<User>()
            .HasOne(u => u.DefaultSite)
            .WithMany(p => p.Users)
            .HasForeignKey(u => u.DefaultSiteId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Product>()
            .HasOne(p => p.ProductType)
            .WithMany(t => t.Products)
            .HasForeignKey(p => p.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.Product)
            .WithMany(p => p.SerialNumbers)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.MillVendor)
            .WithMany()
            .HasForeignKey(s => s.MillVendorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.ProcessorVendor)
            .WithMany()
            .HasForeignKey(s => s.ProcessorVendorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.HeadsVendor)
            .WithMany()
            .HasForeignKey(s => s.HeadsVendorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.ReplaceBySN)
            .WithMany()
            .HasForeignKey(s => s.ReplaceBySNId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SerialNumber>()
            .HasOne(s => s.ModifiedByUser)
            .WithMany()
            .HasForeignKey(s => s.ModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ProductionRecord>()
            .HasOne(r => r.SerialNumber)
            .WithMany(s => s.ProductionRecords)
            .HasForeignKey(r => r.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionRecord>()
            .HasOne(r => r.WorkCenter)
            .WithMany(w => w.ProductionRecords)
            .HasForeignKey(r => r.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionRecord>()
            .HasOne(r => r.Asset)
            .WithMany()
            .HasForeignKey(r => r.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionRecord>()
            .HasOne(r => r.ProductionLine)
            .WithMany()
            .HasForeignKey(r => r.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionRecord>()
            .HasOne(r => r.Operator)
            .WithMany()
            .HasForeignKey(r => r.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ProductionRecord>()
            .HasOne(r => r.PlantGear)
            .WithMany(g => g.ProductionRecords)
            .HasForeignKey(r => r.PlantGearId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WelderLog>()
            .HasOne(w => w.ProductionRecord)
            .WithMany(r => r.WelderLogs)
            .HasForeignKey(w => w.ProductionRecordId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WelderLog>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WelderLog>()
            .HasOne(w => w.Characteristic)
            .WithMany(c => c.WelderLogs)
            .HasForeignKey(w => w.CharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Characteristic>()
            .HasOne(c => c.ProductType)
            .WithMany(t => t.Characteristics)
            .HasForeignKey(c => c.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<InspectionRecord>()
            .HasOne(i => i.WorkCenter)
            .WithMany(w => w.InspectionRecords)
            .HasForeignKey(i => i.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InspectionRecord>()
            .HasOne(i => i.Operator)
            .WithMany()
            .HasForeignKey(i => i.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<InspectionRecord>()
            .HasOne(i => i.ControlPlan)
            .WithMany(c => c.InspectionRecords)
            .HasForeignKey(i => i.ControlPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ControlPlan>()
            .HasOne(c => c.Characteristic)
            .WithMany()
            .HasForeignKey(c => c.CharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<ControlPlan>()
            .HasOne(c => c.WorkCenter)
            .WithMany(w => w.ControlPlans)
            .HasForeignKey(c => c.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DefectLog>()
            .HasOne(d => d.ProductionRecord)
            .WithMany(r => r.DefectLogs)
            .HasForeignKey(d => d.ProductionRecordId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DefectLog>()
            .HasOne(d => d.InspectionRecord)
            .WithMany(i => i.DefectLogs)
            .HasForeignKey(d => d.InspectionRecordId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DefectLog>()
            .HasOne(d => d.DefectCode)
            .WithMany(c => c.DefectLogs)
            .HasForeignKey(d => d.DefectCodeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DefectLog>()
            .HasOne(d => d.Characteristic)
            .WithMany(c => c.DefectLogs)
            .HasForeignKey(d => d.CharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DefectLog>()
            .HasOne(d => d.Location)
            .WithMany(l => l.DefectLogs)
            .HasForeignKey(d => d.LocationId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DefectLog>()
            .HasOne(d => d.RepairedByUser)
            .WithMany()
            .HasForeignKey(d => d.RepairedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DefectLocation>()
            .HasOne(d => d.Characteristic)
            .WithMany(c => c.DefectLocations)
            .HasForeignKey(d => d.CharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CharacteristicWorkCenter>()
            .HasOne(c => c.Characteristic)
            .WithMany()
            .HasForeignKey(c => c.CharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<CharacteristicWorkCenter>()
            .HasOne(c => c.WorkCenter)
            .WithMany(w => w.CharacteristicWorkCenters)
            .HasForeignKey(c => c.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DefectWorkCenter>()
            .HasOne(d => d.DefectCode)
            .WithMany()
            .HasForeignKey(d => d.DefectCodeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DefectWorkCenter>()
            .HasOne(d => d.WorkCenter)
            .WithMany(w => w.DefectWorkCenters)
            .HasForeignKey(d => d.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<DefectWorkCenter>()
            .HasOne(d => d.EarliestDetectionWorkCenter)
            .WithMany()
            .HasForeignKey(d => d.EarliestDetectionWorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<MaterialQueueItem>()
            .HasOne(m => m.WorkCenter)
            .WithMany(w => w.MaterialQueueItems)
            .HasForeignKey(m => m.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkCenter>()
            .HasOne(w => w.MaterialQueueForWC)
            .WithMany()
            .HasForeignKey(w => w.MaterialQueueForWCId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Assembly>()
            .HasOne(a => a.WorkCenter)
            .WithMany(w => w.Assemblies)
            .HasForeignKey(a => a.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Assembly>()
            .HasOne(a => a.Asset)
            .WithMany()
            .HasForeignKey(a => a.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Assembly>()
            .HasOne(a => a.ProductionLine)
            .WithMany()
            .HasForeignKey(a => a.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Assembly>()
            .HasOne(a => a.Operator)
            .WithMany()
            .HasForeignKey(a => a.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Annotation>()
            .HasOne(a => a.ProductionRecord)
            .WithMany(r => r.Annotations)
            .HasForeignKey(a => a.ProductionRecordId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Annotation>()
            .HasOne(a => a.AnnotationType)
            .WithMany(t => t.Annotations)
            .HasForeignKey(a => a.AnnotationTypeId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Annotation>()
            .HasOne(a => a.InitiatedByUser)
            .WithMany()
            .HasForeignKey(a => a.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Annotation>()
            .HasOne(a => a.ResolvedByUser)
            .WithMany()
            .HasForeignKey(a => a.ResolvedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<PlantGear>()
            .HasOne(g => g.Plant)
            .WithMany(p => p.PlantGears)
            .HasForeignKey(g => g.PlantId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<ChangeLog>()
            .HasOne(c => c.ChangeByUser)
            .WithMany()
            .HasForeignKey(c => c.ChangeByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<XrayQueueItem>()
            .HasOne(x => x.WorkCenter)
            .WithMany()
            .HasForeignKey(x => x.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<XrayQueueItem>()
            .HasOne(x => x.Operator)
            .WithMany()
            .HasForeignKey(x => x.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<RoundSeamSetup>()
            .HasOne(r => r.WorkCenter)
            .WithMany()
            .HasForeignKey(r => r.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<NameplateRecord>()
            .HasOne(n => n.Product)
            .WithMany()
            .HasForeignKey(n => n.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<NameplateRecord>()
            .HasOne(n => n.WorkCenter)
            .WithMany()
            .HasForeignKey(n => n.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<NameplateRecord>()
            .HasOne(n => n.Operator)
            .WithMany()
            .HasForeignKey(n => n.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<HydroRecord>()
            .HasOne(h => h.WorkCenter)
            .WithMany()
            .HasForeignKey(h => h.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<HydroRecord>()
            .HasOne(h => h.Asset)
            .WithMany()
            .HasForeignKey(h => h.AssetId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<HydroRecord>()
            .HasOne(h => h.Operator)
            .WithMany()
            .HasForeignKey(h => h.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<DefectLog>()
            .HasOne(d => d.HydroRecord)
            .WithMany(h => h.DefectLogs)
            .HasForeignKey(d => d.HydroRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<SpotXrayIncrement>()
            .HasOne(s => s.ProductionRecord)
            .WithMany()
            .HasForeignKey(s => s.ManufacturingLogId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SpotXrayIncrement>()
            .HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<SpotXrayIncrement>()
            .HasOne(s => s.ModifiedByUser)
            .WithMany()
            .HasForeignKey(s => s.ModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        // ----- Indexes -----
        modelBuilder.Entity<Plant>().HasIndex(p => p.Code).IsUnique();
        modelBuilder.Entity<SerialNumber>().HasIndex(s => s.Serial);
        modelBuilder.Entity<User>().HasIndex(u => u.EmployeeNumber);
        modelBuilder.Entity<MaterialQueueItem>().HasIndex(m => new { m.WorkCenterId, m.Status });
        modelBuilder.Entity<ProductionRecord>().HasIndex(r => new { r.SerialNumberId, r.WorkCenterId, r.Timestamp });
        modelBuilder.Entity<TraceabilityLog>().HasIndex(t => t.Timestamp);
        modelBuilder.Entity<DefectCode>().HasIndex(d => d.Code);
        modelBuilder.Entity<BarcodeCard>().HasIndex(b => b.CardValue);
        modelBuilder.Entity<XrayQueueItem>().HasIndex(x => x.WorkCenterId);
        modelBuilder.Entity<NameplateRecord>().HasIndex(n => n.SerialNumber);
        modelBuilder.Entity<HydroRecord>().HasIndex(h => h.AssemblyAlphaCode);
        modelBuilder.Entity<RoundSeamSetup>().HasIndex(r => new { r.WorkCenterId, r.CreatedAt });
        modelBuilder.Entity<Vendor>().HasIndex(v => new { v.VendorType, v.SiteCode });
        modelBuilder.Entity<SerialNumber>().HasIndex(s => s.SiteCode);
        modelBuilder.Entity<SpotXrayIncrement>().HasIndex(s => s.ManufacturingLogId);
        modelBuilder.Entity<SiteSchedule>().HasIndex(s => s.SiteCode);

        // ----- Seed data -----
        var plant1Id = Guid.Parse("11111111-1111-1111-1111-111111111111"); // Cleveland (000)
        var plant2Id = Guid.Parse("22222222-2222-2222-2222-222222222222"); // Fremont (600)
        var plant3Id = Guid.Parse("33333333-3333-3333-3333-333333333333"); // West Jordan (700)

        modelBuilder.Entity<Plant>().HasData(
            new Plant { Id = plant1Id, Code = "000", Name = "Cleveland", TimeZoneId = "America/Chicago" },
            new Plant { Id = plant2Id, Code = "600", Name = "Fremont", TimeZoneId = "America/New_York" },
            new Plant { Id = plant3Id, Code = "700", Name = "West Jordan", TimeZoneId = "America/Denver" }
        );

        // Work center types (10)
        var wctRollsId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var wctLongSeamId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var wctInspectionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var wctFitupId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");
        var wctRoundSeamId = Guid.Parse("eeeeeeee-eeee-eeee-eeee-eeeeeeeeeeee");
        var wctNameplateId = Guid.Parse("f0f0f0f0-f0f0-f0f0-f0f0-f0f0f0f0f0f0");
        var wctHydroId = Guid.Parse("f1f1f1f1-f1f1-f1f1-f1f1-f1f1f1f1f1f1");
        var wctXrayId = Guid.Parse("f2f2f2f2-f2f2-f2f2-f2f2-f2f2f2f2f2f2");
        var wctSpotXrayId = Guid.Parse("f3f3f3f3-f3f3-f3f3-f3f3-f3f3f3f3f3f3");
        var wctMaterialQueueId = Guid.Parse("f4f4f4f4-f4f4-f4f4-f4f4-f4f4f4f4f4f4");

        modelBuilder.Entity<WorkCenterType>().HasData(
            new WorkCenterType { Id = wctRollsId, Name = "Rolls" },
            new WorkCenterType { Id = wctLongSeamId, Name = "Long Seam" },
            new WorkCenterType { Id = wctInspectionId, Name = "Inspection" },
            new WorkCenterType { Id = wctFitupId, Name = "Fitup" },
            new WorkCenterType { Id = wctRoundSeamId, Name = "Round Seam" },
            new WorkCenterType { Id = wctNameplateId, Name = "Nameplate" },
            new WorkCenterType { Id = wctHydroId, Name = "Hydro" },
            new WorkCenterType { Id = wctXrayId, Name = "X-Ray" },
            new WorkCenterType { Id = wctSpotXrayId, Name = "Spot X-Ray" },
            new WorkCenterType { Id = wctMaterialQueueId, Name = "Material Queue" }
        );

        // Production lines: 2 Cleveland, 2 Fremont, 1 West Jordan
        var line1Plt1 = Guid.Parse("e1111111-1111-1111-1111-111111111111");
        var line2Plt1 = Guid.Parse("e2111111-1111-1111-1111-111111111111");
        var line1Plt2 = Guid.Parse("e1222222-2222-2222-2222-222222222222");
        var line2Plt2 = Guid.Parse("e2222222-2222-2222-2222-222222222222");
        var line1Plt3 = Guid.Parse("e1333333-3333-3333-3333-333333333333");

        modelBuilder.Entity<ProductionLine>().HasData(
            new ProductionLine { Id = line1Plt1, Name = "Line 1", PlantId = plant1Id },
            new ProductionLine { Id = line2Plt1, Name = "Line 2", PlantId = plant1Id },
            new ProductionLine { Id = line1Plt2, Name = "Line 1", PlantId = plant2Id },
            new ProductionLine { Id = line2Plt2, Name = "Line 2", PlantId = plant2Id },
            new ProductionLine { Id = line1Plt3, Name = "Line 1", PlantId = plant3Id }
        );

        // Work centers per plant (12 each, tied to Line 1). Cleveland IDs preserved for tests.
        var wcRolls1Plt1 = Guid.Parse("f1111111-1111-1111-1111-111111111111");
        var wcLongSeam1Plt1 = Guid.Parse("f2111111-1111-1111-1111-111111111111");
        var wcLongSeamInspPlt1 = Guid.Parse("f3111111-1111-1111-1111-111111111111");
        var wcRtXrayQueuePlt1 = Guid.Parse("f4111111-1111-1111-1111-111111111111");
        var wcFitupPlt1 = Guid.Parse("f5111111-1111-1111-1111-111111111111");
        var wcRoundSeamPlt1 = Guid.Parse("f6111111-1111-1111-1111-111111111111");
        var wcRoundSeamInspPlt1 = Guid.Parse("f7111111-1111-1111-1111-111111111111");
        var wcSpotXrayPlt1 = Guid.Parse("f8111111-1111-1111-1111-111111111111");
        var wcNameplatePlt1 = Guid.Parse("f9111111-1111-1111-1111-111111111111");
        var wcHydroPlt1 = Guid.Parse("fa111111-1111-1111-1111-111111111111");
        var wcRollsMaterialPlt1 = Guid.Parse("fb111111-1111-1111-1111-111111111111");
        var wcFitupQueuePlt1 = Guid.Parse("fc111111-1111-1111-1111-111111111111");

        modelBuilder.Entity<WorkCenter>().HasData(
            new WorkCenter { Id = wcRolls1Plt1, Name = "Rolls 1", PlantId = plant1Id, WorkCenterTypeId = wctRollsId, ProductionLineId = line1Plt1, NumberOfWelders = 1, DataEntryType = null },
            new WorkCenter { Id = wcLongSeam1Plt1, Name = "Long Seam 1", PlantId = plant1Id, WorkCenterTypeId = wctLongSeamId, ProductionLineId = line1Plt1, NumberOfWelders = 1, DataEntryType = "standard" },
            new WorkCenter { Id = wcLongSeamInspPlt1, Name = "Long Seam Inspection", PlantId = plant1Id, WorkCenterTypeId = wctInspectionId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = "inspection" },
            new WorkCenter { Id = wcRtXrayQueuePlt1, Name = "RT X-ray Queue", PlantId = plant1Id, WorkCenterTypeId = wctXrayId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = null },
            new WorkCenter { Id = wcFitupPlt1, Name = "Fitup", PlantId = plant1Id, WorkCenterTypeId = wctFitupId, ProductionLineId = line1Plt1, NumberOfWelders = 1, DataEntryType = null },
            new WorkCenter { Id = wcRoundSeamPlt1, Name = "Round Seam", PlantId = plant1Id, WorkCenterTypeId = wctRoundSeamId, ProductionLineId = line1Plt1, NumberOfWelders = 1, DataEntryType = null },
            new WorkCenter { Id = wcRoundSeamInspPlt1, Name = "Round Seam Inspection", PlantId = plant1Id, WorkCenterTypeId = wctInspectionId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = "inspection" },
            new WorkCenter { Id = wcSpotXrayPlt1, Name = "Spot X-ray", PlantId = plant1Id, WorkCenterTypeId = wctSpotXrayId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = null },
            new WorkCenter { Id = wcNameplatePlt1, Name = "Nameplate", PlantId = plant1Id, WorkCenterTypeId = wctNameplateId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = null },
            new WorkCenter { Id = wcHydroPlt1, Name = "Hydro", PlantId = plant1Id, WorkCenterTypeId = wctHydroId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = null },
            new WorkCenter { Id = wcRollsMaterialPlt1, Name = "Rolls Material", PlantId = plant1Id, WorkCenterTypeId = wctMaterialQueueId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = null, MaterialQueueForWCId = wcRolls1Plt1 },
            new WorkCenter { Id = wcFitupQueuePlt1, Name = "Fitup Queue", PlantId = plant1Id, WorkCenterTypeId = wctMaterialQueueId, ProductionLineId = line1Plt1, NumberOfWelders = 0, DataEntryType = null, MaterialQueueForWCId = wcFitupPlt1 },
            new WorkCenter { Id = Guid.Parse("f1222222-2222-2222-2222-222222222222"), Name = "Rolls 1", PlantId = plant2Id, WorkCenterTypeId = wctRollsId, ProductionLineId = line1Plt2, NumberOfWelders = 1, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("f2222222-2222-2222-2222-222222222222"), Name = "Long Seam 1", PlantId = plant2Id, WorkCenterTypeId = wctLongSeamId, ProductionLineId = line1Plt2, NumberOfWelders = 1, DataEntryType = "standard" },
            new WorkCenter { Id = Guid.Parse("f3222222-2222-2222-2222-222222222222"), Name = "Long Seam Inspection", PlantId = plant2Id, WorkCenterTypeId = wctInspectionId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = "inspection" },
            new WorkCenter { Id = Guid.Parse("f4222222-2222-2222-2222-222222222222"), Name = "RT X-ray Queue", PlantId = plant2Id, WorkCenterTypeId = wctXrayId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("f5222222-2222-2222-2222-222222222222"), Name = "Fitup", PlantId = plant2Id, WorkCenterTypeId = wctFitupId, ProductionLineId = line1Plt2, NumberOfWelders = 1, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("f6222222-2222-2222-2222-222222222222"), Name = "Round Seam", PlantId = plant2Id, WorkCenterTypeId = wctRoundSeamId, ProductionLineId = line1Plt2, NumberOfWelders = 1, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("f7222222-2222-2222-2222-222222222222"), Name = "Round Seam Inspection", PlantId = plant2Id, WorkCenterTypeId = wctInspectionId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = "inspection" },
            new WorkCenter { Id = Guid.Parse("f8222222-2222-2222-2222-222222222222"), Name = "Spot X-ray", PlantId = plant2Id, WorkCenterTypeId = wctSpotXrayId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("f9222222-2222-2222-2222-222222222222"), Name = "Nameplate", PlantId = plant2Id, WorkCenterTypeId = wctNameplateId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("fa222222-2222-2222-2222-222222222222"), Name = "Hydro", PlantId = plant2Id, WorkCenterTypeId = wctHydroId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("fb222222-2222-2222-2222-222222222222"), Name = "Rolls Material", PlantId = plant2Id, WorkCenterTypeId = wctMaterialQueueId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = null, MaterialQueueForWCId = Guid.Parse("f1222222-2222-2222-2222-222222222222") },
            new WorkCenter { Id = Guid.Parse("fc222222-2222-2222-2222-222222222222"), Name = "Fitup Queue", PlantId = plant2Id, WorkCenterTypeId = wctMaterialQueueId, ProductionLineId = line1Plt2, NumberOfWelders = 0, DataEntryType = null, MaterialQueueForWCId = Guid.Parse("f5222222-2222-2222-2222-222222222222") },
            new WorkCenter { Id = Guid.Parse("f1333333-3333-3333-3333-333333333333"), Name = "Rolls 1", PlantId = plant3Id, WorkCenterTypeId = wctRollsId, ProductionLineId = line1Plt3, NumberOfWelders = 1, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("f2333333-3333-3333-3333-333333333333"), Name = "Long Seam 1", PlantId = plant3Id, WorkCenterTypeId = wctLongSeamId, ProductionLineId = line1Plt3, NumberOfWelders = 1, DataEntryType = "standard" },
            new WorkCenter { Id = Guid.Parse("f3333333-3333-3333-3333-333333333333"), Name = "Long Seam Inspection", PlantId = plant3Id, WorkCenterTypeId = wctInspectionId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = "inspection" },
            new WorkCenter { Id = Guid.Parse("f4333333-3333-3333-3333-333333333333"), Name = "RT X-ray Queue", PlantId = plant3Id, WorkCenterTypeId = wctXrayId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("f5333333-3333-3333-3333-333333333333"), Name = "Fitup", PlantId = plant3Id, WorkCenterTypeId = wctFitupId, ProductionLineId = line1Plt3, NumberOfWelders = 1, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("f6333333-3333-3333-3333-333333333333"), Name = "Round Seam", PlantId = plant3Id, WorkCenterTypeId = wctRoundSeamId, ProductionLineId = line1Plt3, NumberOfWelders = 1, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("f7333333-3333-3333-3333-333333333333"), Name = "Round Seam Inspection", PlantId = plant3Id, WorkCenterTypeId = wctInspectionId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = "inspection" },
            new WorkCenter { Id = Guid.Parse("f8333333-3333-3333-3333-333333333333"), Name = "Spot X-ray", PlantId = plant3Id, WorkCenterTypeId = wctSpotXrayId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("f9333333-3333-3333-3333-333333333333"), Name = "Nameplate", PlantId = plant3Id, WorkCenterTypeId = wctNameplateId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("fa333333-3333-3333-3333-333333333333"), Name = "Hydro", PlantId = plant3Id, WorkCenterTypeId = wctHydroId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = null },
            new WorkCenter { Id = Guid.Parse("fb333333-3333-3333-3333-333333333333"), Name = "Rolls Material", PlantId = plant3Id, WorkCenterTypeId = wctMaterialQueueId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = null, MaterialQueueForWCId = Guid.Parse("f1333333-3333-3333-3333-333333333333") },
            new WorkCenter { Id = Guid.Parse("fc333333-3333-3333-3333-333333333333"), Name = "Fitup Queue", PlantId = plant3Id, WorkCenterTypeId = wctMaterialQueueId, ProductionLineId = line1Plt3, NumberOfWelders = 0, DataEntryType = null, MaterialQueueForWCId = Guid.Parse("f5333333-3333-3333-3333-333333333333") }
        );

        // Product types (6) with SystemTypeName
        var ptPlateId = Guid.Parse("a1111111-1111-1111-1111-111111111111");
        var ptHeadId = Guid.Parse("a2222222-2222-2222-2222-222222222222");
        var ptShellId = Guid.Parse("a3333333-3333-3333-3333-333333333333");
        var ptAssembledTankId = Guid.Parse("a4444444-4444-4444-4444-444444444444");
        var ptSellableTankId = Guid.Parse("a5555555-5555-5555-5555-555555555555");
        var ptPlasmaId = Guid.Parse("a6666666-6666-6666-6666-666666666666");

        modelBuilder.Entity<ProductType>().HasData(
            new ProductType { Id = ptPlateId, Name = "Plate", SystemTypeName = "plate" },
            new ProductType { Id = ptHeadId, Name = "Head", SystemTypeName = "head" },
            new ProductType { Id = ptShellId, Name = "Shell", SystemTypeName = "shell" },
            new ProductType { Id = ptAssembledTankId, Name = "Assembled Tank", SystemTypeName = "assembled" },
            new ProductType { Id = ptSellableTankId, Name = "Sellable Tank", SystemTypeName = "sellable" },
            new ProductType { Id = ptPlasmaId, Name = "Plasma", SystemTypeName = "plasma" }
        );

        // Products: Plates, Heads, Shells, Sellable Tanks
        // SiteNumbers is a comma-separated list of plant codes that use the product
        var allSites = "000,600,700";
        var clevelandFremont = "000,600";
        modelBuilder.Entity<Product>().HasData(
            new Product { Id = Guid.Parse("b1011111-1111-1111-1111-111111111111"), ProductNumber = "PL .140NOM X 54.00 X 74.625", TankSize = 120, TankType = "Plate", SiteNumbers = allSites, ProductTypeId = ptPlateId },
            new Product { Id = Guid.Parse("b1021111-1111-1111-1111-111111111111"), ProductNumber = "PL .175NOM X 63.25 X 93.375", TankSize = 250, TankType = "Plate", SiteNumbers = allSites, ProductTypeId = ptPlateId },
            new Product { Id = Guid.Parse("b1031111-1111-1111-1111-111111111111"), ProductNumber = "PL .175NOM X 87.00 X 93.375", TankSize = 320, TankType = "Plate", SiteNumbers = allSites, ProductTypeId = ptPlateId },
            new Product { Id = Guid.Parse("b1041111-1111-1111-1111-111111111111"), ProductNumber = "PL .218NOM X 83.00 X 116.6875", TankSize = 500, TankType = "Plate", SiteNumbers = clevelandFremont, ProductTypeId = ptPlateId },
            new Product { Id = Guid.Parse("b1051111-1111-1111-1111-111111111111"), ProductNumber = "PL .239NOM X 75.75 X 127.5675", TankSize = 1000, TankType = "Plate", SiteNumbers = clevelandFremont, ProductTypeId = ptPlateId },
            new Product { Id = Guid.Parse("b2011111-1111-1111-1111-111111111111"), ProductNumber = "ELLIP 24\" OD", TankSize = 120, TankType = "Head", SiteNumbers = allSites, ProductTypeId = ptHeadId },
            new Product { Id = Guid.Parse("b2021111-1111-1111-1111-111111111111"), ProductNumber = "HEMI 30\" OD", TankSize = 250, TankType = "Head", SiteNumbers = allSites, ProductTypeId = ptHeadId },
            new Product { Id = Guid.Parse("b2031111-1111-1111-1111-111111111111"), ProductNumber = "HEMI 30\" OD", TankSize = 320, TankType = "Head", SiteNumbers = allSites, ProductTypeId = ptHeadId },
            new Product { Id = Guid.Parse("b2041111-1111-1111-1111-111111111111"), ProductNumber = "HEMI 37\" ID", TankSize = 500, TankType = "Head", SiteNumbers = clevelandFremont, ProductTypeId = ptHeadId },
            new Product { Id = Guid.Parse("b2051111-1111-1111-1111-111111111111"), ProductNumber = "HEMI 40.5\" ID", TankSize = 1000, TankType = "Head", SiteNumbers = clevelandFremont, ProductTypeId = ptHeadId },
            new Product { Id = Guid.Parse("b3011111-1111-1111-1111-111111111111"), ProductNumber = "120 gal", TankSize = 120, TankType = "Shell", SiteNumbers = allSites, ProductTypeId = ptShellId },
            new Product { Id = Guid.Parse("b3021111-1111-1111-1111-111111111111"), ProductNumber = "250 gal", TankSize = 250, TankType = "Shell", SiteNumbers = allSites, ProductTypeId = ptShellId },
            new Product { Id = Guid.Parse("b3031111-1111-1111-1111-111111111111"), ProductNumber = "320 gal", TankSize = 320, TankType = "Shell", SiteNumbers = allSites, ProductTypeId = ptShellId },
            new Product { Id = Guid.Parse("b3041111-1111-1111-1111-111111111111"), ProductNumber = "500 gal", TankSize = 500, TankType = "Shell", SiteNumbers = clevelandFremont, ProductTypeId = ptShellId },
            new Product { Id = Guid.Parse("b3051111-1111-1111-1111-111111111111"), ProductNumber = "1000 gal", TankSize = 1000, TankType = "Shell", SiteNumbers = clevelandFremont, ProductTypeId = ptShellId },
            new Product { Id = Guid.Parse("b5011111-1111-1111-1111-111111111111"), ProductNumber = "120 AG", TankSize = 120, TankType = "Sellable", SiteNumbers = allSites, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5021111-1111-1111-1111-111111111111"), ProductNumber = "120 UG", TankSize = 120, TankType = "Sellable", SiteNumbers = allSites, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5031111-1111-1111-1111-111111111111"), ProductNumber = "250 AG", TankSize = 250, TankType = "Sellable", SiteNumbers = allSites, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5041111-1111-1111-1111-111111111111"), ProductNumber = "250 UG", TankSize = 250, TankType = "Sellable", SiteNumbers = allSites, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5051111-1111-1111-1111-111111111111"), ProductNumber = "320 AG", TankSize = 320, TankType = "Sellable", SiteNumbers = allSites, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5061111-1111-1111-1111-111111111111"), ProductNumber = "320 UG", TankSize = 320, TankType = "Sellable", SiteNumbers = allSites, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5071111-1111-1111-1111-111111111111"), ProductNumber = "500 AG", TankSize = 500, TankType = "Sellable", SiteNumbers = clevelandFremont, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5081111-1111-1111-1111-111111111111"), ProductNumber = "500 UG", TankSize = 500, TankType = "Sellable", SiteNumbers = clevelandFremont, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b5091111-1111-1111-1111-111111111111"), ProductNumber = "1000 AG", TankSize = 1000, TankType = "Sellable", SiteNumbers = clevelandFremont, ProductTypeId = ptSellableTankId },
            new Product { Id = Guid.Parse("b50a1111-1111-1111-1111-111111111111"), ProductNumber = "1000 UG", TankSize = 1000, TankType = "Sellable", SiteNumbers = clevelandFremont, ProductTypeId = ptSellableTankId }
        );

        // Users: EMP001 = Jeff Thompson (TestUserId), Administrator, Cleveland
        var testUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        modelBuilder.Entity<User>().HasData(
            new User { Id = testUserId, EmployeeNumber = "EMP001", FirstName = "Jeff", LastName = "Thompson", DisplayName = "Jeff Thompson", RoleTier = 1.0m, RoleName = "Administrator", DefaultSiteId = plant1Id, IsCertifiedWelder = false, RequirePinForLogin = false, PinHash = null },
            new User { Id = Guid.Parse("88888888-8888-8888-8888-888888888801"), EmployeeNumber = "EMP002", FirstName = "Sarah", LastName = "Miller", DisplayName = "Sarah Miller", RoleTier = 6.0m, RoleName = "Operator", DefaultSiteId = plant1Id, IsCertifiedWelder = false, RequirePinForLogin = false, PinHash = null },
            new User { Id = Guid.Parse("88888888-8888-8888-8888-888888888802"), EmployeeNumber = "EMP003", FirstName = "Mike", LastName = "Rodriguez", DisplayName = "Mike Rodriguez", RoleTier = 6.0m, RoleName = "Operator", DefaultSiteId = plant1Id, IsCertifiedWelder = true, RequirePinForLogin = false, PinHash = null },
            new User { Id = Guid.Parse("88888888-8888-8888-8888-888888888803"), EmployeeNumber = "EMP004", FirstName = "Tom", LastName = "Wilson", DisplayName = "Tom Wilson", RoleTier = 6.0m, RoleName = "Operator", DefaultSiteId = plant2Id, IsCertifiedWelder = true, RequirePinForLogin = false, PinHash = null },
            new User { Id = Guid.Parse("88888888-8888-8888-8888-888888888804"), EmployeeNumber = "EMP005", FirstName = "Lisa", LastName = "Chen", DisplayName = "Lisa Chen", RoleTier = 4.0m, RoleName = "Supervisor", DefaultSiteId = plant1Id, IsCertifiedWelder = false, RequirePinForLogin = false, PinHash = null }
        );

        // Vendors: mills, processors, head
        modelBuilder.Entity<Vendor>().HasData(
            new Vendor { Id = Guid.Parse("51000001-0000-0000-0000-000000000001"), Name = "Nucor Steel", VendorType = "mill", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.Parse("51000002-0000-0000-0000-000000000002"), Name = "Steel Dynamics", VendorType = "mill", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.Parse("51000003-0000-0000-0000-000000000003"), Name = "NLMK", VendorType = "mill", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.Parse("52000001-0000-0000-0000-000000000001"), Name = "Metals USA", VendorType = "processor", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.Parse("52000002-0000-0000-0000-000000000002"), Name = "Steel Technologies", VendorType = "processor", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.Parse("53000001-0000-0000-0000-000000000001"), Name = "CMF Inc", VendorType = "head", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.Parse("53000002-0000-0000-0000-000000000002"), Name = "Compco Industries", VendorType = "head", SiteCode = null, IsActive = true }
        );

        // Plant gears: 5 per plant (Gear 1–5, Level 1–5)
        modelBuilder.Entity<PlantGear>().HasData(
            new PlantGear { Id = Guid.Parse("61111111-1111-1111-1111-111111111111"), Name = "Gear 1", Level = 1, PlantId = plant1Id },
            new PlantGear { Id = Guid.Parse("61111111-1111-1111-1111-111111111112"), Name = "Gear 2", Level = 2, PlantId = plant1Id },
            new PlantGear { Id = Guid.Parse("61111111-1111-1111-1111-111111111113"), Name = "Gear 3", Level = 3, PlantId = plant1Id },
            new PlantGear { Id = Guid.Parse("61111111-1111-1111-1111-111111111114"), Name = "Gear 4", Level = 4, PlantId = plant1Id },
            new PlantGear { Id = Guid.Parse("61111111-1111-1111-1111-111111111115"), Name = "Gear 5", Level = 5, PlantId = plant1Id },
            new PlantGear { Id = Guid.Parse("62222222-2222-2222-2222-222222222221"), Name = "Gear 1", Level = 1, PlantId = plant2Id },
            new PlantGear { Id = Guid.Parse("62222222-2222-2222-2222-222222222222"), Name = "Gear 2", Level = 2, PlantId = plant2Id },
            new PlantGear { Id = Guid.Parse("62222222-2222-2222-2222-222222222223"), Name = "Gear 3", Level = 3, PlantId = plant2Id },
            new PlantGear { Id = Guid.Parse("62222222-2222-2222-2222-222222222224"), Name = "Gear 4", Level = 4, PlantId = plant2Id },
            new PlantGear { Id = Guid.Parse("62222222-2222-2222-2222-222222222225"), Name = "Gear 5", Level = 5, PlantId = plant2Id },
            new PlantGear { Id = Guid.Parse("63333333-3333-3333-3333-333333333331"), Name = "Gear 1", Level = 1, PlantId = plant3Id },
            new PlantGear { Id = Guid.Parse("63333333-3333-3333-3333-333333333332"), Name = "Gear 2", Level = 2, PlantId = plant3Id },
            new PlantGear { Id = Guid.Parse("63333333-3333-3333-3333-333333333333"), Name = "Gear 3", Level = 3, PlantId = plant3Id },
            new PlantGear { Id = Guid.Parse("63333333-3333-3333-3333-333333333334"), Name = "Gear 4", Level = 4, PlantId = plant3Id },
            new PlantGear { Id = Guid.Parse("63333333-3333-3333-3333-333333333335"), Name = "Gear 5", Level = 5, PlantId = plant3Id }
        );

        // Annotation types (spec 15.1): Name, Abbreviation, RequiresResolution, OperatorCanCreate, DisplayColor
        modelBuilder.Entity<AnnotationType>().HasData(
            new AnnotationType { Id = Guid.Parse("a1000001-0000-0000-0000-000000000001"), Name = "Note", Abbreviation = "N", RequiresResolution = false, OperatorCanCreate = true, DisplayColor = "#cc00ff" },
            new AnnotationType { Id = Guid.Parse("a1000002-0000-0000-0000-000000000002"), Name = "AI Review", Abbreviation = "AI", RequiresResolution = false, OperatorCanCreate = false, DisplayColor = "#33cc33" },
            new AnnotationType { Id = Guid.Parse("a1000003-0000-0000-0000-000000000003"), Name = "Defect", Abbreviation = "D", RequiresResolution = true, OperatorCanCreate = true, DisplayColor = "#ff0000" },
            new AnnotationType { Id = Guid.Parse("a1000004-0000-0000-0000-000000000004"), Name = "Internal Review", Abbreviation = "IR", RequiresResolution = false, OperatorCanCreate = false, DisplayColor = "#0099ff" },
            new AnnotationType { Id = Guid.Parse("a1000005-0000-0000-0000-000000000005"), Name = "Correction Needed", Abbreviation = "C", RequiresResolution = true, OperatorCanCreate = true, DisplayColor = "#ffff00" }
        );

        // Characteristics: Long Seam, RS1, RS2, RS3, RS4
        var charLongSeamId = Guid.Parse("c1000001-0000-0000-0000-000000000001");
        var charRs1Id = Guid.Parse("c2000001-0000-0000-0000-000000000001");
        var charRs2Id = Guid.Parse("c2000002-0000-0000-0000-000000000002");
        var charRs3Id = Guid.Parse("c2000003-0000-0000-0000-000000000003");
        var charRs4Id = Guid.Parse("c2000004-0000-0000-0000-000000000004");

        modelBuilder.Entity<Characteristic>().HasData(
            new Characteristic { Id = charLongSeamId, Name = "Long Seam", SpecHigh = null, SpecLow = null, SpecTarget = null, ProductTypeId = null },
            new Characteristic { Id = charRs1Id, Name = "RS1", SpecHigh = null, SpecLow = null, SpecTarget = null, ProductTypeId = null },
            new Characteristic { Id = charRs2Id, Name = "RS2", SpecHigh = null, SpecLow = null, SpecTarget = null, ProductTypeId = null },
            new Characteristic { Id = charRs3Id, Name = "RS3", SpecHigh = null, SpecLow = null, SpecTarget = null, ProductTypeId = null },
            new Characteristic { Id = charRs4Id, Name = "RS4", SpecHigh = null, SpecLow = null, SpecTarget = null, ProductTypeId = null }
        );

        // Characteristic–WorkCenter: Long Seam -> Long Seam Inspection; RS1–RS4 -> Round Seam Inspection (all plants)
        modelBuilder.Entity<CharacteristicWorkCenter>().HasData(
            new CharacteristicWorkCenter { Id = Guid.Parse("c1000001-0000-0000-0000-000000000001"), CharacteristicId = charLongSeamId, WorkCenterId = wcLongSeamInspPlt1 },
            new CharacteristicWorkCenter { Id = Guid.Parse("c1000001-0000-0000-0000-000000000002"), CharacteristicId = charLongSeamId, WorkCenterId = Guid.Parse("f3222222-2222-2222-2222-222222222222") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c1000001-0000-0000-0000-000000000003"), CharacteristicId = charLongSeamId, WorkCenterId = Guid.Parse("f3333333-3333-3333-3333-333333333333") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000001-0000-0000-0000-000000000001"), CharacteristicId = charRs1Id, WorkCenterId = wcRoundSeamInspPlt1 },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000001-0000-0000-0000-000000000002"), CharacteristicId = charRs1Id, WorkCenterId = Guid.Parse("f7222222-2222-2222-2222-222222222222") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000001-0000-0000-0000-000000000003"), CharacteristicId = charRs1Id, WorkCenterId = Guid.Parse("f7333333-3333-3333-3333-333333333333") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000002-0000-0000-0000-000000000001"), CharacteristicId = charRs2Id, WorkCenterId = wcRoundSeamInspPlt1 },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000002-0000-0000-0000-000000000002"), CharacteristicId = charRs2Id, WorkCenterId = Guid.Parse("f7222222-2222-2222-2222-222222222222") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000002-0000-0000-0000-000000000003"), CharacteristicId = charRs2Id, WorkCenterId = Guid.Parse("f7333333-3333-3333-3333-333333333333") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000003-0000-0000-0000-000000000001"), CharacteristicId = charRs3Id, WorkCenterId = wcRoundSeamInspPlt1 },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000003-0000-0000-0000-000000000002"), CharacteristicId = charRs3Id, WorkCenterId = Guid.Parse("f7222222-2222-2222-2222-222222222222") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000003-0000-0000-0000-000000000003"), CharacteristicId = charRs3Id, WorkCenterId = Guid.Parse("f7333333-3333-3333-3333-333333333333") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000004-0000-0000-0000-000000000001"), CharacteristicId = charRs4Id, WorkCenterId = wcRoundSeamInspPlt1 },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000004-0000-0000-0000-000000000002"), CharacteristicId = charRs4Id, WorkCenterId = Guid.Parse("f7222222-2222-2222-2222-222222222222") },
            new CharacteristicWorkCenter { Id = Guid.Parse("c2000004-0000-0000-0000-000000000003"), CharacteristicId = charRs4Id, WorkCenterId = Guid.Parse("f7333333-3333-3333-3333-333333333333") }
        );

        // Defect codes: 101–105, 999
        var dc101Id = Guid.Parse("d1010001-0000-0000-0000-000000000001");
        var dc102Id = Guid.Parse("d1010002-0000-0000-0000-000000000002");
        var dc103Id = Guid.Parse("d1010003-0000-0000-0000-000000000003");
        var dc104Id = Guid.Parse("d1010004-0000-0000-0000-000000000004");
        var dc105Id = Guid.Parse("d1010005-0000-0000-0000-000000000005");
        var dc999Id = Guid.Parse("d9990001-0000-0000-0000-000000000001");

        modelBuilder.Entity<DefectCode>().HasData(
            new DefectCode { Id = dc101Id, Code = "101", Name = "Burn Through", Severity = null, SystemType = null },
            new DefectCode { Id = dc102Id, Code = "102", Name = "Undercut", Severity = null, SystemType = null },
            new DefectCode { Id = dc103Id, Code = "103", Name = "Cold Lap", Severity = null, SystemType = null },
            new DefectCode { Id = dc104Id, Code = "104", Name = "Porosity", Severity = null, SystemType = null },
            new DefectCode { Id = dc105Id, Code = "105", Name = "Crack", Severity = null, SystemType = null },
            new DefectCode { Id = dc999Id, Code = "999", Name = "Shell Plate Thickness", Severity = null, SystemType = null }
        );

        // Defect–WorkCenter: all defect codes linked to Long Seam Inspection, Round Seam Inspection, Hydro (Cleveland)
        modelBuilder.Entity<DefectWorkCenter>().HasData(
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000001"), DefectCodeId = dc101Id, WorkCenterId = wcLongSeamInspPlt1, EarliestDetectionWorkCenterId = wcLongSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000002"), DefectCodeId = dc102Id, WorkCenterId = wcLongSeamInspPlt1, EarliestDetectionWorkCenterId = wcLongSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000003"), DefectCodeId = dc103Id, WorkCenterId = wcLongSeamInspPlt1, EarliestDetectionWorkCenterId = wcLongSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000004"), DefectCodeId = dc104Id, WorkCenterId = wcLongSeamInspPlt1, EarliestDetectionWorkCenterId = wcLongSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000005"), DefectCodeId = dc105Id, WorkCenterId = wcLongSeamInspPlt1, EarliestDetectionWorkCenterId = wcLongSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000001-0000-0000-0000-000000000006"), DefectCodeId = dc999Id, WorkCenterId = wcLongSeamInspPlt1, EarliestDetectionWorkCenterId = wcLongSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000001"), DefectCodeId = dc101Id, WorkCenterId = wcRoundSeamInspPlt1, EarliestDetectionWorkCenterId = wcRoundSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000002"), DefectCodeId = dc102Id, WorkCenterId = wcRoundSeamInspPlt1, EarliestDetectionWorkCenterId = wcRoundSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000003"), DefectCodeId = dc103Id, WorkCenterId = wcRoundSeamInspPlt1, EarliestDetectionWorkCenterId = wcRoundSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000004"), DefectCodeId = dc104Id, WorkCenterId = wcRoundSeamInspPlt1, EarliestDetectionWorkCenterId = wcRoundSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000005"), DefectCodeId = dc105Id, WorkCenterId = wcRoundSeamInspPlt1, EarliestDetectionWorkCenterId = wcRoundSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000002-0000-0000-0000-000000000006"), DefectCodeId = dc999Id, WorkCenterId = wcRoundSeamInspPlt1, EarliestDetectionWorkCenterId = wcRoundSeamInspPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000001"), DefectCodeId = dc101Id, WorkCenterId = wcHydroPlt1, EarliestDetectionWorkCenterId = wcHydroPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000002"), DefectCodeId = dc102Id, WorkCenterId = wcHydroPlt1, EarliestDetectionWorkCenterId = wcHydroPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000003"), DefectCodeId = dc103Id, WorkCenterId = wcHydroPlt1, EarliestDetectionWorkCenterId = wcHydroPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000004"), DefectCodeId = dc104Id, WorkCenterId = wcHydroPlt1, EarliestDetectionWorkCenterId = wcHydroPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000005"), DefectCodeId = dc105Id, WorkCenterId = wcHydroPlt1, EarliestDetectionWorkCenterId = wcHydroPlt1 },
            new DefectWorkCenter { Id = Guid.Parse("d0000003-0000-0000-0000-000000000006"), DefectCodeId = dc999Id, WorkCenterId = wcHydroPlt1, EarliestDetectionWorkCenterId = wcHydroPlt1 }
        );

        // Defect locations: T-Joint (1), Tack (2), Fill Valve (3), Leg (4), Start (5), End (6) -> Long Seam characteristic
        modelBuilder.Entity<DefectLocation>().HasData(
            new DefectLocation { Id = Guid.Parse("d1000001-0000-0000-0000-000000000001"), Code = "1", Name = "T-Joint", DefaultLocationDetail = null, CharacteristicId = charLongSeamId },
            new DefectLocation { Id = Guid.Parse("d1000001-0000-0000-0000-000000000002"), Code = "2", Name = "Tack", DefaultLocationDetail = null, CharacteristicId = charLongSeamId },
            new DefectLocation { Id = Guid.Parse("d1000001-0000-0000-0000-000000000003"), Code = "3", Name = "Fill Valve", DefaultLocationDetail = null, CharacteristicId = charLongSeamId },
            new DefectLocation { Id = Guid.Parse("d1000001-0000-0000-0000-000000000004"), Code = "4", Name = "Leg", DefaultLocationDetail = null, CharacteristicId = charLongSeamId },
            new DefectLocation { Id = Guid.Parse("d1000001-0000-0000-0000-000000000005"), Code = "5", Name = "Start", DefaultLocationDetail = null, CharacteristicId = charLongSeamId },
            new DefectLocation { Id = Guid.Parse("d1000001-0000-0000-0000-000000000006"), Code = "6", Name = "End", DefaultLocationDetail = null, CharacteristicId = charLongSeamId }
        );

        // Barcode cards (5 for Cleveland / global)
        modelBuilder.Entity<BarcodeCard>().HasData(
            new BarcodeCard { Id = Guid.Parse("bc000001-0000-0000-0000-000000000001"), CardValue = "01", Color = "Red", Description = null },
            new BarcodeCard { Id = Guid.Parse("bc000002-0000-0000-0000-000000000002"), CardValue = "02", Color = "Yellow", Description = null },
            new BarcodeCard { Id = Guid.Parse("bc000003-0000-0000-0000-000000000003"), CardValue = "03", Color = "Blue", Description = null },
            new BarcodeCard { Id = Guid.Parse("bc000004-0000-0000-0000-000000000004"), CardValue = "04", Color = "Green", Description = null },
            new BarcodeCard { Id = Guid.Parse("bc000005-0000-0000-0000-000000000005"), CardValue = "05", Color = "Orange", Description = null }
        );

        // Assets: one per key work center for Cleveland Line 1. TestAssetId = Rolls 1 asset for tests.
        var testAssetId = Guid.Parse("a0000001-0000-0000-0000-000000000001");
        modelBuilder.Entity<Asset>().HasData(
            new Asset { Id = testAssetId, Name = "Rolls 1 Asset", WorkCenterId = wcRolls1Plt1 },
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000002"), Name = "Long Seam 1 Asset", WorkCenterId = wcLongSeam1Plt1 },
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000003"), Name = "Fitup Asset", WorkCenterId = wcFitupPlt1 },
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000004"), Name = "Round Seam Asset", WorkCenterId = wcRoundSeamPlt1 },
            new Asset { Id = Guid.Parse("a0000001-0000-0000-0000-000000000005"), Name = "Hydro Asset", WorkCenterId = wcHydroPlt1 }
        );
    }
}
