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
        [FromQuery] string? siteCode,
        CancellationToken cancellationToken)
    {
        var list = await _productService.GetVendorsAsync(type, siteCode, cancellationToken);
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
                SiteCode = v.SiteCode,
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
            SiteCode = dto.SiteCode,
            IsActive = true
        };
        _db.Vendors.Add(vendor);
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new AdminVendorDto { Id = vendor.Id, Name = vendor.Name, VendorType = vendor.VendorType, SiteCode = vendor.SiteCode, IsActive = vendor.IsActive });
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<AdminVendorDto>> UpdateVendor(Guid id, [FromBody] UpdateVendorDto dto, CancellationToken cancellationToken)
    {
        var vendor = await _db.Vendors.FindAsync(new object[] { id }, cancellationToken);
        if (vendor == null) return NotFound();
        vendor.Name = dto.Name;
        vendor.VendorType = dto.VendorType;
        vendor.SiteCode = dto.SiteCode;
        vendor.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new AdminVendorDto { Id = vendor.Id, Name = vendor.Name, VendorType = vendor.VendorType, SiteCode = vendor.SiteCode, IsActive = vendor.IsActive });
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult> DeleteVendor(Guid id, CancellationToken cancellationToken)
    {
        var vendor = await _db.Vendors.FindAsync(new object[] { id }, cancellationToken);
        if (vendor == null) return NotFound();
        _db.Vendors.Remove(vendor);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
