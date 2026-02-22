using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface INameplateService
{
    Task<NameplateRecordResponseDto> CreateAsync(CreateNameplateRecordDto dto, CancellationToken cancellationToken = default);
    Task<NameplateRecordResponseDto?> GetBySerialAsync(string serialNumber, CancellationToken cancellationToken = default);
    Task<NameplateRecordResponseDto> ReprintAsync(Guid id, CancellationToken cancellationToken = default);
}
