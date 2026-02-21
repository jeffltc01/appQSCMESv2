using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class VendorsControllerTests
{
    private static readonly Guid Plt1 = TestHelpers.PlantPlt1Id;
    private static readonly Guid Plt2 = TestHelpers.PlantPlt2Id;
    private static readonly Guid Plt3 = TestHelpers.PlantPlt3Id;

    private VendorsController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        var productService = new ProductService(db);
        return new VendorsController(productService, db);
    }

    private static void SeedPlantSpecificVendors(Data.MesDbContext db)
    {
        var global = new Vendor { Id = Guid.NewGuid(), Name = "Global Mill", VendorType = "mill", IsActive = true };
        var cleveland = new Vendor { Id = Guid.NewGuid(), Name = "Cleveland Mill", VendorType = "mill", IsActive = true };
        var multi = new Vendor { Id = Guid.NewGuid(), Name = "Multi Mill", VendorType = "mill", IsActive = true };
        var wj = new Vendor { Id = Guid.NewGuid(), Name = "WJ Only Mill", VendorType = "mill", IsActive = true };
        var inactive = new Vendor { Id = Guid.NewGuid(), Name = "Inactive Cleveland Mill", VendorType = "mill", IsActive = false };
        db.Vendors.AddRange(global, cleveland, multi, wj, inactive);

        db.VendorPlants.AddRange(
            new VendorPlant { Id = Guid.NewGuid(), VendorId = cleveland.Id, PlantId = Plt1 },
            new VendorPlant { Id = Guid.NewGuid(), VendorId = multi.Id, PlantId = Plt1 },
            new VendorPlant { Id = Guid.NewGuid(), VendorId = multi.Id, PlantId = Plt2 },
            new VendorPlant { Id = Guid.NewGuid(), VendorId = wj.Id, PlantId = Plt3 },
            new VendorPlant { Id = Guid.NewGuid(), VendorId = inactive.Id, PlantId = Plt1 }
        );
        db.SaveChanges();
    }

    [Fact]
    public async Task GetVendors_WithPlantId_ReturnsOnlyExplicitlyMatchingVendors()
    {
        var controller = CreateController(out var db);
        SeedPlantSpecificVendors(db);

        var result = await controller.GetVendors("mill", Plt1.ToString(), CancellationToken.None);

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

        var result = await controller.GetVendors("mill", Plt3.ToString(), CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<VendorDto>>(ok.Value).ToList();
        Assert.DoesNotContain(list, v => v.Name == "Global Mill");
        Assert.Contains(list, v => v.Name == "WJ Only Mill");
        Assert.DoesNotContain(list, v => v.Name == "Cleveland Mill");
        Assert.DoesNotContain(list, v => v.Name == "Multi Mill");
    }
}
