namespace MESv2.Api.Models;

public class ProductType
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Characteristic> Characteristics { get; set; } = new List<Characteristic>();
}
