using Microsoft.AspNetCore.Mvc;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Controllers;

[ApiController]
[Route("api/vendors")]
public class VendorsController : ControllerBase
{
    private readonly IProductService _productService;

    public VendorsController(IProductService productService)
    {
        _productService = productService;
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
}
