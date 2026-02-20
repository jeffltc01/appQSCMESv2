using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public class ProductService : IProductService
{
    private readonly MesDbContext _db;

    public ProductService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ProductListDto>> GetProductsAsync(string? type, string? siteCode, CancellationToken cancellationToken = default)
    {
        var query = _db.Products.Include(p => p.ProductType).AsQueryable();

        if (!string.IsNullOrEmpty(type))
            query = query.Where(p => p.ProductType.SystemTypeName == type);

        var list = await query
            .OrderBy(p => p.TankSize)
            .ThenBy(p => p.ProductNumber)
            .ToListAsync(cancellationToken);

        return list.Select(p => new ProductListDto
        {
            Id = p.Id,
            ProductNumber = p.ProductNumber,
            TankSize = p.TankSize,
            TankType = p.TankType,
            NameplateNumber = p.NameplateNumber
        }).ToList();
    }

    public async Task<IReadOnlyList<VendorDto>> GetVendorsAsync(string? type, string? siteCode, CancellationToken cancellationToken = default)
    {
        var query = _db.Vendors.Where(v => v.IsActive);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(v => v.VendorType == type);

        if (!string.IsNullOrEmpty(siteCode))
            query = query.Where(v => v.SiteCode == null || v.SiteCode == siteCode);

        var list = await query
            .OrderBy(v => v.Name)
            .ToListAsync(cancellationToken);

        return list.Select(v => new VendorDto
        {
            Id = v.Id,
            Name = v.Name,
            VendorType = v.VendorType
        }).ToList();
    }
}
