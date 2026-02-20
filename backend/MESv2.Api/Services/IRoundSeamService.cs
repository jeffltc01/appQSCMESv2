using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IRoundSeamService
{
    Task<RoundSeamSetupDto?> GetSetupAsync(Guid wcId, CancellationToken cancellationToken = default);
    Task<RoundSeamSetupDto> SaveSetupAsync(Guid wcId, CreateRoundSeamSetupDto dto, CancellationToken cancellationToken = default);
    Task<CreateProductionRecordResponseDto> CreateRoundSeamRecordAsync(CreateRoundSeamRecordDto dto, CancellationToken cancellationToken = default);
    Task<AssemblyLookupDto?> GetAssemblyByShellAsync(string serial, CancellationToken cancellationToken = default);
}
