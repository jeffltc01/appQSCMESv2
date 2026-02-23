using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface INlqToolExecutor
{
    Task<NaturalLanguageQueryResponseDto> ExecuteAsync(
        NlqIntent intent,
        NaturalLanguageQueryRequestDto request,
        Guid plantId,
        CancellationToken cancellationToken = default);
}
