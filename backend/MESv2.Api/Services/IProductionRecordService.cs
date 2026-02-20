using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IProductionRecordService
{
    Task<CreateProductionRecordResponseDto> CreateAsync(CreateProductionRecordDto dto, CancellationToken cancellationToken = default);
}
