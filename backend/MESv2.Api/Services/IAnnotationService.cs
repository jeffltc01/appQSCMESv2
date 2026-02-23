using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IAnnotationService
{
    Task<List<AdminAnnotationDto>> GetAllAsync(Guid? siteId, CancellationToken ct = default);
    Task<AdminAnnotationDto> CreateAsync(CreateAnnotationDto dto, CancellationToken ct = default);
    Task<AdminAnnotationDto> CreateForProductionRecordAsync(CreateLogAnnotationDto dto, CancellationToken ct = default);
    Task<AdminAnnotationDto?> UpdateAsync(Guid id, UpdateAnnotationDto dto, CancellationToken ct = default);
}
