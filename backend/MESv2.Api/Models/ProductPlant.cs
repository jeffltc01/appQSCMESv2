namespace MESv2.Api.Models;

public class ProductPlant
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid PlantId { get; set; }

    public Product Product { get; set; } = null!;
    public Plant Plant { get; set; } = null!;
}
