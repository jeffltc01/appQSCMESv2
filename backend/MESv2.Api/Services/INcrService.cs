using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface INcrService
{
    Task<IReadOnlyList<NcrTypeDto>> GetNcrTypesAsync(bool includeInactive, CancellationToken ct = default);
    Task<NcrTypeDto> UpsertNcrTypeAsync(UpsertNcrTypeRequestDto dto, CancellationToken ct = default);
    Task<IReadOnlyList<NcrDto>> GetNcrsAsync(string? siteCode, CancellationToken ct = default);
    Task<NcrDto?> GetNcrByIdAsync(Guid id, CancellationToken ct = default);
    Task<NcrDto> CreateNcrAsync(CreateNcrRequestDto dto, CancellationToken ct = default);
    Task<NcrDto> UpdateNcrDataAsync(UpdateNcrDataRequestDto dto, CancellationToken ct = default);
    Task<NcrDto> SubmitNcrStepAsync(SubmitNcrStepRequestDto dto, CancellationToken ct = default);
    Task<NcrDto> ApproveNcrStepAsync(NcrDecisionRequestDto dto, CancellationToken ct = default);
    Task<NcrDto> RejectNcrStepAsync(NcrDecisionRequestDto dto, CancellationToken ct = default);
    Task<NcrDto> VoidNcrAsync(VoidNcrRequestDto dto, CancellationToken ct = default);
    Task AddAttachmentAsync(AddNcrAttachmentRequestDto dto, CancellationToken ct = default);
}
