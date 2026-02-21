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

    [Fact]
    public async Task GetVendors_FiltersBySiteCode_IncludesGlobalAndMatchingSites()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Global Mill", VendorType = "mill", SiteCode = null, IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Global Empty Mill", VendorType = "mill", SiteCode = "", IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Cleveland Mill", VendorType = "mill", SiteCode = "000", IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Multi Mill", VendorType = "mill", SiteCode = "000,600", IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "WJ Only Mill", VendorType = "mill", SiteCode = "700", IsActive = true });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);

        var result000 = await sut.GetVendorsAsync("mill", "000");
        Assert.Contains(result000, v => v.Name == "Global Mill");
        Assert.Contains(result000, v => v.Name == "Global Empty Mill");
        Assert.Contains(result000, v => v.Name == "Cleveland Mill");
        Assert.Contains(result000, v => v.Name == "Multi Mill");
        Assert.DoesNotContain(result000, v => v.Name == "WJ Only Mill");

        var result600 = await sut.GetVendorsAsync("mill", "600");
        Assert.Contains(result600, v => v.Name == "Global Mill");
        Assert.DoesNotContain(result600, v => v.Name == "Cleveland Mill");
        Assert.Contains(result600, v => v.Name == "Multi Mill");
        Assert.DoesNotContain(result600, v => v.Name == "WJ Only Mill");

        var result700 = await sut.GetVendorsAsync("mill", "700");
        Assert.Contains(result700, v => v.Name == "Global Mill");
        Assert.Contains(result700, v => v.Name == "WJ Only Mill");
        Assert.DoesNotContain(result700, v => v.Name == "Cleveland Mill");
        Assert.DoesNotContain(result700, v => v.Name == "Multi Mill");
    }

    [Fact]
    public async Task GetVendors_NoSiteCode_ReturnsAllActiveVendorsIncludingGlobal()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Global Mill", VendorType = "mill", SiteCode = null, IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Cleveland Mill", VendorType = "mill", SiteCode = "000", IsActive = true });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetVendorsAsync("mill", null);

        Assert.Contains(result, v => v.Name == "Global Mill");
        Assert.Contains(result, v => v.Name == "Cleveland Mill");
    }
}
