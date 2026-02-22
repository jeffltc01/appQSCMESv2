using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface ILimbleService
{
    Task<List<LimbleStatusDto>> GetStatusesAsync(CancellationToken cancellationToken = default);
    Task<List<LimbleTaskDto>> GetMyRequestsAsync(string employeeNumber, CancellationToken cancellationToken = default);
    Task<LimbleTaskDto> CreateWorkRequestAsync(CreateLimbleWorkRequestDto dto, CancellationToken cancellationToken = default);
}
