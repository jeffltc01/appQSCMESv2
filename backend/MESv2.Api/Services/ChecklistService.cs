using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class ChecklistService : IChecklistService
{
    private static readonly HashSet<string> AllowedResponses = new(StringComparer.OrdinalIgnoreCase)
    {
        "Pass", "Fail", "N/A"
    };

    private readonly MesDbContext _db;

    public ChecklistService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ChecklistTemplateDto>> GetTemplatesAsync(Guid? siteId, string? checklistType, CancellationToken ct = default)
    {
        var query = _db.ChecklistTemplates
            .Include(t => t.Items.OrderBy(i => i.SortOrder))
            .AsQueryable();

        if (siteId.HasValue)
        {
            query = query.Where(t => t.SiteId == siteId || t.SiteId == null);
        }

        if (!string.IsNullOrWhiteSpace(checklistType))
        {
            query = query.Where(t => t.ChecklistType == checklistType);
        }

        var templates = await query
            .OrderBy(t => t.ChecklistType)
            .ThenBy(t => t.TemplateCode)
            .ThenByDescending(t => t.VersionNo)
            .ToListAsync(ct);

        return templates.Select(MapTemplate).ToList();
    }

    public async Task<ChecklistTemplateDto?> GetTemplateAsync(Guid templateId, CancellationToken ct = default)
    {
        var template = await _db.ChecklistTemplates
            .Include(t => t.Items.OrderBy(i => i.SortOrder))
            .FirstOrDefaultAsync(t => t.Id == templateId, ct);
        return template == null ? null : MapTemplate(template);
    }

    public async Task<ChecklistTemplateDto> UpsertTemplateAsync(UpsertChecklistTemplateRequestDto request, Guid userId, decimal callerRoleTier, Guid callerSiteId, CancellationToken ct = default)
    {
        return await UpsertTemplateInternalAsync(request, userId, callerRoleTier, callerSiteId, retryOnConcurrency: true, ct);
    }

    private async Task<ChecklistTemplateDto> UpsertTemplateInternalAsync(
        UpsertChecklistTemplateRequestDto request,
        Guid userId,
        decimal callerRoleTier,
        Guid callerSiteId,
        bool retryOnConcurrency,
        CancellationToken ct)
    {
        if (callerRoleTier > 4m)
        {
            throw new InvalidOperationException("Supervisor or above required for checklist template management.");
        }

        ValidateScope(request.ScopeLevel, request.SiteId, request.WorkCenterId);
        ValidateItems(request.Items, request.ResponseMode, request.RequireFailNote || request.IsSafetyProfile);

        if (request.SiteId.HasValue && callerRoleTier > 2m && request.SiteId.Value != callerSiteId)
        {
            throw new InvalidOperationException("Cross-site template management is not allowed for this role.");
        }

        await EnsureNoActiveOverlapAsync(request, ct);

        ChecklistTemplate template;
        if (request.Id.HasValue)
        {
            template = await _db.ChecklistTemplates
                .Include(t => t.Items)
                .FirstOrDefaultAsync(t => t.Id == request.Id!.Value, ct)
                ?? throw new KeyNotFoundException("Checklist template not found.");
        }
        else
        {
            template = new ChecklistTemplate
            {
                Id = Guid.NewGuid(),
                CreatedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.ChecklistTemplates.Add(template);
        }

        template.TemplateCode = request.TemplateCode.Trim();
        template.Title = request.Title.Trim();
        template.ChecklistType = request.ChecklistType.Trim();
        template.ScopeLevel = request.ScopeLevel.Trim();
        template.SiteId = request.SiteId;
        template.WorkCenterId = request.WorkCenterId;
        template.ProductionLineId = request.ProductionLineId;
        template.VersionNo = request.VersionNo;
        template.EffectiveFromUtc = DateTime.SpecifyKind(request.EffectiveFromUtc, DateTimeKind.Utc);
        template.EffectiveToUtc = request.EffectiveToUtc.HasValue
            ? DateTime.SpecifyKind(request.EffectiveToUtc.Value, DateTimeKind.Utc)
            : null;
        template.IsActive = request.IsActive;
        template.ResponseMode = request.ResponseMode;
        template.RequireFailNote = request.RequireFailNote;
        template.IsSafetyProfile = request.IsSafetyProfile;

        var existingById = template.Items.ToDictionary(i => i.Id, i => i);
        var explicitDeleteIds = request.DeletedItemIds.ToHashSet();
        var toRemove = template.Items.Where(i => explicitDeleteIds.Contains(i.Id)).ToList();

        if (toRemove.Count > 0)
        {
            var removeIds = toRemove.Select(i => i.Id).ToList();
            var referencedIds = await _db.ChecklistEntryItemResponses
                .Where(r => removeIds.Contains(r.ChecklistTemplateItemId))
                .Select(r => r.ChecklistTemplateItemId)
                .Distinct()
                .ToListAsync(ct);

            if (referencedIds.Count > 0)
            {
                throw new InvalidOperationException("One or more checklist questions cannot be removed because responses already exist. Keep those questions or create a new template version.");
            }

            foreach (var item in toRemove)
            {
                template.Items.Remove(item);
            }
        }

        var referencedExistingIds = await _db.ChecklistEntryItemResponses
            .Where(r => r.ChecklistTemplateItem.ChecklistTemplateId == template.Id)
            .Select(r => r.ChecklistTemplateItemId)
            .Distinct()
            .ToHashSetAsync(ct);

        foreach (var itemDto in request.Items.OrderBy(i => i.SortOrder))
        {
            ChecklistTemplateItem item;
            if (itemDto.Id.HasValue && existingById.TryGetValue(itemDto.Id.Value, out var existing))
            {
                var isUnchanged = IsSameItemDefinition(existing, itemDto, request.ResponseMode);
                if (isUnchanged)
                {
                    continue;
                }

                if (referencedExistingIds.Contains(existing.Id))
                {
                    EnsureReferencedItemUnchanged(existing, itemDto, request.ResponseMode);
                    continue;
                }

                item = existing;
            }
            else
            {
                item = new ChecklistTemplateItem
                {
                    Id = itemDto.Id ?? Guid.NewGuid(),
                    ChecklistTemplateId = template.Id
                };
                _db.ChecklistTemplateItems.Add(item);
            }

            item.SortOrder = itemDto.SortOrder;
            item.Prompt = itemDto.Prompt.Trim();
            item.IsRequired = itemDto.IsRequired;
            item.ResponseMode = string.IsNullOrWhiteSpace(itemDto.ResponseMode) ? null : itemDto.ResponseMode.Trim();
            item.ResponseType = ResolveResponseType(itemDto.ResponseType, itemDto.ResponseMode, request.ResponseMode);
            item.ResponseOptionsJson = SerializeResponseOptions(itemDto.ResponseOptions);
            item.HelpText = string.IsNullOrWhiteSpace(itemDto.HelpText) ? null : itemDto.HelpText.Trim();
            item.RequireFailNote = itemDto.RequireFailNote;
        }

        try
        {
            await _db.SaveChangesAsync(ct);
        }
        catch (DbUpdateConcurrencyException) when (retryOnConcurrency)
        {
            // Retry once against a fresh tracked graph. This handles transient races
            // where another operation changed/deleted the same template rows.
            _db.ChangeTracker.Clear();
            return await UpsertTemplateInternalAsync(request, userId, callerRoleTier, callerSiteId, retryOnConcurrency: false, ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new InvalidOperationException("Checklist template was modified by another operation. Please refresh and try saving again.");
        }

        return await GetTemplateAsync(template.Id, ct) ?? throw new InvalidOperationException("Checklist template save failed.");
    }

    public async Task<ChecklistTemplateDto?> ResolveTemplateAsync(ResolveChecklistTemplateRequestDto request, Guid callerSiteId, CancellationToken ct = default)
    {
        if (request.SiteId != callerSiteId)
        {
            throw new InvalidOperationException("Site access denied.");
        }

        var now = DateTime.UtcNow;
        var candidates = await _db.ChecklistTemplates
            .Include(t => t.Items.OrderBy(i => i.SortOrder))
            .Where(t =>
                t.IsActive &&
                t.ChecklistType == request.ChecklistType &&
                t.EffectiveFromUtc <= now &&
                (t.EffectiveToUtc == null || t.EffectiveToUtc >= now))
            .ToListAsync(ct);

        var selected = ResolveByScope(candidates, request.SiteId, request.WorkCenterId, request.ProductionLineId);
        return selected == null ? null : MapTemplate(selected);
    }

    public async Task<ChecklistEntryDto> CreateEntryAsync(CreateChecklistEntryRequestDto request, Guid callerSiteId, decimal callerRoleTier, CancellationToken ct = default)
    {
        if (request.SiteId != callerSiteId)
        {
            throw new InvalidOperationException("Site access denied.");
        }

        if (request.ChecklistType.Equals("SafetyPeriodic", StringComparison.OrdinalIgnoreCase) && callerRoleTier > 4m)
        {
            throw new InvalidOperationException("Supervisor or above required for periodic safety audits.");
        }

        var resolved = await ResolveTemplateAsync(new ResolveChecklistTemplateRequestDto
        {
            ChecklistType = request.ChecklistType,
            SiteId = request.SiteId,
            WorkCenterId = request.WorkCenterId,
            ProductionLineId = request.ProductionLineId
        }, callerSiteId, ct) ?? throw new InvalidOperationException("No active checklist template resolved for this context.");

        var entry = new ChecklistEntry
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = resolved.Id,
            ChecklistType = request.ChecklistType,
            SiteId = request.SiteId,
            WorkCenterId = request.WorkCenterId,
            ProductionLineId = request.ProductionLineId,
            OperatorUserId = request.OperatorUserId,
            Status = ChecklistEntryStatuses.InProgress,
            StartedAtUtc = DateTime.UtcNow,
            ResolvedFromScope = resolved.ScopeLevel,
            ResolvedTemplateCode = resolved.TemplateCode,
            ResolvedTemplateVersionNo = resolved.VersionNo
        };

        _db.ChecklistEntries.Add(entry);
        await _db.SaveChangesAsync(ct);
        return await GetEntryDetailAsync(entry.Id, callerSiteId, ct) ?? throw new InvalidOperationException("Checklist entry creation failed.");
    }

    public async Task<ChecklistEntryDto?> SubmitResponsesAsync(Guid entryId, SubmitChecklistResponsesRequestDto request, Guid callerSiteId, decimal callerRoleTier, CancellationToken ct = default)
    {
        var entry = await _db.ChecklistEntries
            .Include(e => e.ChecklistTemplate).ThenInclude(t => t.Items)
            .Include(e => e.Responses)
            .FirstOrDefaultAsync(e => e.Id == entryId, ct);

        if (entry == null)
        {
            return null;
        }

        if (entry.SiteId != callerSiteId)
        {
            throw new InvalidOperationException("Site access denied.");
        }

        if (entry.ChecklistType.Equals("SafetyPeriodic", StringComparison.OrdinalIgnoreCase) && callerRoleTier > 4m)
        {
            throw new InvalidOperationException("Supervisor or above required for periodic safety audits.");
        }

        var templateItemMap = entry.ChecklistTemplate.Items.ToDictionary(i => i.Id, i => i);
        foreach (var response in request.Responses)
        {
            if (!templateItemMap.ContainsKey(response.ChecklistTemplateItemId))
            {
                throw new InvalidOperationException("Response references an invalid template item.");
            }

            if (!AllowedResponses.Contains(response.ResponseValue))
            {
                throw new InvalidOperationException("Invalid response value.");
            }
        }

        foreach (var response in request.Responses)
        {
            var templateItem = templateItemMap[response.ChecklistTemplateItemId];
            if (!string.Equals(templateItem.ResponseType, ChecklistQuestionResponseTypes.PassFail, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("Only PassFail checklist questions are currently supported during checklist execution.");
            }
            var normalizedResponse = NormalizeResponse(response.ResponseValue);
            ValidateResponseForMode(normalizedResponse, templateItem.ResponseMode ?? entry.ChecklistTemplate.ResponseMode);

            var failNeedsNote =
                normalizedResponse == "Fail" &&
                (templateItem.RequireFailNote || entry.ChecklistTemplate.RequireFailNote || entry.ChecklistTemplate.IsSafetyProfile);
            if (failNeedsNote && string.IsNullOrWhiteSpace(response.Note))
            {
                throw new InvalidOperationException("Failure note is required for failed checklist items.");
            }

            var existing = entry.Responses.FirstOrDefault(r => r.ChecklistTemplateItemId == response.ChecklistTemplateItemId);
            if (existing == null)
            {
                existing = new ChecklistEntryItemResponse
                {
                    Id = Guid.NewGuid(),
                    ChecklistEntryId = entry.Id,
                    ChecklistTemplateItemId = response.ChecklistTemplateItemId
                };
                _db.ChecklistEntryItemResponses.Add(existing);
            }

            existing.ResponseValue = normalizedResponse;
            existing.Note = string.IsNullOrWhiteSpace(response.Note) ? null : response.Note.Trim();
            existing.RespondedAtUtc = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        return await GetEntryDetailAsync(entryId, callerSiteId, ct);
    }

    public async Task<ChecklistEntryDto?> CompleteEntryAsync(Guid entryId, Guid callerSiteId, decimal callerRoleTier, CancellationToken ct = default)
    {
        var entry = await _db.ChecklistEntries
            .Include(e => e.ChecklistTemplate).ThenInclude(t => t.Items)
            .Include(e => e.Responses)
            .FirstOrDefaultAsync(e => e.Id == entryId, ct);

        if (entry == null)
        {
            return null;
        }

        if (entry.SiteId != callerSiteId)
        {
            throw new InvalidOperationException("Site access denied.");
        }

        if (entry.ChecklistType.Equals("SafetyPeriodic", StringComparison.OrdinalIgnoreCase) && callerRoleTier > 4m)
        {
            throw new InvalidOperationException("Supervisor or above required for periodic safety audits.");
        }

        var requiredItemIds = entry.ChecklistTemplate.Items.Where(i => i.IsRequired).Select(i => i.Id).ToHashSet();
        var answeredIds = entry.Responses.Select(r => r.ChecklistTemplateItemId).ToHashSet();
        if (!requiredItemIds.IsSubsetOf(answeredIds))
        {
            throw new InvalidOperationException("All required checklist items must be answered before completion.");
        }

        entry.Status = ChecklistEntryStatuses.Completed;
        entry.CompletedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);

        return await GetEntryDetailAsync(entryId, callerSiteId, ct);
    }

    public async Task<IReadOnlyList<ChecklistEntryDto>> GetEntryHistoryAsync(Guid siteId, Guid? workCenterId, string? checklistType, CancellationToken ct = default)
    {
        var query = _db.ChecklistEntries
            .Include(e => e.Responses)
            .Where(e => e.SiteId == siteId);

        if (workCenterId.HasValue)
        {
            query = query.Where(e => e.WorkCenterId == workCenterId);
        }

        if (!string.IsNullOrWhiteSpace(checklistType))
        {
            query = query.Where(e => e.ChecklistType == checklistType);
        }

        var entries = await query
            .OrderByDescending(e => e.StartedAtUtc)
            .Take(200)
            .ToListAsync(ct);

        return entries.Select(MapEntry).ToList();
    }

    public async Task<ChecklistEntryDto?> GetEntryDetailAsync(Guid entryId, Guid callerSiteId, CancellationToken ct = default)
    {
        var entry = await _db.ChecklistEntries
            .Include(e => e.Responses)
            .FirstOrDefaultAsync(e => e.Id == entryId, ct);

        if (entry == null || entry.SiteId != callerSiteId)
        {
            return null;
        }

        return MapEntry(entry);
    }

    private async Task EnsureNoActiveOverlapAsync(UpsertChecklistTemplateRequestDto request, CancellationToken ct)
    {
        if (!request.IsActive)
        {
            return;
        }

        var fromUtc = DateTime.SpecifyKind(request.EffectiveFromUtc, DateTimeKind.Utc);
        var toUtc = request.EffectiveToUtc.HasValue ? DateTime.SpecifyKind(request.EffectiveToUtc.Value, DateTimeKind.Utc) : (DateTime?)null;

        var overlapExists = await _db.ChecklistTemplates
            .Where(t => t.IsActive)
            .Where(t => t.Id != request.Id)
            .Where(t => t.ChecklistType == request.ChecklistType)
            .Where(t => t.ScopeLevel == request.ScopeLevel)
            .Where(t => t.SiteId == request.SiteId)
            .Where(t => t.WorkCenterId == request.WorkCenterId)
            .Where(t => t.ProductionLineId == request.ProductionLineId)
            .AnyAsync(t =>
                t.EffectiveFromUtc <= (toUtc ?? DateTime.MaxValue) &&
                (t.EffectiveToUtc ?? DateTime.MaxValue) >= fromUtc, ct);

        if (overlapExists)
        {
            throw new InvalidOperationException("An overlapping active template already exists for this checklist type and scope.");
        }
    }

    private static ChecklistTemplate? ResolveByScope(
        IEnumerable<ChecklistTemplate> templates,
        Guid siteId,
        Guid workCenterId,
        Guid? productionLineId)
    {
        var ordered = templates
            .OrderByDescending(t => t.VersionNo)
            .ThenByDescending(t => t.EffectiveFromUtc)
            .ToList();

        var plantWc = ordered.FirstOrDefault(t =>
            t.ScopeLevel == ChecklistScopeLevels.PlantWorkCenter &&
            t.SiteId == siteId &&
            t.WorkCenterId == workCenterId &&
            (t.ProductionLineId == null || t.ProductionLineId == productionLineId));
        if (plantWc != null) return plantWc;

        var siteDefault = ordered.FirstOrDefault(t =>
            t.ScopeLevel == ChecklistScopeLevels.SiteDefault &&
            t.SiteId == siteId &&
            t.WorkCenterId == null);
        if (siteDefault != null) return siteDefault;

        return ordered.FirstOrDefault(t =>
            t.ScopeLevel == ChecklistScopeLevels.GlobalDefault &&
            t.SiteId == null &&
            t.WorkCenterId == null);
    }

    private static void ValidateScope(string scopeLevel, Guid? siteId, Guid? workCenterId)
    {
        if (scopeLevel == ChecklistScopeLevels.PlantWorkCenter && (!siteId.HasValue || !workCenterId.HasValue))
        {
            throw new InvalidOperationException("PlantWorkCenter scope requires both site and work center.");
        }

        if (scopeLevel == ChecklistScopeLevels.SiteDefault && !siteId.HasValue)
        {
            throw new InvalidOperationException("SiteDefault scope requires site.");
        }

        if (scopeLevel == ChecklistScopeLevels.GlobalDefault && (siteId.HasValue || workCenterId.HasValue))
        {
            throw new InvalidOperationException("GlobalDefault scope cannot include site or work center.");
        }
    }

    private static void ValidateItems(IReadOnlyList<ChecklistTemplateItemDto> items, string templateResponseMode, bool templateFailNoteRequired)
    {
        if (items.Count == 0)
        {
            throw new InvalidOperationException("At least one checklist item is required.");
        }

        foreach (var item in items)
        {
            if (string.IsNullOrWhiteSpace(item.Prompt))
            {
                throw new InvalidOperationException("Checklist item prompt is required.");
            }

            var mode = item.ResponseMode ?? templateResponseMode;
            if (mode != ChecklistResponseModes.PassFail && mode != ChecklistResponseModes.PassFailNa)
            {
                throw new InvalidOperationException("Unsupported checklist response mode.");
            }

            var responseType = ResolveResponseType(item.ResponseType, item.ResponseMode, templateResponseMode);
            var responseOptions = NormalizeResponseOptions(item.ResponseOptions);
            if (responseType == ChecklistQuestionResponseTypes.Select)
            {
                if (responseOptions.Count < 2)
                {
                    throw new InvalidOperationException("Select checklist questions require at least two options.");
                }
            }
            else if (responseOptions.Count > 0)
            {
                throw new InvalidOperationException("Only Select checklist questions can define response options.");
            }

            if ((item.RequireFailNote || templateFailNoteRequired) && mode != ChecklistResponseModes.PassFail)
            {
                throw new InvalidOperationException("Fail-note enforcement requires PF response mode.");
            }
        }
    }

    private static string ResolveResponseType(string? explicitResponseType, string? itemResponseMode, string templateResponseMode)
    {
        if (!string.IsNullOrWhiteSpace(explicitResponseType))
        {
            var normalizedType = explicitResponseType.Trim();
            if (normalizedType != ChecklistQuestionResponseTypes.PassFail &&
                normalizedType != ChecklistQuestionResponseTypes.Text &&
                normalizedType != ChecklistQuestionResponseTypes.Select &&
                normalizedType != ChecklistQuestionResponseTypes.Date)
            {
                throw new InvalidOperationException("Unsupported checklist response type.");
            }

            return normalizedType;
        }

        // Backward compatibility: legacy templates imply pass/fail via response mode fields.
        var mode = itemResponseMode ?? templateResponseMode;
        return mode == ChecklistResponseModes.PassFail || mode == ChecklistResponseModes.PassFailNa
            ? ChecklistQuestionResponseTypes.PassFail
            : ChecklistQuestionResponseTypes.PassFail;
    }

    private static List<string> NormalizeResponseOptions(IEnumerable<string>? options) =>
        (options ?? [])
            .Select(o => o.Trim())
            .Where(o => !string.IsNullOrWhiteSpace(o))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string? SerializeResponseOptions(IEnumerable<string>? options)
    {
        var normalized = NormalizeResponseOptions(options);
        return normalized.Count == 0 ? null : JsonSerializer.Serialize(normalized);
    }

    private static List<string> DeserializeResponseOptions(string? optionsJson)
    {
        if (string.IsNullOrWhiteSpace(optionsJson))
        {
            return [];
        }

        try
        {
            return NormalizeResponseOptions(JsonSerializer.Deserialize<List<string>>(optionsJson) ?? []);
        }
        catch
        {
            return [];
        }
    }

    private static void EnsureReferencedItemUnchanged(ChecklistTemplateItem existing, ChecklistTemplateItemDto incoming, string templateResponseMode)
    {
        if (!IsSameItemDefinition(existing, incoming, templateResponseMode))
        {
            throw new InvalidOperationException("Checklist questions with captured responses cannot be edited. Create a new template version to change answered questions.");
        }
    }

    private static bool IsSameItemDefinition(ChecklistTemplateItem existing, ChecklistTemplateItemDto incoming, string templateResponseMode)
    {
        var incomingPrompt = incoming.Prompt.Trim();
        var incomingResponseMode = string.IsNullOrWhiteSpace(incoming.ResponseMode) ? null : incoming.ResponseMode.Trim();
        var incomingResponseType = ResolveResponseType(incoming.ResponseType, incoming.ResponseMode, templateResponseMode);
        var incomingOptions = NormalizeResponseOptions(incoming.ResponseOptions);
        var existingOptions = NormalizeResponseOptions(DeserializeResponseOptions(existing.ResponseOptionsJson));
        var incomingHelpText = string.IsNullOrWhiteSpace(incoming.HelpText) ? null : incoming.HelpText.Trim();

        return
            existing.SortOrder == incoming.SortOrder &&
            string.Equals(existing.Prompt, incomingPrompt, StringComparison.Ordinal) &&
            existing.IsRequired == incoming.IsRequired &&
            string.Equals(existing.ResponseMode, incomingResponseMode, StringComparison.Ordinal) &&
            string.Equals(existing.ResponseType, incomingResponseType, StringComparison.Ordinal) &&
            string.Equals(existing.HelpText, incomingHelpText, StringComparison.Ordinal) &&
            existing.RequireFailNote == incoming.RequireFailNote &&
            existingOptions.SequenceEqual(incomingOptions);
    }

    private static string NormalizeResponse(string response) =>
        response.Equals("N/A", StringComparison.OrdinalIgnoreCase) ? "N/A" :
        response.Equals("Pass", StringComparison.OrdinalIgnoreCase) ? "Pass" :
        "Fail";

    private static void ValidateResponseForMode(string response, string mode)
    {
        if (mode == ChecklistResponseModes.PassFailNa)
        {
            return;
        }

        if (mode == ChecklistResponseModes.PassFail && response == "N/A")
        {
            throw new InvalidOperationException("N/A is not allowed for this checklist item.");
        }
    }

    private static ChecklistTemplateDto MapTemplate(ChecklistTemplate template) =>
        new()
        {
            Id = template.Id,
            TemplateCode = template.TemplateCode,
            Title = template.Title,
            ChecklistType = template.ChecklistType,
            ScopeLevel = template.ScopeLevel,
            SiteId = template.SiteId,
            WorkCenterId = template.WorkCenterId,
            ProductionLineId = template.ProductionLineId,
            VersionNo = template.VersionNo,
            EffectiveFromUtc = DateTime.SpecifyKind(template.EffectiveFromUtc, DateTimeKind.Utc),
            EffectiveToUtc = template.EffectiveToUtc.HasValue ? DateTime.SpecifyKind(template.EffectiveToUtc.Value, DateTimeKind.Utc) : null,
            IsActive = template.IsActive,
            ResponseMode = template.ResponseMode,
            RequireFailNote = template.RequireFailNote,
            IsSafetyProfile = template.IsSafetyProfile,
            Items = template.Items
                .OrderBy(i => i.SortOrder)
                .Select(i => new ChecklistTemplateItemDto
                {
                    Id = i.Id,
                    SortOrder = i.SortOrder,
                    Prompt = i.Prompt,
                    IsRequired = i.IsRequired,
                    ResponseMode = i.ResponseMode,
                    ResponseType = ResolveResponseType(i.ResponseType, i.ResponseMode, template.ResponseMode),
                    ResponseOptions = DeserializeResponseOptions(i.ResponseOptionsJson),
                    HelpText = i.HelpText,
                    RequireFailNote = i.RequireFailNote
                })
                .ToList()
        };

    private static ChecklistEntryDto MapEntry(ChecklistEntry entry) =>
        new()
        {
            Id = entry.Id,
            ChecklistTemplateId = entry.ChecklistTemplateId,
            ChecklistType = entry.ChecklistType,
            SiteId = entry.SiteId,
            WorkCenterId = entry.WorkCenterId,
            ProductionLineId = entry.ProductionLineId,
            OperatorUserId = entry.OperatorUserId,
            Status = entry.Status,
            StartedAtUtc = DateTime.SpecifyKind(entry.StartedAtUtc, DateTimeKind.Utc),
            CompletedAtUtc = entry.CompletedAtUtc.HasValue ? DateTime.SpecifyKind(entry.CompletedAtUtc.Value, DateTimeKind.Utc) : null,
            ResolvedFromScope = entry.ResolvedFromScope,
            ResolvedTemplateCode = entry.ResolvedTemplateCode,
            ResolvedTemplateVersionNo = entry.ResolvedTemplateVersionNo,
            Responses = entry.Responses
                .OrderBy(r => r.RespondedAtUtc)
                .Select(r => new ChecklistResponseDto
                {
                    Id = r.Id,
                    ChecklistTemplateItemId = r.ChecklistTemplateItemId,
                    ResponseValue = r.ResponseValue,
                    Note = r.Note
                })
                .ToList()
        };
}
