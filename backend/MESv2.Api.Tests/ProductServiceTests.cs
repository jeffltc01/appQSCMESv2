using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class ProductServiceTests
{
    private static readonly Guid Plt1 = TestHelpers.PlantPlt1Id;
    private static readonly Guid Plt2 = TestHelpers.PlantPlt2Id;
    private static readonly Guid Plt3 = TestHelpers.PlantPlt3Id;

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
    public async Task GetProducts_FiltersByPlant_ViaJunctionTable()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var ptId = Guid.NewGuid();
        db.ProductTypes.Add(new ProductType { Id = ptId, Name = "Sellable", SystemTypeName = "sellable-jt" });

        var pClv = new Product { Id = Guid.NewGuid(), ProductNumber = "P-CLV", TankSize = 120, TankType = "AG", ProductTypeId = ptId };
        var pMulti = new Product { Id = Guid.NewGuid(), ProductNumber = "P-MULTI", TankSize = 250, TankType = "UG", ProductTypeId = ptId };
        var pWj = new Product { Id = Guid.NewGuid(), ProductNumber = "P-WJ", TankSize = 500, TankType = "AG", ProductTypeId = ptId };
        var pNone = new Product { Id = Guid.NewGuid(), ProductNumber = "P-NONE", TankSize = 320, TankType = "AG", ProductTypeId = ptId };
        db.Products.AddRange(pClv, pMulti, pWj, pNone);

        db.ProductPlants.AddRange(
            new ProductPlant { Id = Guid.NewGuid(), ProductId = pClv.Id, PlantId = Plt1 },
            new ProductPlant { Id = Guid.NewGuid(), ProductId = pMulti.Id, PlantId = Plt1 },
            new ProductPlant { Id = Guid.NewGuid(), ProductId = pMulti.Id, PlantId = Plt2 },
            new ProductPlant { Id = Guid.NewGuid(), ProductId = pWj.Id, PlantId = Plt3 }
        );
        await db.SaveChangesAsync();

        var sut = new ProductService(db);

        var plt1Result = await sut.GetProductsAsync("sellable-jt", Plt1.ToString());
        Assert.Contains(plt1Result, p => p.ProductNumber == "P-CLV");
        Assert.Contains(plt1Result, p => p.ProductNumber == "P-MULTI");
        Assert.DoesNotContain(plt1Result, p => p.ProductNumber == "P-WJ");
        Assert.DoesNotContain(plt1Result, p => p.ProductNumber == "P-NONE");
    }

    [Fact]
    public async Task GetProducts_NoPlantId_ReturnsAll()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var ptId = Guid.NewGuid();
        db.ProductTypes.Add(new ProductType { Id = ptId, Name = "Sellable", SystemTypeName = "sellable-all" });
        var pA = new Product { Id = Guid.NewGuid(), ProductNumber = "PA", TankSize = 120, TankType = "AG", ProductTypeId = ptId };
        var pB = new Product { Id = Guid.NewGuid(), ProductNumber = "PB", TankSize = 250, TankType = "UG", ProductTypeId = ptId };
        db.Products.AddRange(pA, pB);
        db.ProductPlants.Add(new ProductPlant { Id = Guid.NewGuid(), ProductId = pA.Id, PlantId = Plt1 });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetProductsAsync("sellable-all", null);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetVendors_ReturnsActiveVendors()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var activeMill = new Vendor { Id = Guid.NewGuid(), Name = "Mill Co", VendorType = "mill", IsActive = true };
        var inactiveMill = new Vendor { Id = Guid.NewGuid(), Name = "Old Mill", VendorType = "mill", IsActive = false };
        db.Vendors.AddRange(activeMill, inactiveMill);
        db.VendorPlants.Add(new VendorPlant { Id = Guid.NewGuid(), VendorId = activeMill.Id, PlantId = Plt1 });
        db.VendorPlants.Add(new VendorPlant { Id = Guid.NewGuid(), VendorId = inactiveMill.Id, PlantId = Plt1 });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetVendorsAsync("mill", null);

        Assert.Contains(result, v => v.Name == "Mill Co");
        Assert.DoesNotContain(result, v => v.Name == "Old Mill");
    }

    [Fact]
    public async Task GetVendors_FiltersByPlant_ViaJunctionTable()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var globalMill = new Vendor { Id = Guid.NewGuid(), Name = "Global Mill", VendorType = "mill-jt", IsActive = true };
        var clevelandMill = new Vendor { Id = Guid.NewGuid(), Name = "Cleveland Mill", VendorType = "mill-jt", IsActive = true };
        var multiMill = new Vendor { Id = Guid.NewGuid(), Name = "Multi Mill", VendorType = "mill-jt", IsActive = true };
        var wjMill = new Vendor { Id = Guid.NewGuid(), Name = "WJ Only Mill", VendorType = "mill-jt", IsActive = true };
        db.Vendors.AddRange(globalMill, clevelandMill, multiMill, wjMill);

        db.VendorPlants.AddRange(
            new VendorPlant { Id = Guid.NewGuid(), VendorId = clevelandMill.Id, PlantId = Plt1 },
            new VendorPlant { Id = Guid.NewGuid(), VendorId = multiMill.Id, PlantId = Plt1 },
            new VendorPlant { Id = Guid.NewGuid(), VendorId = multiMill.Id, PlantId = Plt2 },
            new VendorPlant { Id = Guid.NewGuid(), VendorId = wjMill.Id, PlantId = Plt3 }
        );
        await db.SaveChangesAsync();

        var sut = new ProductService(db);

        var resultPlt1 = await sut.GetVendorsAsync("mill-jt", Plt1.ToString());
        Assert.DoesNotContain(resultPlt1, v => v.Name == "Global Mill");
        Assert.Contains(resultPlt1, v => v.Name == "Cleveland Mill");
        Assert.Contains(resultPlt1, v => v.Name == "Multi Mill");
        Assert.DoesNotContain(resultPlt1, v => v.Name == "WJ Only Mill");

        var resultPlt2 = await sut.GetVendorsAsync("mill-jt", Plt2.ToString());
        Assert.DoesNotContain(resultPlt2, v => v.Name == "Cleveland Mill");
        Assert.Contains(resultPlt2, v => v.Name == "Multi Mill");
        Assert.DoesNotContain(resultPlt2, v => v.Name == "WJ Only Mill");

        var resultPlt3 = await sut.GetVendorsAsync("mill-jt", Plt3.ToString());
        Assert.Contains(resultPlt3, v => v.Name == "WJ Only Mill");
        Assert.DoesNotContain(resultPlt3, v => v.Name == "Cleveland Mill");
    }

    [Fact]
    public async Task GetVendors_NoPlantId_ReturnsAllActive()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var v1 = new Vendor { Id = Guid.NewGuid(), Name = "Global Mill", VendorType = "mill-all", IsActive = true };
        var v2 = new Vendor { Id = Guid.NewGuid(), Name = "Cleveland Mill", VendorType = "mill-all", IsActive = true };
        db.Vendors.AddRange(v1, v2);
        db.VendorPlants.Add(new VendorPlant { Id = Guid.NewGuid(), VendorId = v2.Id, PlantId = Plt1 });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetVendorsAsync("mill-all", null);

        Assert.Contains(result, v => v.Name == "Global Mill");
        Assert.Contains(result, v => v.Name == "Cleveland Mill");
    }

    [Fact]
    public async Task GetVendors_EmptyPlantId_TreatedSameAsNull_ReturnsAll()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var v1 = new Vendor { Id = Guid.NewGuid(), Name = "Global Mill", VendorType = "mill-empty", IsActive = true };
        var v2 = new Vendor { Id = Guid.NewGuid(), Name = "Cleveland Mill", VendorType = "mill-empty", IsActive = true };
        db.Vendors.AddRange(v1, v2);
        db.VendorPlants.Add(new VendorPlant { Id = Guid.NewGuid(), VendorId = v2.Id, PlantId = Plt1 });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetVendorsAsync("mill-empty", "");

        Assert.Contains(result, v => v.Name == "Global Mill");
        Assert.Contains(result, v => v.Name == "Cleveland Mill");
    }

    [Fact]
    public async Task GetVendors_InactiveVendor_ExcludedEvenWhenPlantMatches()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var active = new Vendor { Id = Guid.NewGuid(), Name = "Active Cleveland", VendorType = "mill-act", IsActive = true };
        var inactive = new Vendor { Id = Guid.NewGuid(), Name = "Inactive Cleveland", VendorType = "mill-act", IsActive = false };
        db.Vendors.AddRange(active, inactive);
        db.VendorPlants.AddRange(
            new VendorPlant { Id = Guid.NewGuid(), VendorId = active.Id, PlantId = Plt1 },
            new VendorPlant { Id = Guid.NewGuid(), VendorId = inactive.Id, PlantId = Plt1 }
        );
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetVendorsAsync("mill-act", Plt1.ToString());

        Assert.Contains(result, v => v.Name == "Active Cleveland");
        Assert.DoesNotContain(result, v => v.Name == "Inactive Cleveland");
    }

    [Fact]
    public async Task GetVendors_NoJunctionRecords_ExcludedWhenPlantIdProvided()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var unassigned = new Vendor { Id = Guid.NewGuid(), Name = "Unassigned Vendor", VendorType = "test-unasgn", IsActive = true };
        var assigned = new Vendor { Id = Guid.NewGuid(), Name = "Assigned Vendor", VendorType = "test-unasgn", IsActive = true };
        db.Vendors.AddRange(unassigned, assigned);
        db.VendorPlants.Add(new VendorPlant { Id = Guid.NewGuid(), VendorId = assigned.Id, PlantId = Plt1 });
        await db.SaveChangesAsync();

        var sut = new ProductService(db);
        var result = await sut.GetVendorsAsync("test-unasgn", Plt1.ToString());

        Assert.Single(result);
        Assert.Equal("Assigned Vendor", result[0].Name);
    }
}
