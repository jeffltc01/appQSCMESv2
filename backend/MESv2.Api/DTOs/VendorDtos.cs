namespace MESv2.Api.DTOs;

public class VendorDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string VendorType { get; set; } = string.Empty;
}

public class ProductListDto
{
    public Guid Id { get; set; }
    public string ProductNumber { get; set; } = string.Empty;
    public int TankSize { get; set; }
    public string TankType { get; set; } = string.Empty;
    public string? NameplateNumber { get; set; }
}
