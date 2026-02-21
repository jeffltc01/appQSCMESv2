using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class VendorsControllerTests
{
    private VendorsController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        var productService = new ProductService(db);
        return new VendorsController(productService, db);
    }

    private static void SeedSiteSpecificVendors(Data.MesDbContext db)
    {
        db.Vendors.AddRange(
            new Vendor { Id = Guid.NewGuid(), Name = "Global Mill", VendorType = "mill", SiteCode = null, IsActive = true },
            new Vendor { Id = Guid.NewGuid(), Name = "Cleveland Mill", VendorType = "mill", SiteCode = "000", IsActive = true },
            new Vendor { Id = Guid.NewGuid(), Name = "Multi Mill", VendorType = "mill", SiteCode = "000,600", IsActive = true },
            new Vendor { Id = Guid.NewGuid(), Name = "WJ Only Mill", VendorType = "mill", SiteCode = "700", IsActive = true },
            new Vendor { Id = Guid.NewGuid(), Name = "Inactive Cleveland Mill", VendorType = "mill", SiteCode = "000", IsActive = false }
        );
        db.SaveChanges();
    }

    [Fact]
    public async Task GetVendors_WithSiteCode_ReturnsOnlyExplicitlyMatchingVendors()
    {
        var controller = CreateController(out var db);
        SeedSiteSpecificVendors(db);

        var result = await controller.GetVendors("mill", "000", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<VendorDto>>(ok.Value).ToList();
        Assert.DoesNotContain(list, v => v.Name == "Global Mill");
        Assert.Contains(list, v => v.Name == "Cleveland Mill");
        Assert.Contains(list, v => v.Name == "Multi Mill");
        Assert.DoesNotContain(list, v => v.Name == "WJ Only Mill");
        Assert.DoesNotContain(list, v => v.Name == "Inactive Cleveland Mill");
    }

    [Fact]
    public async Task GetVendors_WithoutSiteCode_ReturnsAllActiveVendors()
    {
        var controller = CreateController(out var db);
        SeedSiteSpecificVendors(db);

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
    public async Task GetVendors_SiteCode700_ExcludesClevelandSpecific()
    {
        var controller = CreateController(out var db);
        SeedSiteSpecificVendors(db);

        var result = await controller.GetVendors("mill", "700", CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<VendorDto>>(ok.Value).ToList();
        Assert.DoesNotContain(list, v => v.Name == "Global Mill");
        Assert.Contains(list, v => v.Name == "WJ Only Mill");
        Assert.DoesNotContain(list, v => v.Name == "Cleveland Mill");
        Assert.DoesNotContain(list, v => v.Name == "Multi Mill");
    }
}
