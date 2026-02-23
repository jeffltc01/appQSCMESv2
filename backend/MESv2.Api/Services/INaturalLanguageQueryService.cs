using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface INaturalLanguageQueryService
{
    Task<NaturalLanguageQueryResponseDto> AskAsync(
        Guid callerUserId,
        decimal callerRoleTier,
        Guid callerSiteId,
        NaturalLanguageQueryRequestDto request,
        CancellationToken cancellationToken = default);
}
