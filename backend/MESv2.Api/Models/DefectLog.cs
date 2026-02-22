namespace MESv2.Api.Models;

public class DefectLog
{
    public Guid Id { get; set; }
    public Guid? ProductionRecordId { get; set; }
    public Guid SerialNumberId { get; set; }
    public Guid DefectCodeId { get; set; }
    public Guid CharacteristicId { get; set; }
    public Guid LocationId { get; set; }
    public decimal? LocDetails1 { get; set; }
    public decimal? LocDetails2 { get; set; }
    public string? LocDetailsCode { get; set; }
    public bool IsRepaired { get; set; }
    public Guid? RepairedByUserId { get; set; }
    public DateTime? RepairedDateTime { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime Timestamp { get; set; }

    public ProductionRecord? ProductionRecord { get; set; }
    public SerialNumber SerialNumber { get; set; } = null!;
    public DefectCode DefectCode { get; set; } = null!;
    public Characteristic Characteristic { get; set; } = null!;
    public DefectLocation Location { get; set; } = null!;
    public User? RepairedByUser { get; set; }
    public User? CreatedByUser { get; set; }
}
