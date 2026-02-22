using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IAIReviewService
{
    Task<IReadOnlyList<AIReviewRecordDto>> GetRecordsAsync(
        Guid wcId, Guid plantId, string date, CancellationToken cancellationToken = default);

    Task<AIReviewResultDto> SubmitReviewAsync(
        Guid userId, CreateAIReviewRequest request, CancellationToken cancellationToken = default);
}
