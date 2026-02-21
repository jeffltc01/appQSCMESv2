using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class VendorsControllerTests
{
    private static readonly string Plt1 = TestHelpers.PlantPlt1Id.ToString();
    private static readonly string Plt2 = TestHelpers.PlantPlt2Id.ToString();
    private static readonly string Plt3 = TestHelpers.PlantPlt3Id.ToString();

    private VendorsController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        var productService = new ProductService(db);
        return new VendorsController(productService, db);
    }

    private static void SeedPlantSpecificVendors(Data.MesDbContext db)
    {
        db.Vendors.AddRange(
            new Vendor { Id = Guid.NewGuid(), Name = "Global Mill", VendorType = "mill", PlantIds = null, IsActive = true },
            new Vendor { Id = Guid.NewGuid(), Name = "Cleveland Mill", VendorType = "mill", PlantIds = Plt1, IsActive = true },
            new Vendor { Id = Guid.NewGuid(), Name = "Multi Mill", VendorType = "mill", PlantIds = $"{Plt1},{Plt2}", IsActive = true },
            new Vendor { Id = Guid.NewGuid(), Name = "WJ Only Mill", VendorType = "mill", PlantIds = Plt3, IsActive = true },
            new Vendor { Id = Guid.NewGuid(), Name = "Inactive Cleveland Mill", VendorType = "mill", PlantIds = Plt1, IsActive = false }
        );
        db.SaveChanges();
    }

    [Fact]
    public async Task GetVendors_WithPlantId_ReturnsOnlyExplicitlyMatchingVendors()
    {
        var controller = CreateController(out var db);
        SeedPlantSpecificVendors(db);

        var result = await controller.GetVendors("mill", Plt1, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<VendorDto>>(ok.Value).ToList();
        Assert.DoesNotContain(list, v => v.Name == "Global Mill");
        Assert.Contains(list, v => v.Name == "Cleveland Mill");
        Assert.Contains(list, v => v.Name == "Multi Mill");
        Assert.DoesNotContain(list, v => v.Name == "WJ Only Mill");
        Assert.DoesNotContain(list, v => v.Name == "Inactive Cleveland Mill");
    }

    [Fact]
    public async Task GetVendors_WithoutPlantId_ReturnsAllActiveVendors()
    {
        var controller = CreateController(out var db);
        SeedPlantSpecificVendors(db);

        var result = await controller.GetVendors("mill", null, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<VendorDto>>(ok.Value).ToList();
        Assert.Contains(list, v => v.Name == "Global Mill");
        Assert.Contains(list, v => v.Name == "Cleveland Mill");
        Assert.Contains(list, v => v.Name == "Multi Mill");
        Assert.Contains(list, v => v.Name == "WJ Only Mill");
        Assert.DoesNotContain(list, v => v.Name == "Inactive Cleveland Mill");
    }

    [Fact]
    public async Task GetVendors_PlantId3_ExcludesClevelandSpecific()
    {
        var controller = CreateController(out var db);
        SeedPlantSpecificVendors(db);

        var result = await controller.GetVendors("mill", Plt3, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<VendorDto>>(ok.Value).ToList();
        Assert.DoesNotContain(list, v => v.Name == "Global Mill");
        Assert.Contains(list, v => v.Name == "WJ Only Mill");
        Assert.DoesNotContain(list, v => v.Name == "Cleveland Mill");
        Assert.DoesNotContain(list, v => v.Name == "Multi Mill");
    }
}
