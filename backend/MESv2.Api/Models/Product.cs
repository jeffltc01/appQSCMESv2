namespace MESv2.Api.Models;

public class Product
{
    public Guid Id { get; set; }
    public string ProductNumber { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public string TankType { get; set; } = string.Empty;
    public string? SageItemNumber { get; set; }
    public string? NameplateNumber { get; set; }
    public Guid ProductTypeId { get; set; }

    public ProductType ProductType { get; set; } = null!;
    public ICollection<SerialNumber> SerialNumbers { get; set; } = new List<SerialNumber>();
}
