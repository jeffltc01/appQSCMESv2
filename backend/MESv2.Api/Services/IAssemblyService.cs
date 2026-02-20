using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IAssemblyService
{
    Task<CreateAssemblyResponseDto> CreateAsync(CreateAssemblyDto dto, CancellationToken cancellationToken = default);
    Task<string> GetNextAlphaCodeAsync(Guid plantId, CancellationToken cancellationToken = default);
    Task<CreateAssemblyResponseDto> ReassembleAsync(string alphaCode, ReassemblyDto dto, CancellationToken cancellationToken = default);
}
