namespace MESv2.Api.Models;

public class Characteristic
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal? SpecHigh { get; set; }
    public decimal? SpecLow { get; set; }
    public decimal? SpecTarget { get; set; }
    public int? MinTankSize { get; set; }
    public Guid? ProductTypeId { get; set; }

    public ProductType? ProductType { get; set; }
    public ICollection<WelderLog> WelderLogs { get; set; } = new List<WelderLog>();
    public ICollection<DefectLog> DefectLogs { get; set; } = new List<DefectLog>();
    public ICollection<DefectLocation> DefectLocations { get; set; } = new List<DefectLocation>();
}
