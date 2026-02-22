using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IRealTimeXrayService
{
    Task<XrayInspectionResponseDto> ProcessInspectionAsync(XrayInspectionRequestDto dto, CancellationToken cancellationToken = default);
}
