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
    public DbSet<QueueTransaction> QueueTransactions => Set<QueueTransaction>();
    public DbSet<BarcodeCard> BarcodeCards => Set<BarcodeCard>();
    public DbSet<Annotation> Annotations => Set<Annotation>();
    public DbSet<AnnotationType> AnnotationTypes => Set<AnnotationType>();
    public DbSet<PlantGear> PlantGears => Set<PlantGear>();
    public DbSet<ChangeLog> ChangeLogs => Set<ChangeLog>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<XrayQueueItem> XrayQueueItems => Set<XrayQueueItem>();
    public DbSet<RoundSeamSetup> RoundSeamSetups => Set<RoundSeamSetup>();
    public DbSet<ActiveSession> ActiveSessions => Set<ActiveSession>();
    public DbSet<SpotXrayIncrement> SpotXrayIncrements => Set<SpotXrayIncrement>();
    public DbSet<SpotXrayIncrementTank> SpotXrayIncrementTanks => Set<SpotXrayIncrementTank>();
    public DbSet<XrayShotCounter> XrayShotCounters => Set<XrayShotCounter>();
    public DbSet<SiteSchedule> SiteSchedules => Set<SiteSchedule>();
    public DbSet<WorkCenterProductionLine> WorkCenterProductionLines => Set<WorkCenterProductionLine>();
    public DbSet<VendorPlant> VendorPlants => Set<VendorPlant>();
    public DbSet<ProductPlant> ProductPlants => Set<ProductPlant>();
    public DbSet<PlantPrinter> PlantPrinters => Set<PlantPrinter>();
    public DbSet<PrintLog> PrintLogs => Set<PrintLog>();
    public DbSet<IssueRequest> IssueRequests => Set<IssueRequest>();
    public DbSet<DowntimeReasonCategory> DowntimeReasonCategories => Set<DowntimeReasonCategory>();
    public DbSet<DowntimeReason> DowntimeReasons => Set<DowntimeReason>();
    public DbSet<WorkCenterProductionLineDowntimeReason> WorkCenterProductionLineDowntimeReasons => Set<WorkCenterProductionLineDowntimeReason>();
    public DbSet<DowntimeEvent> DowntimeEvents => Set<DowntimeEvent>();
    public DbSet<ShiftSchedule> ShiftSchedules => Set<ShiftSchedule>();
    public DbSet<WorkCenterCapacityTarget> WorkCenterCapacityTargets => Set<WorkCenterCapacityTarget>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MesDbContext).Assembly);
    }
}
