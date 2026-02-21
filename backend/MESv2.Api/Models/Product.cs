namespace MESv2.Api.Models;

public class Product
{
    public Guid Id { get; set; }
    public string ProductNumber { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public string TankType { get; set; } = string.Empty;
    public string? SageItemNumber { get; set; }
    public string? NameplateNumber { get; set; }
    public string? SiteNumbers { get; set; }
    public Guid ProductTypeId { get; set; }
    public bool IsActive { get; set; } = true;

    public ProductType ProductType { get; set; } = null!;
    public ICollection<SerialNumber> SerialNumbers { get; set; } = new List<SerialNumber>();
    public ICollection<ProductPlant> ProductPlants { get; set; } = new List<ProductPlant>();
}
