namespace MESv2.Api.Models;

public class WorkCenter
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Guid PlantId { get; set; }
    public Guid WorkCenterTypeId { get; set; }
    public Guid? ProductionLineId { get; set; }
    public bool RequiresWelder { get; set; }
    public string? DataEntryType { get; set; }
    public Guid? MaterialQueueForWCId { get; set; }

    public Plant Plant { get; set; } = null!;
    public WorkCenterType WorkCenterType { get; set; } = null!;
    public ProductionLine? ProductionLine { get; set; }
    public WorkCenter? MaterialQueueForWC { get; set; }
    public ICollection<Asset> Assets { get; set; } = new List<Asset>();
    public ICollection<MaterialQueueItem> MaterialQueueItems { get; set; } = new List<MaterialQueueItem>();
    public ICollection<ProductionRecord> ProductionRecords { get; set; } = new List<ProductionRecord>();
    public ICollection<InspectionRecord> InspectionRecords { get; set; } = new List<InspectionRecord>();
    public ICollection<ControlPlan> ControlPlans { get; set; } = new List<ControlPlan>();
    public ICollection<Assembly> Assemblies { get; set; } = new List<Assembly>();
    public ICollection<CharacteristicWorkCenter> CharacteristicWorkCenters { get; set; } = new List<CharacteristicWorkCenter>();
    public ICollection<DefectWorkCenter> DefectWorkCenters { get; set; } = new List<DefectWorkCenter>();
}
