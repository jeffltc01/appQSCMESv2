using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IInspectionRecordService
{
    Task<InspectionRecordResponseDto> CreateAsync(CreateInspectionRecordDto dto, CancellationToken cancellationToken = default);
}
