using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public interface IChecklistService
{
    Task<IReadOnlyList<ScoreTypeDto>> GetScoreTypesAsync(bool includeArchived, CancellationToken ct = default);
    Task<ScoreTypeDto?> GetScoreTypeAsync(Guid scoreTypeId, CancellationToken ct = default);
    Task<ScoreTypeDto> UpsertScoreTypeAsync(UpsertScoreTypeRequestDto request, Guid userId, decimal callerRoleTier, CancellationToken ct = default);
    Task<IReadOnlyList<ChecklistTemplateDto>> GetTemplatesAsync(Guid? siteId, string? checklistType, CancellationToken ct = default);
    Task<ChecklistTemplateDto?> GetTemplateAsync(Guid templateId, CancellationToken ct = default);
    Task<ChecklistTemplateDto> UpsertTemplateAsync(UpsertChecklistTemplateRequestDto request, Guid userId, decimal callerRoleTier, Guid callerSiteId, CancellationToken ct = default);
    Task<ChecklistTemplateDto?> ResolveTemplateAsync(ResolveChecklistTemplateRequestDto request, Guid callerSiteId, CancellationToken ct = default);
    Task<ChecklistEntryDto> CreateEntryAsync(CreateChecklistEntryRequestDto request, Guid callerSiteId, decimal callerRoleTier, CancellationToken ct = default);
    Task<ChecklistEntryDto?> SubmitResponsesAsync(Guid entryId, SubmitChecklistResponsesRequestDto request, Guid callerSiteId, decimal callerRoleTier, CancellationToken ct = default);
    Task<ChecklistEntryDto?> CompleteEntryAsync(Guid entryId, Guid callerSiteId, decimal callerRoleTier, CancellationToken ct = default);
    Task<IReadOnlyList<ChecklistEntryDto>> GetEntryHistoryAsync(Guid siteId, Guid? workCenterId, string? checklistType, CancellationToken ct = default);
    Task<ChecklistEntryDto?> GetEntryDetailAsync(Guid entryId, Guid callerSiteId, CancellationToken ct = default);
    Task<ChecklistReviewSummaryDto> GetReviewSummaryAsync(Guid siteId, DateTime fromUtc, DateTime toUtc, string? checklistType, CancellationToken ct = default);
    Task<ChecklistQuestionResponsesDto> GetQuestionResponsesAsync(Guid siteId, DateTime fromUtc, DateTime toUtc, Guid checklistTemplateItemId, string? checklistType, CancellationToken ct = default);
}
