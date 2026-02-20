using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class ProductServiceTests
{
    [Fact]
    public async Task GetProducts_ReturnsAllProducts()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var ptId = Guid.NewGuid();
        db.ProductTypes.Add(new ProductType { Id = ptId, Name = "Test Type", SystemTypeName = "test-unique" });
        db.Products.Add(new Product { Id = Guid.NewGuid(), ProductNumber = "PL-001", TankSize = 120, TankType = "AG", ProductTypeId = ptId });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetProductsAsync("test-unique", null);

        Assert.Single(result);
        Assert.Equal("PL-001", result[0].ProductNumber);
    }

    [Fact]
    public async Task GetVendors_ReturnsActiveVendors()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Mill Co", VendorType = "mill", IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Old Mill", VendorType = "mill", IsActive = false });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetVendorsAsync("mill", null);

        Assert.Contains(result, v => v.Name == "Mill Co");
        Assert.DoesNotContain(result, v => v.Name == "Old Mill");
    }
}
