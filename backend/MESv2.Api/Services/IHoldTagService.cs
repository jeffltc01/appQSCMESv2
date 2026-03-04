using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IHoldTagService
{
    Task<HoldTagDto> CreateHoldTagAsync(CreateHoldTagRequestDto dto, CancellationToken ct = default);
    Task<HoldTagDto> SetDispositionAsync(SetHoldTagDispositionRequestDto dto, CancellationToken ct = default);
    Task<HoldTagDto> LinkNcrAsync(LinkHoldTagNcrRequestDto dto, CancellationToken ct = default);
    Task<HoldTagDto> ResolveAsync(ResolveHoldTagRequestDto dto, CancellationToken ct = default);
    Task<HoldTagDto> VoidAsync(VoidHoldTagRequestDto dto, CancellationToken ct = default);
    Task<HoldTagDto?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IReadOnlyList<HoldTagDto>> GetListAsync(string? siteCode, CancellationToken ct = default);
}
