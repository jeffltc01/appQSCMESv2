using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IIssueRequestService
{
    Task<IssueRequestDto> SubmitAsync(CreateIssueRequestDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<IssueRequestDto>> GetPendingAsync(CancellationToken ct = default);
    Task<IReadOnlyList<IssueRequestDto>> GetMyRequestsAsync(Guid userId, CancellationToken ct = default);
    Task<IssueRequestDto> ApproveAsync(Guid id, ApproveIssueRequestDto dto, CancellationToken ct = default);
    Task<IssueRequestDto> RejectAsync(Guid id, RejectIssueRequestDto dto, CancellationToken ct = default);
}
