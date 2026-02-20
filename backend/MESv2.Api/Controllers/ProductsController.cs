using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/products")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly MesDbContext _db;

    public ProductsController(IProductService productService, MesDbContext db)
    {
        _productService = productService;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ProductListDto>>> GetProducts(
        [FromQuery] string? type,
        [FromQuery] string? siteCode,
        CancellationToken cancellationToken)
    {
        var list = await _productService.GetProductsAsync(type, siteCode, cancellationToken);
        return Ok(list);
    }

    [HttpGet("admin")]
    public async Task<ActionResult<IEnumerable<AdminProductDto>>> GetAllProducts(CancellationToken cancellationToken)
    {
        var list = await _db.Products
            .Include(p => p.ProductType)
            .OrderBy(p => p.TankType).ThenBy(p => p.TankSize)
            .Select(p => new AdminProductDto
            {
                Id = p.Id,
                ProductNumber = p.ProductNumber,
                TankSize = p.TankSize,
                TankType = p.TankType,
                SageItemNumber = p.SageItemNumber,
                NameplateNumber = p.NameplateNumber,
                ProductTypeId = p.ProductTypeId,
                ProductTypeName = p.ProductType.Name
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpGet("types")]
    public async Task<ActionResult<IEnumerable<ProductTypeDto>>> GetProductTypes(CancellationToken cancellationToken)
    {
        var list = await _db.ProductTypes
            .OrderBy(t => t.Name)
            .Select(t => new ProductTypeDto { Id = t.Id, Name = t.Name })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AdminProductDto>> CreateProduct([FromBody] CreateProductDto dto, CancellationToken cancellationToken)
    {
        var product = new Product
        {
            Id = Guid.NewGuid(),
            ProductNumber = dto.ProductNumber,
            TankSize = dto.TankSize,
            TankType = dto.TankType,
            SageItemNumber = dto.SageItemNumber,
            NameplateNumber = dto.NameplateNumber,
            ProductTypeId = dto.ProductTypeId
        };
        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        var pt = await _db.ProductTypes.FindAsync(new object[] { dto.ProductTypeId }, cancellationToken);
        return Ok(new AdminProductDto
        {
            Id = product.Id,
            ProductNumber = product.ProductNumber,
            TankSize = product.TankSize,
            TankType = product.TankType,
            SageItemNumber = product.SageItemNumber,
            NameplateNumber = product.NameplateNumber,
            ProductTypeId = product.ProductTypeId,
            ProductTypeName = pt?.Name ?? ""
        });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminProductDto>> UpdateProduct(Guid id, [FromBody] UpdateProductDto dto, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null) return NotFound();

        product.ProductNumber = dto.ProductNumber;
        product.TankSize = dto.TankSize;
        product.TankType = dto.TankType;
        product.SageItemNumber = dto.SageItemNumber;
        product.NameplateNumber = dto.NameplateNumber;
        product.ProductTypeId = dto.ProductTypeId;

        await _db.SaveChangesAsync(cancellationToken);

        var pt = await _db.ProductTypes.FindAsync(new object[] { dto.ProductTypeId }, cancellationToken);
        return Ok(new AdminProductDto
        {
            Id = product.Id,
            ProductNumber = product.ProductNumber,
            TankSize = product.TankSize,
            TankType = product.TankType,
            SageItemNumber = product.SageItemNumber,
            NameplateNumber = product.NameplateNumber,
            ProductTypeId = product.ProductTypeId,
            ProductTypeName = pt?.Name ?? ""
        });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteProduct(Guid id, CancellationToken cancellationToken)
    {
        var product = await _db.Products.FindAsync(new object[] { id }, cancellationToken);
        if (product == null) return NotFound();
        _db.Products.Remove(product);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
