using Microsoft.EntityFrameworkCore;
using System.Globalization;
using System.Text.Json;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class ChecklistService : IChecklistService
{
    private static readonly HashSet<string> AllowedChecklistTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "SafetyPreShift",
        "SafetyPeriodic",
        "OpsPreShift",
        "OpsChangeover"
    };

    private static readonly HashSet<string> AllowedQuestionResponseTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        ChecklistQuestionResponseTypes.Checkbox,
        ChecklistQuestionResponseTypes.Datetime,
        ChecklistQuestionResponseTypes.Number,
        ChecklistQuestionResponseTypes.Image,
        ChecklistQuestionResponseTypes.Dimension,
        ChecklistQuestionResponseTypes.Score
    };

    private readonly MesDbContext _db;

    public ChecklistService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<ScoreTypeDto>> GetScoreTypesAsync(bool includeArchived, CancellationToken ct = default)
    {
        var query = _db.ScoreTypes
            .Include(s => s.Values.OrderBy(v => v.SortOrder))
            .AsQueryable();

        if (!includeArchived)
        {
            query = query.Where(s => s.IsActive);
        }

        var rows = await query
            .OrderBy(s => s.Name)
            .ToListAsync(ct);

        return rows.Select(MapScoreType).ToList();
    }

    public async Task<ScoreTypeDto?> GetScoreTypeAsync(Guid scoreTypeId, CancellationToken ct = default)
    {
        var item = await _db.ScoreTypes
            .Include(s => s.Values.OrderBy(v => v.SortOrder))
            .FirstOrDefaultAsync(s => s.Id == scoreTypeId, ct);
        return item == null ? null : MapScoreType(item);
    }

    public async Task<ScoreTypeDto> UpsertScoreTypeAsync(UpsertScoreTypeRequestDto request, Guid userId, decimal callerRoleTier, CancellationToken ct = default)
    {
        if (callerRoleTier > 2m)
        {
            throw new InvalidOperationException("Only administrator or director roles can manage score types.");
        }

        var name = request.Name?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException("Score type name is required.");
        }

        var normalizedValues = NormalizeScoreTypeValues(request.Values);
        if (normalizedValues.Count == 0)
        {
            throw new InvalidOperationException("At least one score value is required.");
        }

        var conflictExists = await _db.ScoreTypes
            .Where(s => s.Id != request.Id)
            .Where(s => s.IsActive)
            .AnyAsync(s => s.Name.ToLower() == name.ToLower(), ct);
        if (conflictExists)
        {
            throw new InvalidOperationException("An active score type with this name already exists.");
        }

        ScoreType scoreType;
        if (request.Id.HasValue)
        {
            scoreType = await _db.ScoreTypes
                .Include(s => s.Values)
                .FirstOrDefaultAsync(s => s.Id == request.Id.Value, ct)
                ?? throw new KeyNotFoundException("Score type not found.");
            scoreType.ModifiedByUserId = userId;
            scoreType.ModifiedAtUtc = DateTime.UtcNow;
        }
        else
        {
            scoreType = new ScoreType
            {
                Id = Guid.NewGuid(),
                CreatedByUserId = userId,
                CreatedAtUtc = DateTime.UtcNow
            };
            _db.ScoreTypes.Add(scoreType);
        }

        scoreType.Name = name;
        scoreType.IsActive = request.IsActive;

        var incomingById = normalizedValues.Where(v => v.Id.HasValue).ToDictionary(v => v.Id!.Value, v => v);
        var removeValues = scoreType.Values.Where(v => !incomingById.ContainsKey(v.Id)).ToList();
        foreach (var remove in removeValues)
        {
            scoreType.Values.Remove(remove);
        }

        foreach (var valueDto in normalizedValues)
        {
            ScoreTypeValue valueRow;
            if (valueDto.Id.HasValue)
            {
                valueRow = scoreType.Values.FirstOrDefault(v => v.Id == valueDto.Id.Value)
                    ?? new ScoreTypeValue { Id = valueDto.Id.Value, ScoreTypeId = scoreType.Id };
                if (!scoreType.Values.Contains(valueRow))
                {
                    scoreType.Values.Add(valueRow);
                }
            }
            else
            {
                valueRow = new ScoreTypeValue { Id = Guid.NewGuid(), ScoreTypeId = scoreType.Id };
                scoreType.Values.Add(valueRow);
            }

            valueRow.Score = valueDto.Score;
            valueRow.Description = valueDto.Description;
            valueRow.SortOrder = valueDto.SortOrder;
        }

        await _db.SaveChangesAsync(ct);
        return await GetScoreTypeAsync(scoreType.Id, ct) ?? throw new InvalidOperationException("Score type save failed.");
    }

    public async Task<IReadOnlyList<ChecklistTemplateDto>> GetTemplatesAsync(Guid? siteId, string? checklistType, CancellationToken ct = default)
    {
        var query = _db.ChecklistTemplates
            .Include(t => t.Items.OrderBy(i => i.SortOrder)).ThenInclude(i => i.ScoreType).ThenInclude(s => s!.Values.OrderBy(v => v.SortOrder))
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
            .Include(t => t.Items.OrderBy(i => i.SortOrder)).ThenInclude(i => i.ScoreType).ThenInclude(s => s!.Values.OrderBy(v => v.SortOrder))
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
        var normalizedChecklistType = NormalizeChecklistType(request.ChecklistType);
        ValidateOwner(request.OwnerUserId);
        ValidateScope(request.ScopeLevel, request.SiteId, request.WorkCenterId);
        ValidateItems(request.Items);
        await ValidateReferencedScoreTypesAsync(request.Items, ct);
        await ValidateOwnerExistsAsync(request.OwnerUserId!.Value, ct);

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

            if (template.OwnerUserId != userId)
            {
                throw new InvalidOperationException("Only the template owner can edit this checklist template.");
            }
        }
        else
        {
            if (request.OwnerUserId!.Value != userId && callerRoleTier > 2m)
            {
                throw new InvalidOperationException("Only administrator or director roles can assign a different checklist owner.");
            }

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
        template.ChecklistType = normalizedChecklistType;
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
        template.OwnerUserId = request.OwnerUserId!.Value;

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
                var isUnchanged = IsSameItemDefinition(existing, itemDto);
                if (isUnchanged)
                {
                    continue;
                }

                if (referencedExistingIds.Contains(existing.Id))
                {
                    EnsureReferencedItemUnchanged(existing, itemDto);
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
            item.Section = string.IsNullOrWhiteSpace(itemDto.Section) ? null : itemDto.Section.Trim();
            item.ResponseMode = string.IsNullOrWhiteSpace(itemDto.ResponseMode) ? null : itemDto.ResponseMode.Trim();
            item.ResponseType = ResolveResponseType(itemDto.ResponseType);
            item.ResponseOptionsJson = SerializeResponseOptions(itemDto.ResponseOptions);
            item.ScoreTypeId = itemDto.ScoreTypeId;
            item.DimensionTarget = itemDto.DimensionTarget;
            item.DimensionUpperLimit = itemDto.DimensionUpperLimit;
            item.DimensionLowerLimit = itemDto.DimensionLowerLimit;
            item.DimensionUnitOfMeasure = string.IsNullOrWhiteSpace(itemDto.DimensionUnitOfMeasure) ? null : itemDto.DimensionUnitOfMeasure.Trim();
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
        var normalizedChecklistType = NormalizeChecklistType(request.ChecklistType);
        if (request.SiteId != callerSiteId)
        {
            throw new InvalidOperationException("Site access denied.");
        }

        var now = DateTime.UtcNow;
        var candidates = await _db.ChecklistTemplates
            .Include(t => t.Items.OrderBy(i => i.SortOrder))
            .ThenInclude(i => i.ScoreType)
            .ThenInclude(s => s!.Values.OrderBy(v => v.SortOrder))
            .Where(t =>
                t.IsActive &&
                t.ChecklistType == normalizedChecklistType &&
                t.EffectiveFromUtc <= now &&
                (t.EffectiveToUtc == null || t.EffectiveToUtc >= now))
            .ToListAsync(ct);

        var selected = ResolveByScope(candidates, request.SiteId, request.WorkCenterId, request.ProductionLineId);
        return selected == null ? null : MapTemplate(selected);
    }

    public async Task<ChecklistEntryDto> CreateEntryAsync(CreateChecklistEntryRequestDto request, Guid callerSiteId, decimal callerRoleTier, CancellationToken ct = default)
    {
        var normalizedChecklistType = NormalizeChecklistType(request.ChecklistType);
        if (request.SiteId != callerSiteId)
        {
            throw new InvalidOperationException("Site access denied.");
        }

        if (normalizedChecklistType.Equals("SafetyPeriodic", StringComparison.OrdinalIgnoreCase) && callerRoleTier > 4m)
        {
            throw new InvalidOperationException("Supervisor or above required for periodic safety audits.");
        }

        var resolved = await ResolveTemplateAsync(new ResolveChecklistTemplateRequestDto
        {
            ChecklistType = normalizedChecklistType,
            SiteId = request.SiteId,
            WorkCenterId = request.WorkCenterId,
            ProductionLineId = request.ProductionLineId
        }, callerSiteId, ct) ?? throw new InvalidOperationException("No active checklist template resolved for this context.");

        var entry = new ChecklistEntry
        {
            Id = Guid.NewGuid(),
            ChecklistTemplateId = resolved.Id,
            ChecklistType = normalizedChecklistType,
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
        var scoreTypeIds = entry.ChecklistTemplate.Items
            .Where(i => string.Equals(i.ResponseType, ChecklistQuestionResponseTypes.Score, StringComparison.OrdinalIgnoreCase) && i.ScoreTypeId.HasValue)
            .Select(i => i.ScoreTypeId!.Value)
            .Distinct()
            .ToList();
        var scoreOptionsByType = await _db.ScoreTypeValues
            .Where(v => scoreTypeIds.Contains(v.ScoreTypeId))
            .ToListAsync(ct);
        var scoreOptionsLookup = scoreOptionsByType
            .GroupBy(v => v.ScoreTypeId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var response in request.Responses)
        {
            if (!templateItemMap.ContainsKey(response.ChecklistTemplateItemId))
            {
                throw new InvalidOperationException("Response references an invalid template item.");
            }
        }

        foreach (var response in request.Responses)
        {
            var templateItem = templateItemMap[response.ChecklistTemplateItemId];
            var normalizedResponse = NormalizeExecutionResponse(templateItem, response.ResponseValue, scoreOptionsLookup);

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

    public async Task<ChecklistReviewSummaryDto> GetReviewSummaryAsync(
        Guid siteId,
        DateTime fromUtc,
        DateTime toUtc,
        string? checklistType,
        CancellationToken ct = default)
    {
        var normalizedChecklistType = string.IsNullOrWhiteSpace(checklistType) ? null : checklistType.Trim();
        var (from, to) = await ResolveSiteDateWindowToUtcAsync(siteId, fromUtc, toUtc, ct);

        var rows = await BuildReviewResponseRowsQuery(siteId, from, to, normalizedChecklistType)
            .ToListAsync(ct);

        var scoreTypeIds = rows
            .Where(r => string.Equals(r.ResponseType, ChecklistQuestionResponseTypes.Score, StringComparison.OrdinalIgnoreCase) && r.ScoreTypeId.HasValue)
            .Select(r => r.ScoreTypeId!.Value)
            .Distinct()
            .ToList();
        var scoreOptionsLookup = await GetScoreOptionsLookupAsync(scoreTypeIds, ct);

        var questionSummaries = rows
            .GroupBy(r => new { r.ChecklistTemplateItemId, r.Prompt, r.Section, r.ResponseType })
            .Select(g =>
            {
                var responseBuckets = g
                    .GroupBy(r => r.ResponseValue)
                    .Select(bg =>
                    {
                        var firstRow = bg.First();
                        return new ChecklistResponseBucketDto
                        {
                            Value = bg.Key,
                            Label = ResolveResponseLabel(firstRow.ResponseType, firstRow.ScoreTypeId, bg.Key, scoreOptionsLookup),
                            Count = bg.Count()
                        };
                    })
                    .OrderByDescending(b => b.Count)
                    .ThenBy(b => b.Label)
                    .ToList();

                return new ChecklistQuestionSummaryDto
                {
                    ChecklistTemplateItemId = g.Key.ChecklistTemplateItemId,
                    Prompt = g.Key.Prompt,
                    Section = g.Key.Section,
                    ResponseType = g.Key.ResponseType,
                    ResponseCount = g.Count(),
                    ResponseBuckets = responseBuckets
                };
            })
            .OrderBy(q => q.Section ?? string.Empty)
            .ThenBy(q => q.Prompt)
            .ToList();

        var checklistTypesFound = rows
            .Select(r => r.ChecklistType)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(v => v)
            .ToList();

        var checklistFiltersFound = rows
            .GroupBy(r => r.ChecklistType, StringComparer.OrdinalIgnoreCase)
            .Select(g =>
            {
                var checklistName = g
                    .Where(x => !string.IsNullOrWhiteSpace(x.ChecklistName))
                    .GroupBy(x => x.ChecklistName!, StringComparer.Ordinal)
                    .OrderByDescending(x => x.Count())
                    .Select(x => x.Key)
                    .FirstOrDefault();

                return new ChecklistFilterOptionDto
                {
                    ChecklistType = g.Key,
                    ChecklistName = checklistName ?? g.Key
                };
            })
            .OrderBy(v => v.ChecklistName)
            .ToList();

        var totalEntries = rows
            .Select(r => r.ChecklistEntryId)
            .Distinct()
            .Count();

        return new ChecklistReviewSummaryDto
        {
            SiteId = siteId,
            FromUtc = from,
            ToUtc = to,
            ChecklistType = normalizedChecklistType,
            TotalEntries = totalEntries,
            TotalResponses = rows.Count,
            ChecklistTypesFound = checklistTypesFound,
            ChecklistFiltersFound = checklistFiltersFound,
            Questions = questionSummaries
        };
    }

    public async Task<ChecklistQuestionResponsesDto> GetQuestionResponsesAsync(
        Guid siteId,
        DateTime fromUtc,
        DateTime toUtc,
        Guid checklistTemplateItemId,
        string? checklistType,
        CancellationToken ct = default)
    {
        var normalizedChecklistType = string.IsNullOrWhiteSpace(checklistType) ? null : checklistType.Trim();
        var (from, to) = await ResolveSiteDateWindowToUtcAsync(siteId, fromUtc, toUtc, ct);

        var rows = await BuildReviewResponseRowsQuery(siteId, from, to, normalizedChecklistType)
            .Where(r => r.ChecklistTemplateItemId == checklistTemplateItemId)
            .OrderByDescending(r => r.RespondedAtUtc)
            .ToListAsync(ct);

        if (rows.Count == 0)
        {
            var item = await _db.ChecklistTemplateItems
                .AsNoTracking()
                .Where(i => i.Id == checklistTemplateItemId)
                .Select(i => new { i.Id, i.Prompt, i.Section, i.ResponseType })
                .FirstOrDefaultAsync(ct);
            if (item == null)
            {
                throw new KeyNotFoundException("Checklist question not found.");
            }

            return new ChecklistQuestionResponsesDto
            {
                ChecklistTemplateItemId = item.Id,
                Prompt = item.Prompt,
                Section = item.Section,
                ResponseType = item.ResponseType,
                TotalResponses = 0,
                ResponseBuckets = [],
                Rows = []
            };
        }

        var scoreTypeIds = rows
            .Where(r => string.Equals(r.ResponseType, ChecklistQuestionResponseTypes.Score, StringComparison.OrdinalIgnoreCase) && r.ScoreTypeId.HasValue)
            .Select(r => r.ScoreTypeId!.Value)
            .Distinct()
            .ToList();
        var scoreOptionsLookup = await GetScoreOptionsLookupAsync(scoreTypeIds, ct);

        var responseBuckets = rows
            .GroupBy(r => r.ResponseValue)
            .Select(g =>
            {
                var first = g.First();
                return new ChecklistResponseBucketDto
                {
                    Value = g.Key,
                    Label = ResolveResponseLabel(first.ResponseType, first.ScoreTypeId, g.Key, scoreOptionsLookup),
                    Count = g.Count()
                };
            })
            .OrderByDescending(v => v.Count)
            .ThenBy(v => v.Label)
            .ToList();

        var firstRow = rows[0];
        return new ChecklistQuestionResponsesDto
        {
            ChecklistTemplateItemId = firstRow.ChecklistTemplateItemId,
            Prompt = firstRow.Prompt,
            Section = firstRow.Section,
            ResponseType = firstRow.ResponseType,
            TotalResponses = rows.Count,
            ResponseBuckets = responseBuckets,
            Rows = rows
                .Select(r => new ChecklistQuestionResponseRowDto
                {
                    ChecklistEntryId = r.ChecklistEntryId,
                    ChecklistType = r.ChecklistType,
                    OperatorUserId = r.OperatorUserId,
                    OperatorDisplayName = r.OperatorDisplayName,
                    RespondedAtUtc = r.RespondedAtUtc,
                    ResponseValue = r.ResponseValue,
                    ResponseLabel = ResolveResponseLabel(r.ResponseType, r.ScoreTypeId, r.ResponseValue, scoreOptionsLookup),
                    Note = r.Note
                })
                .ToList()
        };
    }

    private IQueryable<ReviewResponseRow> BuildReviewResponseRowsQuery(
        Guid siteId,
        DateTime fromUtc,
        DateTime toUtc,
        string? checklistType)
    {
        var query = _db.ChecklistEntryItemResponses
            .AsNoTracking()
            .Where(r =>
                r.ChecklistEntry.SiteId == siteId &&
                r.ChecklistEntry.CompletedAtUtc.HasValue &&
                r.ChecklistEntry.CompletedAtUtc.Value >= fromUtc &&
                r.ChecklistEntry.CompletedAtUtc.Value <= toUtc)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(checklistType))
        {
            query = query.Where(r => r.ChecklistEntry.ChecklistType == checklistType);
        }

        return query.Select(r => new ReviewResponseRow
        {
            ChecklistEntryId = r.ChecklistEntryId,
            ChecklistType = r.ChecklistEntry.ChecklistType,
            ChecklistName = r.ChecklistEntry.ChecklistTemplate.Title,
            OperatorUserId = r.ChecklistEntry.OperatorUserId,
            OperatorDisplayName = r.ChecklistEntry.OperatorUser.DisplayName,
            ChecklistTemplateItemId = r.ChecklistTemplateItemId,
            Prompt = r.ChecklistTemplateItem.Prompt,
            Section = r.ChecklistTemplateItem.Section,
            ResponseType = r.ChecklistTemplateItem.ResponseType,
            ScoreTypeId = r.ChecklistTemplateItem.ScoreTypeId,
            ResponseValue = r.ResponseValue,
            Note = r.Note,
            RespondedAtUtc = r.RespondedAtUtc
        });
    }

    private async Task<Dictionary<Guid, Dictionary<Guid, ScoreTypeValue>>> GetScoreOptionsLookupAsync(
        IReadOnlyCollection<Guid> scoreTypeIds,
        CancellationToken ct)
    {
        if (scoreTypeIds.Count == 0)
        {
            return [];
        }

        var scoreOptions = await _db.ScoreTypeValues
            .AsNoTracking()
            .Where(v => scoreTypeIds.Contains(v.ScoreTypeId))
            .ToListAsync(ct);

        return scoreOptions
            .GroupBy(v => v.ScoreTypeId)
            .ToDictionary(
                g => g.Key,
                g => g.ToDictionary(v => v.Id, v => v));
    }

    private static string ResolveResponseLabel(
        string responseType,
        Guid? scoreTypeId,
        string rawValue,
        IReadOnlyDictionary<Guid, Dictionary<Guid, ScoreTypeValue>> scoreOptionsLookup)
    {
        if (string.Equals(responseType, ChecklistQuestionResponseTypes.Checkbox, StringComparison.OrdinalIgnoreCase))
        {
            if (bool.TryParse(rawValue, out var boolValue))
            {
                return boolValue ? "Pass" : "Fail";
            }

            return rawValue;
        }

        if (string.Equals(responseType, ChecklistQuestionResponseTypes.Score, StringComparison.OrdinalIgnoreCase)
            && scoreTypeId.HasValue
            && Guid.TryParse(rawValue, out var scoreValueId)
            && scoreOptionsLookup.TryGetValue(scoreTypeId.Value, out var valuesById)
            && valuesById.TryGetValue(scoreValueId, out var scoreValue))
        {
            return $"{scoreValue.Score.ToString(CultureInfo.InvariantCulture)} - {scoreValue.Description}";
        }

        return rawValue;
    }

    private static void ValidateReviewWindow(DateTime fromUtc, DateTime toUtc)
    {
        if (fromUtc > toUtc)
        {
            throw new InvalidOperationException("FromUtc must be less than or equal to ToUtc.");
        }

        if ((toUtc - fromUtc).TotalDays > 180)
        {
            throw new InvalidOperationException("Date range cannot exceed 180 days.");
        }
    }

    private async Task<(DateTime FromUtc, DateTime ToUtc)> ResolveSiteDateWindowToUtcAsync(
        Guid siteId,
        DateTime fromUtc,
        DateTime toUtc,
        CancellationToken ct)
    {
        var fromDate = DateOnly.FromDateTime(fromUtc);
        var toDate = DateOnly.FromDateTime(toUtc);
        if (fromDate > toDate)
        {
            throw new InvalidOperationException("FromUtc must be less than or equal to ToUtc.");
        }
        if (toDate.DayNumber - fromDate.DayNumber > 180)
        {
            throw new InvalidOperationException("Date range cannot exceed 180 days.");
        }

        var timeZoneId = await _db.Plants
            .AsNoTracking()
            .Where(p => p.Id == siteId)
            .Select(p => p.TimeZoneId)
            .FirstOrDefaultAsync(ct);

        var siteTimeZone = ResolveTimeZoneOrUtc(timeZoneId);
        var localStart = new DateTime(fromDate.Year, fromDate.Month, fromDate.Day, 0, 0, 0, DateTimeKind.Unspecified);
        var localEnd = new DateTime(toDate.Year, toDate.Month, toDate.Day, 23, 59, 59, 999, DateTimeKind.Unspecified);
        var resolvedFromUtc = TimeZoneInfo.ConvertTimeToUtc(localStart, siteTimeZone);
        var resolvedToUtc = TimeZoneInfo.ConvertTimeToUtc(localEnd, siteTimeZone);
        ValidateReviewWindow(resolvedFromUtc, resolvedToUtc);
        return (resolvedFromUtc, resolvedToUtc);
    }

    private static TimeZoneInfo ResolveTimeZoneOrUtc(string? timeZoneId)
    {
        if (!string.IsNullOrWhiteSpace(timeZoneId))
        {
            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
            }
            catch (InvalidTimeZoneException)
            {
            }
        }

        return TimeZoneInfo.Utc;
    }

    private sealed class ReviewResponseRow
    {
        public Guid ChecklistEntryId { get; init; }
        public string ChecklistType { get; init; } = string.Empty;
        public string? ChecklistName { get; init; }
        public Guid OperatorUserId { get; init; }
        public string OperatorDisplayName { get; init; } = string.Empty;
        public Guid ChecklistTemplateItemId { get; init; }
        public string Prompt { get; init; } = string.Empty;
        public string? Section { get; init; }
        public string ResponseType { get; init; } = string.Empty;
        public Guid? ScoreTypeId { get; init; }
        public string ResponseValue { get; init; } = string.Empty;
        public string? Note { get; init; }
        public DateTime RespondedAtUtc { get; init; }
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

    private static void ValidateOwner(Guid? ownerUserId)
    {
        if (!ownerUserId.HasValue || ownerUserId.Value == Guid.Empty)
        {
            throw new InvalidOperationException("Checklist template owner is required.");
        }
    }

    private async Task ValidateOwnerExistsAsync(Guid ownerUserId, CancellationToken ct)
    {
        var ownerExists = await _db.Users.AnyAsync(u => u.Id == ownerUserId, ct);
        if (!ownerExists)
        {
            throw new InvalidOperationException("Checklist template owner must reference an existing user.");
        }
    }

    private static void ValidateItems(IReadOnlyList<ChecklistTemplateItemDto> items)
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

            var responseType = ResolveResponseType(item.ResponseType);
            if (!AllowedQuestionResponseTypes.Contains(responseType))
            {
                throw new InvalidOperationException("Unsupported checklist response type.");
            }

            var responseOptions = NormalizeResponseOptions(item.ResponseOptions);
            if (responseOptions.Count > 0)
            {
                throw new InvalidOperationException("Checklist response options are not supported for the current response type contract.");
            }

            if (responseType == ChecklistQuestionResponseTypes.Score && !item.ScoreTypeId.HasValue)
            {
                throw new InvalidOperationException("Score checklist questions require a score type.");
            }

            if (responseType != ChecklistQuestionResponseTypes.Score && item.ScoreTypeId.HasValue)
            {
                throw new InvalidOperationException("Score type can only be set for Score response type questions.");
            }

            var isDimension = responseType == ChecklistQuestionResponseTypes.Dimension;
            if (isDimension)
            {
                if (!item.DimensionTarget.HasValue ||
                    !item.DimensionUpperLimit.HasValue ||
                    !item.DimensionLowerLimit.HasValue ||
                    string.IsNullOrWhiteSpace(item.DimensionUnitOfMeasure))
                {
                    throw new InvalidOperationException("Dimension checklist questions require target, upper/lower limits, and unit of measure.");
                }

                if (!string.Equals(item.DimensionUnitOfMeasure.Trim(), "inches", StringComparison.OrdinalIgnoreCase))
                {
                    throw new InvalidOperationException("Dimension unit of measure must be inches.");
                }

                if (!(item.DimensionLowerLimit.Value <= item.DimensionTarget.Value && item.DimensionTarget.Value <= item.DimensionUpperLimit.Value))
                {
                    throw new InvalidOperationException("Dimension checklist question limits are invalid.");
                }
            }
            else if (item.DimensionTarget.HasValue || item.DimensionUpperLimit.HasValue || item.DimensionLowerLimit.HasValue || !string.IsNullOrWhiteSpace(item.DimensionUnitOfMeasure))
            {
                throw new InvalidOperationException("Dimension metadata can only be set for Dimension response type questions.");
            }
        }
    }

    private async Task ValidateReferencedScoreTypesAsync(IReadOnlyList<ChecklistTemplateItemDto> items, CancellationToken ct)
    {
        var scoreTypeIds = items
            .Where(i => string.Equals(ResolveResponseType(i.ResponseType), ChecklistQuestionResponseTypes.Score, StringComparison.OrdinalIgnoreCase) && i.ScoreTypeId.HasValue)
            .Select(i => i.ScoreTypeId!.Value)
            .Distinct()
            .ToList();
        if (scoreTypeIds.Count == 0)
        {
            return;
        }

        var activeScoreTypeIds = await _db.ScoreTypes
            .Where(s => s.IsActive && scoreTypeIds.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(ct);

        if (activeScoreTypeIds.Count != scoreTypeIds.Count)
        {
            throw new InvalidOperationException("One or more referenced score types are missing or archived.");
        }
    }

    private static string ResolveResponseType(string? explicitResponseType)
    {
        if (string.IsNullOrWhiteSpace(explicitResponseType))
        {
            return ChecklistQuestionResponseTypes.Checkbox;
        }

        var normalizedType = explicitResponseType.Trim();
        if (!AllowedQuestionResponseTypes.Contains(normalizedType))
        {
            throw new InvalidOperationException("Unsupported checklist response type.");
        }

        return normalizedType;
    }

    private static string NormalizeChecklistType(string? checklistType)
    {
        if (string.IsNullOrWhiteSpace(checklistType))
        {
            throw new InvalidOperationException("Checklist type is required.");
        }

        var normalized = checklistType.Trim();
        if (!AllowedChecklistTypes.Contains(normalized))
        {
            throw new InvalidOperationException("Unsupported checklist type.");
        }

        if (normalized.Equals("SafetyPreShift", StringComparison.OrdinalIgnoreCase)) return "SafetyPreShift";
        if (normalized.Equals("SafetyPeriodic", StringComparison.OrdinalIgnoreCase)) return "SafetyPeriodic";
        if (normalized.Equals("OpsPreShift", StringComparison.OrdinalIgnoreCase)) return "OpsPreShift";
        return "OpsChangeover";
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

    private static void EnsureReferencedItemUnchanged(ChecklistTemplateItem existing, ChecklistTemplateItemDto incoming)
    {
        if (!IsSameItemDefinition(existing, incoming))
        {
            throw new InvalidOperationException("Checklist questions with captured responses cannot be edited. Create a new template version to change answered questions.");
        }
    }

    private static bool IsSameItemDefinition(ChecklistTemplateItem existing, ChecklistTemplateItemDto incoming)
    {
        var incomingPrompt = incoming.Prompt.Trim();
        var incomingSection = string.IsNullOrWhiteSpace(incoming.Section) ? null : incoming.Section.Trim();
        var incomingResponseMode = string.IsNullOrWhiteSpace(incoming.ResponseMode) ? null : incoming.ResponseMode.Trim();
        var incomingResponseType = ResolveResponseType(incoming.ResponseType);
        var incomingOptions = NormalizeResponseOptions(incoming.ResponseOptions);
        var existingOptions = NormalizeResponseOptions(DeserializeResponseOptions(existing.ResponseOptionsJson));
        var incomingUnit = string.IsNullOrWhiteSpace(incoming.DimensionUnitOfMeasure) ? null : incoming.DimensionUnitOfMeasure.Trim();
        var incomingHelpText = string.IsNullOrWhiteSpace(incoming.HelpText) ? null : incoming.HelpText.Trim();

        return
            existing.SortOrder == incoming.SortOrder &&
            string.Equals(existing.Prompt, incomingPrompt, StringComparison.Ordinal) &&
            existing.IsRequired == incoming.IsRequired &&
            string.Equals(existing.Section, incomingSection, StringComparison.Ordinal) &&
            string.Equals(existing.ResponseMode, incomingResponseMode, StringComparison.Ordinal) &&
            string.Equals(existing.ResponseType, incomingResponseType, StringComparison.Ordinal) &&
            existing.ScoreTypeId == incoming.ScoreTypeId &&
            existing.DimensionTarget == incoming.DimensionTarget &&
            existing.DimensionUpperLimit == incoming.DimensionUpperLimit &&
            existing.DimensionLowerLimit == incoming.DimensionLowerLimit &&
            string.Equals(existing.DimensionUnitOfMeasure, incomingUnit, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(existing.HelpText, incomingHelpText, StringComparison.Ordinal) &&
            existing.RequireFailNote == incoming.RequireFailNote &&
            existingOptions.SequenceEqual(incomingOptions);
    }

    private static string NormalizeExecutionResponse(
        ChecklistTemplateItem templateItem,
        string rawResponse,
        IReadOnlyDictionary<Guid, List<ScoreTypeValue>> scoreOptionsByType)
    {
        var response = rawResponse?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException("Checklist response value is required.");
        }

        if (string.Equals(templateItem.ResponseType, ChecklistQuestionResponseTypes.Checkbox, StringComparison.OrdinalIgnoreCase))
        {
            if (!bool.TryParse(response, out var boolValue))
            {
                throw new InvalidOperationException("Checkbox response must be true or false.");
            }

            return boolValue ? "true" : "false";
        }

        if (string.Equals(templateItem.ResponseType, ChecklistQuestionResponseTypes.Datetime, StringComparison.OrdinalIgnoreCase))
        {
            if (!DateTime.TryParse(response, null, DateTimeStyles.RoundtripKind, out var dateTime))
            {
                throw new InvalidOperationException("Datetime response is invalid.");
            }

            return dateTime.ToUniversalTime().ToString("O");
        }

        if (string.Equals(templateItem.ResponseType, ChecklistQuestionResponseTypes.Number, StringComparison.OrdinalIgnoreCase))
        {
            if (!decimal.TryParse(response, NumberStyles.Number, CultureInfo.InvariantCulture, out var numberValue))
            {
                throw new InvalidOperationException("Number response is invalid.");
            }

            return numberValue.ToString(CultureInfo.InvariantCulture);
        }

        if (string.Equals(templateItem.ResponseType, ChecklistQuestionResponseTypes.Dimension, StringComparison.OrdinalIgnoreCase))
        {
            if (!decimal.TryParse(response, NumberStyles.Number, CultureInfo.InvariantCulture, out var dimensionValue))
            {
                throw new InvalidOperationException("Dimension response must be numeric.");
            }

            return dimensionValue.ToString(CultureInfo.InvariantCulture);
        }

        if (string.Equals(templateItem.ResponseType, ChecklistQuestionResponseTypes.Image, StringComparison.OrdinalIgnoreCase))
        {
            return response;
        }

        if (string.Equals(templateItem.ResponseType, ChecklistQuestionResponseTypes.Score, StringComparison.OrdinalIgnoreCase))
        {
            if (!templateItem.ScoreTypeId.HasValue || !scoreOptionsByType.TryGetValue(templateItem.ScoreTypeId.Value, out var scoreValues) || scoreValues.Count == 0)
            {
                throw new InvalidOperationException("Score question has no configured score options.");
            }

            var scoreMatch = scoreValues.FirstOrDefault(v =>
                string.Equals(v.Id.ToString(), response, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v.Description, response, StringComparison.OrdinalIgnoreCase) ||
                string.Equals(v.Score.ToString(CultureInfo.InvariantCulture), response, StringComparison.OrdinalIgnoreCase));
            if (scoreMatch == null)
            {
                throw new InvalidOperationException("Score response is not a valid option for this question.");
            }

            return scoreMatch.Id.ToString();
        }

        throw new InvalidOperationException("Unsupported checklist response type.");
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
            OwnerUserId = template.OwnerUserId,
            Items = template.Items
                .OrderBy(i => i.SortOrder)
                .Select(i => new ChecklistTemplateItemDto
                {
                    Id = i.Id,
                    SortOrder = i.SortOrder,
                    Prompt = i.Prompt,
                    IsRequired = i.IsRequired,
                    Section = i.Section,
                    ResponseMode = i.ResponseMode,
                    ResponseType = ResolveResponseType(i.ResponseType),
                    ResponseOptions = DeserializeResponseOptions(i.ResponseOptionsJson),
                    ScoreTypeId = i.ScoreTypeId,
                    ScoreOptions = i.ScoreType?.Values
                        .OrderBy(v => v.SortOrder)
                        .Select(v => new ScoreTypeValueDto
                        {
                            Id = v.Id,
                            Score = v.Score,
                            Description = v.Description,
                            SortOrder = v.SortOrder
                        })
                        .ToList() ?? [],
                    DimensionTarget = i.DimensionTarget,
                    DimensionUpperLimit = i.DimensionUpperLimit,
                    DimensionLowerLimit = i.DimensionLowerLimit,
                    DimensionUnitOfMeasure = i.DimensionUnitOfMeasure,
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

    private static ScoreTypeDto MapScoreType(ScoreType scoreType) =>
        new()
        {
            Id = scoreType.Id,
            Name = scoreType.Name,
            IsActive = scoreType.IsActive,
            Values = scoreType.Values
                .OrderBy(v => v.SortOrder)
                .Select(v => new ScoreTypeValueDto
                {
                    Id = v.Id,
                    Score = v.Score,
                    Description = v.Description,
                    SortOrder = v.SortOrder
                })
                .ToList()
        };

    private static List<ScoreTypeValueDto> NormalizeScoreTypeValues(IEnumerable<ScoreTypeValueDto>? values)
    {
        var normalized = (values ?? [])
            .Select((v, index) => new ScoreTypeValueDto
            {
                Id = v.Id,
                Score = v.Score,
                Description = v.Description?.Trim() ?? string.Empty,
                SortOrder = v.SortOrder <= 0 ? index + 1 : v.SortOrder
            })
            .Where(v => !string.IsNullOrWhiteSpace(v.Description))
            .OrderBy(v => v.SortOrder)
            .ToList();

        var duplicatePair = normalized
            .GroupBy(v => $"{v.Score.ToString(CultureInfo.InvariantCulture)}|{v.Description.ToLowerInvariant()}")
            .FirstOrDefault(g => g.Count() > 1);
        if (duplicatePair != null)
        {
            throw new InvalidOperationException("Score values must be unique by score and description.");
        }

        for (var i = 0; i < normalized.Count; i++)
        {
            normalized[i].SortOrder = i + 1;
        }

        return normalized;
    }
}
