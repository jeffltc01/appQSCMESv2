using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/vendors")]
public class VendorsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly MesDbContext _db;

    public VendorsController(IProductService productService, MesDbContext db)
    {
        _productService = productService;
        _db = db;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<VendorDto>>> GetVendors(
        [FromQuery] string? type,
        [FromQuery] string? plantId,
        CancellationToken cancellationToken)
    {
        var list = await _productService.GetVendorsAsync(type, plantId, cancellationToken);
        return Ok(list);
    }

    [HttpGet("admin")]
    public async Task<ActionResult<IEnumerable<AdminVendorDto>>> GetAllVendors(CancellationToken cancellationToken)
    {
        var list = await _db.Vendors
            .OrderBy(v => v.Name)
            .Select(v => new AdminVendorDto
            {
                Id = v.Id,
                Name = v.Name,
                VendorType = v.VendorType,
                PlantIds = v.PlantIds,
                IsActive = v.IsActive
            })
            .ToListAsync(cancellationToken);
        return Ok(list);
    }

    [HttpPost]
    public async Task<ActionResult<AdminVendorDto>> CreateVendor([FromBody] CreateVendorDto dto, CancellationToken cancellationToken)
    {
        var vendor = new Vendor
        {
            Id = Guid.NewGuid(),
            Name = dto.Name,
            VendorType = dto.VendorType,
            PlantIds = dto.PlantIds,
            IsActive = true
        };
        _db.Vendors.Add(vendor);
        SyncVendorPlants(vendor.Id, dto.PlantIds);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new AdminVendorDto { Id = vendor.Id, Name = vendor.Name, VendorType = vendor.VendorType, PlantIds = vendor.PlantIds, IsActive = vendor.IsActive });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminVendorDto>> UpdateVendor(Guid id, [FromBody] UpdateVendorDto dto, CancellationToken cancellationToken)
    {
        var vendor = await _db.Vendors.FindAsync(new object[] { id }, cancellationToken);
        if (vendor == null) return NotFound();
        vendor.Name = dto.Name;
        vendor.VendorType = dto.VendorType;
        vendor.PlantIds = dto.PlantIds;
        vendor.IsActive = dto.IsActive;

        var existing = _db.VendorPlants.Where(vp => vp.VendorId == id);
        _db.VendorPlants.RemoveRange(existing);
        SyncVendorPlants(id, dto.PlantIds);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new AdminVendorDto { Id = vendor.Id, Name = vendor.Name, VendorType = vendor.VendorType, PlantIds = vendor.PlantIds, IsActive = vendor.IsActive });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<AdminVendorDto>> DeleteVendor(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await _db.Vendors.FindAsync(new object[] { id }, cancellationToken);
        if (vendor == null) return NotFound();
        vendor.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new AdminVendorDto { Id = vendor.Id, Name = vendor.Name, VendorType = vendor.VendorType, PlantIds = vendor.PlantIds, IsActive = vendor.IsActive });
    }

    private void SyncVendorPlants(Guid vendorId, string? plantIds)
    {
        if (string.IsNullOrWhiteSpace(plantIds)) return;

        var guids = plantIds.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => Guid.TryParse(s, out _))
            .Select(Guid.Parse)
            .ToList();

        _db.VendorPlants.AddRange(guids.Select(g => new VendorPlant
        {
            Id = Guid.NewGuid(),
            VendorId = vendorId,
            PlantId = g
        }));
    }
}
