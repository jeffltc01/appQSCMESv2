using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using Moq;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class AdminVendorsControllerTests
{
    private VendorsController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        var mockService = new Mock<IProductService>();
        return new VendorsController(mockService.Object, db);
    }

    [Fact]
    public async Task GetAllVendors_ReturnsSeedVendors()
    {
        var controller = CreateController(out _);
        var result = await controller.GetAllVendors(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminVendorDto>>(ok.Value).ToList();
        Assert.True(list.Count >= 1);
    }

    [Fact]
    public async Task CreateVendor_AddsVendor()
    {
        var controller = CreateController(out var db);
        var dto = new CreateVendorDto { Name = "Test Mill", VendorType = "mill" };

        var result = await controller.CreateVendor(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<AdminVendorDto>(ok.Value);
        Assert.Equal("Test Mill", created.Name);
        Assert.True(created.IsActive);
        Assert.True(db.Vendors.Any(v => v.Name == "Test Mill"));
    }

    [Fact]
    public async Task UpdateVendor_ModifiesFields()
    {
        var controller = CreateController(out var db);
        var vendor = new Vendor { Id = Guid.NewGuid(), Name = "Old", VendorType = "processor", IsActive = true };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        var dto = new UpdateVendorDto { Name = "New Name", VendorType = "mill", IsActive = false };
        var result = await controller.UpdateVendor(vendor.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminVendorDto>(ok.Value);
        Assert.Equal("New Name", updated.Name);
        Assert.False(updated.IsActive);
    }

    [Fact]
    public async Task UpdateVendor_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateVendorDto { Name = "X", VendorType = "X", IsActive = true };
        var result = await controller.UpdateVendor(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteVendor_RemovesVendor()
    {
        var controller = CreateController(out var db);
        var vendor = new Vendor { Id = Guid.NewGuid(), Name = "ToDelete", VendorType = "head", IsActive = true };
        db.Vendors.Add(vendor);
        await db.SaveChangesAsync();

        var result = await controller.DeleteVendor(vendor.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.False(db.Vendors.Any(v => v.Id == vendor.Id));
    }
}
