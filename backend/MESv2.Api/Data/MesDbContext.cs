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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ----- Relationships -----
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

        // ----- Seed data -----
        var plant1Id = Guid.Parse("11111111-1111-1111-1111-111111111111");
        var plant2Id = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var plant3Id = Guid.Parse("33333333-3333-3333-3333-333333333333");

        modelBuilder.Entity<Plant>().HasData(
            new Plant { Id = plant1Id, Code = "PLT1", Name = "Plant 1" },
            new Plant { Id = plant2Id, Code = "PLT2", Name = "Plant 2" },
            new Plant { Id = plant3Id, Code = "PLT3", Name = "Plant 3" }
        );

        var rollsId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
        var longSeamId = Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
        var inspectionId = Guid.Parse("cccccccc-cccc-cccc-cccc-cccccccccccc");
        var fitupId = Guid.Parse("dddddddd-dddd-dddd-dddd-dddddddddddd");

        modelBuilder.Entity<WorkCenterType>().HasData(
            new WorkCenterType { Id = rollsId, Name = "Rolls" },
            new WorkCenterType { Id = longSeamId, Name = "Long Seam" },
            new WorkCenterType { Id = inspectionId, Name = "Inspection" },
            new WorkCenterType { Id = fitupId, Name = "Fitup" }
        );

        var line1Plt1 = Guid.Parse("e1111111-1111-1111-1111-111111111111");
        var line2Plt1 = Guid.Parse("e2111111-1111-1111-1111-111111111111");
        var line1Plt2 = Guid.Parse("e1222222-2222-2222-2222-222222222222");

        modelBuilder.Entity<ProductionLine>().HasData(
            new ProductionLine { Id = line1Plt1, Name = "Line 1", PlantId = plant1Id },
            new ProductionLine { Id = line2Plt1, Name = "Line 2", PlantId = plant1Id },
            new ProductionLine { Id = line1Plt2, Name = "Line 1", PlantId = plant2Id }
        );

        var wc1Plt1 = Guid.Parse("f1111111-1111-1111-1111-111111111111");
        var wc2Plt1 = Guid.Parse("f2111111-1111-1111-1111-111111111111");
        var wc3Plt1 = Guid.Parse("f3111111-1111-1111-1111-111111111111");
        var wc1Plt2 = Guid.Parse("f1222222-2222-2222-2222-222222222222");
        var wc2Plt2 = Guid.Parse("f2222222-2222-2222-2222-222222222222");
        var wc1Plt3 = Guid.Parse("f1333333-3333-3333-3333-333333333333");

        modelBuilder.Entity<WorkCenter>().HasData(
            new WorkCenter { Id = wc1Plt1, Name = "Rolls 1", PlantId = plant1Id, WorkCenterTypeId = rollsId, ProductionLineId = line1Plt1, RequiresWelder = false, DataEntryType = null },
            new WorkCenter { Id = wc2Plt1, Name = "Long Seam 1", PlantId = plant1Id, WorkCenterTypeId = longSeamId, ProductionLineId = line1Plt1, RequiresWelder = true, DataEntryType = "standard" },
            new WorkCenter { Id = wc3Plt1, Name = "Inspection 1", PlantId = plant1Id, WorkCenterTypeId = inspectionId, ProductionLineId = line1Plt1, RequiresWelder = false, DataEntryType = "inspection" },
            new WorkCenter { Id = wc1Plt2, Name = "Rolls 1", PlantId = plant2Id, WorkCenterTypeId = rollsId, ProductionLineId = line1Plt2, RequiresWelder = false, DataEntryType = null },
            new WorkCenter { Id = wc2Plt2, Name = "Fitup 1", PlantId = plant2Id, WorkCenterTypeId = fitupId, ProductionLineId = line1Plt2, RequiresWelder = false, DataEntryType = null },
            new WorkCenter { Id = wc1Plt3, Name = "Rolls 1", PlantId = plant3Id, WorkCenterTypeId = rollsId, ProductionLineId = null, RequiresWelder = false, DataEntryType = null }
        );

        var testUserId = Guid.Parse("99999999-9999-9999-9999-999999999999");
        modelBuilder.Entity<User>().HasData(
            new User
            {
                Id = testUserId,
                EmployeeNumber = "EMP001",
                FirstName = "Test",
                LastName = "User",
                DisplayName = "Test User",
                RoleTier = 1.0m,
                RoleName = "Operator",
                DefaultSiteId = plant1Id,
                IsCertifiedWelder = false,
                RequirePinForLogin = false,
                PinHash = null
            }
        );
    }
}
