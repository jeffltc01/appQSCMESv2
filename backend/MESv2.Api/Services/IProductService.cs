using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IProductService
{
    Task<IReadOnlyList<ProductListDto>> GetProductsAsync(string? type, string? siteCode, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<VendorDto>> GetVendorsAsync(string? type, string? siteCode, CancellationToken cancellationToken = default);
}
