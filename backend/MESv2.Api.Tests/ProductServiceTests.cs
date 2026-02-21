using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class ProductServiceTests
{
    private static readonly string Plt1 = TestHelpers.PlantPlt1Id.ToString();
    private static readonly string Plt2 = TestHelpers.PlantPlt2Id.ToString();
    private static readonly string Plt3 = TestHelpers.PlantPlt3Id.ToString();

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
    public async Task GetVendors_FiltersByPlantId_ExcludesUnassignedAndNonMatchingPlants()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Global Mill", VendorType = "mill", PlantIds = null, IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Global Empty Mill", VendorType = "mill", PlantIds = "", IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Cleveland Mill", VendorType = "mill", PlantIds = Plt1, IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Multi Mill", VendorType = "mill", PlantIds = $"{Plt1},{Plt2}", IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "WJ Only Mill", VendorType = "mill", PlantIds = Plt3, IsActive = true });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);

        var resultPlt1 = await sut.GetVendorsAsync("mill", Plt1);
        Assert.DoesNotContain(resultPlt1, v => v.Name == "Global Mill");
        Assert.DoesNotContain(resultPlt1, v => v.Name == "Global Empty Mill");
        Assert.Contains(resultPlt1, v => v.Name == "Cleveland Mill");
        Assert.Contains(resultPlt1, v => v.Name == "Multi Mill");
        Assert.DoesNotContain(resultPlt1, v => v.Name == "WJ Only Mill");

        var resultPlt2 = await sut.GetVendorsAsync("mill", Plt2);
        Assert.DoesNotContain(resultPlt2, v => v.Name == "Global Mill");
        Assert.DoesNotContain(resultPlt2, v => v.Name == "Cleveland Mill");
        Assert.Contains(resultPlt2, v => v.Name == "Multi Mill");
        Assert.DoesNotContain(resultPlt2, v => v.Name == "WJ Only Mill");

        var resultPlt3 = await sut.GetVendorsAsync("mill", Plt3);
        Assert.DoesNotContain(resultPlt3, v => v.Name == "Global Mill");
        Assert.Contains(resultPlt3, v => v.Name == "WJ Only Mill");
        Assert.DoesNotContain(resultPlt3, v => v.Name == "Cleveland Mill");
        Assert.DoesNotContain(resultPlt3, v => v.Name == "Multi Mill");
    }

    [Fact]
    public async Task GetVendors_NoPlantId_ReturnsAllActiveVendorsIncludingGlobal()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Global Mill", VendorType = "mill", PlantIds = null, IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Cleveland Mill", VendorType = "mill", PlantIds = Plt1, IsActive = true });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetVendorsAsync("mill", null);

        Assert.Contains(result, v => v.Name == "Global Mill");
        Assert.Contains(result, v => v.Name == "Cleveland Mill");
    }

    [Fact]
    public async Task GetVendors_EmptyPlantId_TreatedSameAsNull_ReturnsAll()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Global Mill", VendorType = "mill", PlantIds = null, IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Cleveland Mill", VendorType = "mill", PlantIds = Plt1, IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "WJ Mill", VendorType = "mill", PlantIds = Plt3, IsActive = true });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetVendorsAsync("mill", "");

        Assert.Contains(result, v => v.Name == "Global Mill");
        Assert.Contains(result, v => v.Name == "Cleveland Mill");
        Assert.Contains(result, v => v.Name == "WJ Mill");
    }

    [Fact]
    public async Task GetVendors_WhitespaceInCsvPlantIds_StillMatches()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Spaced Mill", VendorType = "mill", PlantIds = $"{Plt1}, {Plt2}", IsActive = true });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);

        var resultPlt1 = await sut.GetVendorsAsync("mill", Plt1);
        Assert.Contains(resultPlt1, v => v.Name == "Spaced Mill");

        var resultPlt2 = await sut.GetVendorsAsync("mill", Plt2);
        Assert.Contains(resultPlt2, v => v.Name == "Spaced Mill");

        var resultPlt3 = await sut.GetVendorsAsync("mill", Plt3);
        Assert.DoesNotContain(resultPlt3, v => v.Name == "Spaced Mill");
    }

    [Fact]
    public async Task GetVendors_InactiveVendor_ExcludedEvenWhenPlantMatches()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Active Cleveland", VendorType = "mill", PlantIds = Plt1, IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Inactive Cleveland", VendorType = "mill", PlantIds = Plt1, IsActive = false });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetVendorsAsync("mill", Plt1);

        Assert.Contains(result, v => v.Name == "Active Cleveland");
        Assert.DoesNotContain(result, v => v.Name == "Inactive Cleveland");
    }

    [Fact]
    public async Task GetVendors_NullOrEmptyPlantIds_ExcludedWhenPlantIdProvided()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Null Vendor", VendorType = "test-iso", PlantIds = null, IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Empty Vendor", VendorType = "test-iso", PlantIds = "", IsActive = true });
        db.Vendors.Add(new Vendor { Id = Guid.NewGuid(), Name = "Assigned Vendor", VendorType = "test-iso", PlantIds = Plt1, IsActive = true });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetVendorsAsync("test-iso", Plt1);

        Assert.Single(result);
        Assert.Equal("Assigned Vendor", result[0].Name);
    }
}
