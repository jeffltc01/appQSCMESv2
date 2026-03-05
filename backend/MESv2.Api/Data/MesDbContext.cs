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
    public DbSet<DefectLocationCharacteristic> DefectLocationCharacteristics => Set<DefectLocationCharacteristic>();
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
    public DbSet<FrontendTelemetryEvent> FrontendTelemetryEvents => Set<FrontendTelemetryEvent>();
    public DbSet<DemoShellFlow> DemoShellFlows => Set<DemoShellFlow>();
    public DbSet<ChecklistTemplate> ChecklistTemplates => Set<ChecklistTemplate>();
    public DbSet<ChecklistTemplateItem> ChecklistTemplateItems => Set<ChecklistTemplateItem>();
    public DbSet<ChecklistEntry> ChecklistEntries => Set<ChecklistEntry>();
    public DbSet<ChecklistEntryItemResponse> ChecklistEntryItemResponses => Set<ChecklistEntryItemResponse>();
    public DbSet<ScoreType> ScoreTypes => Set<ScoreType>();
    public DbSet<ScoreTypeValue> ScoreTypeValues => Set<ScoreTypeValue>();
    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();
    public DbSet<WorkflowStepDefinition> WorkflowStepDefinitions => Set<WorkflowStepDefinition>();
    public DbSet<WorkflowInstance> WorkflowInstances => Set<WorkflowInstance>();
    public DbSet<WorkflowStepInstance> WorkflowStepInstances => Set<WorkflowStepInstance>();
    public DbSet<WorkflowStepApproval> WorkflowStepApprovals => Set<WorkflowStepApproval>();
    public DbSet<WorkItem> WorkItems => Set<WorkItem>();
    public DbSet<NotificationRule> NotificationRules => Set<NotificationRule>();
    public DbSet<WorkflowEvent> WorkflowEvents => Set<WorkflowEvent>();
    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();
    public DbSet<HoldTag> HoldTags => Set<HoldTag>();
    public DbSet<Ncr> Ncrs => Set<Ncr>();
    public DbSet<NcrType> NcrTypes => Set<NcrType>();
    public DbSet<NcrAttachment> NcrAttachments => Set<NcrAttachment>();
    public DbSet<SequenceCounter> SequenceCounters => Set<SequenceCounter>();
    public DbSet<Schedule> Schedules => Set<Schedule>();
    public DbSet<ScheduleLine> ScheduleLines => Set<ScheduleLine>();
    public DbSet<ScheduleChangeLog> ScheduleChangeLogs => Set<ScheduleChangeLog>();
    public DbSet<ErpSalesOrderDemandRaw> ErpSalesOrderDemandRows => Set<ErpSalesOrderDemandRaw>();
    public DbSet<ErpDemandSnapshot> ErpDemandSnapshots => Set<ErpDemandSnapshot>();
    public DbSet<ErpSkuPlanningGroupMapping> ErpSkuPlanningGroupMappings => Set<ErpSkuPlanningGroupMapping>();
    public DbSet<UnmappedDemandException> UnmappedDemandExceptions => Set<UnmappedDemandException>();
    public DbSet<ScheduleExecutionEvent> ScheduleExecutionEvents => Set<ScheduleExecutionEvent>();
    public DbSet<SupermarketPositionSnapshot> SupermarketPositionSnapshots => Set<SupermarketPositionSnapshot>();
    public DbSet<HeijunkaWorkCenterBreakdownConfig> HeijunkaWorkCenterBreakdownConfigs => Set<HeijunkaWorkCenterBreakdownConfig>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MesDbContext).Assembly);

        modelBuilder.Entity<WorkflowDefinition>()
            .HasIndex(x => new { x.WorkflowType, x.Version })
            .IsUnique();
        modelBuilder.Entity<WorkflowStepDefinition>()
            .HasIndex(x => new { x.WorkflowDefinitionId, x.StepCode })
            .IsUnique();
        modelBuilder.Entity<WorkflowInstance>()
            .Property(x => x.RowVersion)
            .IsRowVersion();
        modelBuilder.Entity<WorkflowStepApproval>()
            .HasIndex(x => new { x.WorkflowStepInstanceId, x.AssignmentType, x.AssignedUserId, x.AssignedRoleTier })
            .HasFilter(null)
            .IsUnique();
        modelBuilder.Entity<WorkflowStepApproval>()
            .HasCheckConstraint("CK_WorkflowStepApprovals_Assignee", "([AssignedUserId] IS NOT NULL AND [AssignedRoleTier] IS NULL) OR ([AssignedUserId] IS NULL AND [AssignedRoleTier] IS NOT NULL)");
        modelBuilder.Entity<WorkItem>()
            .HasCheckConstraint("CK_WorkItems_Assignee", "([AssignedUserId] IS NOT NULL AND [AssignedRoleTier] IS NULL) OR ([AssignedUserId] IS NULL AND [AssignedRoleTier] IS NOT NULL)");
        modelBuilder.Entity<IdempotencyRecord>()
            .HasIndex(x => x.Key)
            .IsUnique();
        modelBuilder.Entity<HoldTag>()
            .HasIndex(x => x.HoldTagNumber)
            .IsUnique();
        modelBuilder.Entity<Ncr>()
            .HasIndex(x => x.NcrNumber)
            .IsUnique();
        modelBuilder.Entity<NcrType>()
            .HasIndex(x => x.Code)
            .IsUnique();
        modelBuilder.Entity<SequenceCounter>()
            .HasIndex(x => x.Name)
            .IsUnique();
        modelBuilder.Entity<Schedule>()
            .HasIndex(x => new { x.SiteCode, x.ProductionLineId, x.WeekStartDateLocal, x.RevisionNumber })
            .IsUnique();
        modelBuilder.Entity<Schedule>()
            .HasIndex(x => new { x.SiteCode, x.ProductionLineId, x.WeekStartDateLocal })
            .HasFilter("[Status] = 'Published'")
            .IsUnique();
        modelBuilder.Entity<Schedule>()
            .Property(x => x.RowVersion)
            .IsRowVersion();
        modelBuilder.Entity<ScheduleLine>()
            .HasIndex(x => new { x.ScheduleId, x.PlannedDateLocal, x.SequenceIndex });
        modelBuilder.Entity<ScheduleChangeLog>()
            .HasIndex(x => new { x.ScheduleId, x.ChangedAtUtc });
        modelBuilder.Entity<ErpSalesOrderDemandRaw>()
            .HasIndex(x => new { x.ErpSalesOrderId, x.ErpSalesOrderLineId, x.SourceExtractedAtUtc })
            .IsUnique();
        modelBuilder.Entity<ErpDemandSnapshot>()
            .HasIndex(x => new { x.ErpSalesOrderId, x.ErpSalesOrderLineId, x.CapturedAtUtc })
            .IsUnique();
        modelBuilder.Entity<ErpSkuPlanningGroupMapping>()
            .HasIndex(x => new { x.ErpSkuCode, x.SiteCode, x.EffectiveFromUtc })
            .IsUnique();
        modelBuilder.Entity<UnmappedDemandException>()
            .HasIndex(x => new { x.SiteCode, x.ExceptionStatus, x.DispatchDateLocal });
        modelBuilder.Entity<ScheduleExecutionEvent>()
            .HasIndex(x => x.IdempotencyKey)
            .IsUnique();
        modelBuilder.Entity<ScheduleExecutionEvent>()
            .HasIndex(x => new { x.SiteCode, x.ProductionLineId, x.ExecutionDateLocal });
        modelBuilder.Entity<SupermarketPositionSnapshot>()
            .HasIndex(x => new { x.SiteCode, x.ProductionLineId, x.ProductId, x.CapturedAtUtc });
        modelBuilder.Entity<HeijunkaWorkCenterBreakdownConfig>()
            .HasIndex(x => new { x.SiteCode, x.ProductionLineId, x.WorkCenterId })
            .IsUnique();
    }
}
