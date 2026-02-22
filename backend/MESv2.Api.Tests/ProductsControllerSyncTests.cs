using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class ProductsControllerSyncTests
{
    private static readonly Guid Plt1 = TestHelpers.PlantPlt1Id;
    private static readonly Guid Plt2 = TestHelpers.PlantPlt2Id;
    private static readonly Guid Plt3 = TestHelpers.PlantPlt3Id;

    private ProductsController CreateController(out Data.MesDbContext db)
    {
        db = TestHelpers.CreateInMemoryContext();
        var productService = new ProductService(db);
        return new ProductsController(productService, db);
    }

    [Fact]
    public async Task CreateProduct_PopulatesProductPlants()
    {
        var controller = CreateController(out var db);
        var ptId = db.ProductTypes.First(pt => pt.SystemTypeName == "plate").Id;

        var dto = new CreateProductDto
        {
            ProductNumber = "SYNC-TEST-01",
            TankSize = 999,
            TankType = "Plate",
            SiteNumbers = "000,700",
            ProductTypeId = ptId
        };

        var result = await controller.CreateProduct(dto, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<AdminProductDto>(ok.Value);

        var ppRows = db.ProductPlants.Where(pp => pp.ProductId == created.Id).ToList();
        Assert.Equal(2, ppRows.Count);
        Assert.Contains(ppRows, pp => pp.PlantId == Plt1);
        Assert.Contains(ppRows, pp => pp.PlantId == Plt3);
    }

    [Fact]
    public async Task CreateProduct_NullSiteNumbers_NoProductPlantRows()
    {
        var controller = CreateController(out var db);
        var ptId = db.ProductTypes.First(pt => pt.SystemTypeName == "plate").Id;

        var dto = new CreateProductDto
        {
            ProductNumber = "SYNC-NULL",
            TankSize = 100,
            TankType = "Plate",
            SiteNumbers = null,
            ProductTypeId = ptId
        };

        var result = await controller.CreateProduct(dto, CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var created = Assert.IsType<AdminProductDto>(ok.Value);

        Assert.Empty(db.ProductPlants.Where(pp => pp.ProductId == created.Id));
    }

    [Fact]
    public async Task UpdateProduct_ReplacesProductPlants()
    {
        var controller = CreateController(out var db);
        var ptId = db.ProductTypes.First(pt => pt.SystemTypeName == "plate").Id;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            ProductNumber = "SYNC-UPD",
            TankSize = 120,
            TankType = "Plate",
            SiteNumbers = "000",
            ProductTypeId = ptId
        };
        db.Products.Add(product);
        db.ProductPlants.Add(new ProductPlant { Id = Guid.NewGuid(), ProductId = product.Id, PlantId = Plt1 });
        await db.SaveChangesAsync();

        var dto = new UpdateProductDto
        {
            ProductNumber = "SYNC-UPD",
            TankSize = 120,
            TankType = "Plate",
            SiteNumbers = "600,700",
            ProductTypeId = ptId,
            IsActive = true
        };

        await controller.UpdateProduct(product.Id, dto, CancellationToken.None);

        var ppRows = db.ProductPlants.Where(pp => pp.ProductId == product.Id).ToList();
        Assert.Equal(2, ppRows.Count);
        Assert.DoesNotContain(ppRows, pp => pp.PlantId == Plt1);
        Assert.Contains(ppRows, pp => pp.PlantId == Plt2);
        Assert.Contains(ppRows, pp => pp.PlantId == Plt3);
    }
}
