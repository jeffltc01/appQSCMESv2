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
    public DbSet<WorkCenterProductionLine> WorkCenterProductionLines => Set<WorkCenterProductionLine>();
    public DbSet<WorkCenterWelder> WorkCenterWelders => Set<WorkCenterWelder>();

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
            .HasOne(w => w.WorkCenterType)
            .WithMany(t => t.WorkCenters)
            .HasForeignKey(w => w.WorkCenterTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Asset>()
            .HasOne(a => a.WorkCenter)
            .WithMany(w => w.Assets)
            .HasForeignKey(a => a.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<Asset>()
            .HasOne(a => a.ProductionLine)
            .WithMany()
            .HasForeignKey(a => a.ProductionLineId)
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

        modelBuilder.Entity<WorkCenterProductionLine>()
            .HasOne(wcpl => wcpl.WorkCenter)
            .WithMany(w => w.WorkCenterProductionLines)
            .HasForeignKey(wcpl => wcpl.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkCenterProductionLine>()
            .HasOne(wcpl => wcpl.ProductionLine)
            .WithMany()
            .HasForeignKey(wcpl => wcpl.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<WorkCenterWelder>()
            .HasOne(w => w.WorkCenter)
            .WithMany()
            .HasForeignKey(w => w.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
        modelBuilder.Entity<WorkCenterWelder>()
            .HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
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
        modelBuilder.Entity<WorkCenterProductionLine>()
            .HasIndex(wcpl => new { wcpl.WorkCenterId, wcpl.ProductionLineId })
            .IsUnique();
        modelBuilder.Entity<WorkCenterWelder>()
            .HasIndex(w => new { w.WorkCenterId, w.UserId })
            .IsUnique();

    }
}
