using Microsoft.AspNetCore.Mvc;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using Moq;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class AdminProductsControllerTests
{
    private ProductsController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        var mockService = new Mock<IProductService>();
        return new ProductsController(mockService.Object, db);
    }

    [Fact]
    public async Task GetAllProducts_ReturnsProductsWithTypeName()
    {
        var controller = CreateController(out var db);
        var ptId = Guid.NewGuid();
        db.ProductTypes.Add(new ProductType { Id = ptId, Name = "Shell" });
        db.Products.Add(new Product { Id = Guid.NewGuid(), ProductNumber = "SH-100", TankSize = 100, TankType = "Shell", ProductTypeId = ptId });
        await db.SaveChangesAsync();

        var result = await controller.GetAllProducts(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<AdminProductDto>>(ok.Value);
        var items = list.ToList();
        Assert.Contains(items, p => p.ProductNumber == "SH-100" && p.ProductTypeName == "Shell");
    }

    [Fact]
    public async Task GetProductTypes_ReturnsTypes()
    {
        var controller = CreateController(out _);
        var result = await controller.GetProductTypes(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var list = Assert.IsAssignableFrom<IEnumerable<ProductTypeDto>>(ok.Value);
        Assert.NotEmpty(list);
    }

    [Fact]
    public async Task CreateProduct_AddsToDatabase()
    {
        var controller = CreateController(out var db);
        var ptId = db.ProductTypes.First().Id;

        var dto = new CreateProductDto
        {
            ProductNumber = "NEW-001",
            TankSize = 250,
            TankType = "Plate",
            ProductTypeId = ptId
        };

        var result = await controller.CreateProduct(dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<AdminProductDto>(ok.Value);
        Assert.Equal("NEW-001", created.ProductNumber);
        Assert.Equal(250, created.TankSize);
        Assert.True(db.Products.Any(p => p.ProductNumber == "NEW-001"));
    }

    [Fact]
    public async Task UpdateProduct_ModifiesExisting()
    {
        var controller = CreateController(out var db);
        var ptId = db.ProductTypes.First().Id;
        var product = new Product { Id = Guid.NewGuid(), ProductNumber = "OLD-001", TankSize = 100, TankType = "Shell", ProductTypeId = ptId };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var dto = new UpdateProductDto { ProductNumber = "UPDATED-001", TankSize = 200, TankType = "Plate", ProductTypeId = ptId };
        var result = await controller.UpdateProduct(product.Id, dto, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var updated = Assert.IsType<AdminProductDto>(ok.Value);
        Assert.Equal("UPDATED-001", updated.ProductNumber);
        Assert.Equal(200, updated.TankSize);
    }

    [Fact]
    public async Task UpdateProduct_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var dto = new UpdateProductDto { ProductNumber = "X", TankSize = 1, TankType = "X", ProductTypeId = Guid.NewGuid() };
        var result = await controller.UpdateProduct(Guid.NewGuid(), dto, CancellationToken.None);
        Assert.IsType<NotFoundResult>(result.Result);
    }

    [Fact]
    public async Task DeleteProduct_RemovesFromDatabase()
    {
        var controller = CreateController(out var db);
        var ptId = db.ProductTypes.First().Id;
        var product = new Product { Id = Guid.NewGuid(), ProductNumber = "DEL-001", TankSize = 100, TankType = "Shell", ProductTypeId = ptId };
        db.Products.Add(product);
        await db.SaveChangesAsync();

        var result = await controller.DeleteProduct(product.Id, CancellationToken.None);

        Assert.IsType<NoContentResult>(result);
        Assert.False(db.Products.Any(p => p.Id == product.Id));
    }

    [Fact]
    public async Task DeleteProduct_ReturnsNotFound_WhenMissing()
    {
        var controller = CreateController(out _);
        var result = await controller.DeleteProduct(Guid.NewGuid(), CancellationToken.None);
        Assert.IsType<NotFoundResult>(result);
    }
}
