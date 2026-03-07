using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class WorkflowEngineService : IWorkflowEngineService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private static readonly HashSet<string> StepScopedNotificationEvents = new(StringComparer.Ordinal)
    {
        "StepEntered",
        "SubmittedForApproval",
        "Rejected",
    };
    private readonly MesDbContext _db;

    public WorkflowEngineService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WorkflowDefinitionDto>> GetDefinitionsAsync(string? workflowType, CancellationToken ct = default)
    {
        var query = _db.WorkflowDefinitions
            .Include(x => x.Steps)
            .AsQueryable();
        if (!string.IsNullOrWhiteSpace(workflowType))
            query = query.Where(x => x.WorkflowType == workflowType);

        var defs = await query
            .OrderBy(x => x.WorkflowType)
            .ThenByDescending(x => x.Version)
            .ToListAsync(ct);

        return defs.Select(MapDefinition).ToList();
    }

    public async Task<WorkflowDefinitionDto> UpsertDefinitionAsync(UpsertWorkflowDefinitionDto dto, Guid? actorUserId, CancellationToken ct = default)
    {
        ValidateDefinition(dto);

        var workflowType = dto.WorkflowType.Trim();
        var version = 1;
        if (dto.SourceDefinitionIdForNewVersion.HasValue)
        {
            var source = await _db.WorkflowDefinitions.FindAsync([dto.SourceDefinitionIdForNewVersion.Value], ct);
            if (source == null)
                throw new InvalidOperationException("Source definition not found.");
            workflowType = source.WorkflowType;
            version = source.Version + 1;
        }
        else
        {
            var maxVersion = await _db.WorkflowDefinitions
                .Where(x => x.WorkflowType == workflowType)
                .Select(x => (int?)x.Version)
                .MaxAsync(ct);
            version = (maxVersion ?? 0) + 1;
        }

        if (dto.IsActive)
        {
            var oldActives = await _db.WorkflowDefinitions
                .Where(x => x.WorkflowType == workflowType && x.IsActive)
                .ToListAsync(ct);
            foreach (var old in oldActives)
                old.IsActive = false;
        }

        var def = new WorkflowDefinition
        {
            Id = Guid.NewGuid(),
            WorkflowType = workflowType,
            Version = version,
            IsActive = dto.IsActive,
            StartStepCode = dto.StartStepCode,
            CreatedAtUtc = DateTime.UtcNow,
            CreatedByUserId = actorUserId,
            Steps = dto.Steps.Select(s => new WorkflowStepDefinition
            {
                Id = Guid.NewGuid(),
                StepCode = s.StepCode,
                StepName = s.StepName,
                Sequence = s.Sequence,
                RequiredFieldsJson = JsonSerializer.Serialize(s.RequiredFields, JsonOptions),
                RequiredChecklistTemplateIdsJson = JsonSerializer.Serialize(s.RequiredChecklistTemplateIds, JsonOptions),
                ApprovalMode = s.ApprovalMode,
                ApprovalAssignmentsJson = JsonSerializer.Serialize(s.ApprovalAssignments, JsonOptions),
                AllowReject = s.AllowReject,
                OnApproveNextStepCode = s.OnApproveNextStepCode,
                OnRejectTargetStepCode = s.OnRejectTargetStepCode
            }).ToList()
        };

        _db.WorkflowDefinitions.Add(def);
        await _db.SaveChangesAsync(ct);
        return MapDefinition(def);
    }

    public async Task<IReadOnlyList<NotificationRuleDto>> GetNotificationRulesAsync(string? workflowType, CancellationToken ct = default)
    {
        var q = _db.NotificationRules.AsQueryable();
        if (!string.IsNullOrWhiteSpace(workflowType))
            q = q.Where(x => x.WorkflowType == workflowType);
        return await q.OrderBy(x => x.WorkflowType).ThenBy(x => x.TriggerEvent)
            .Select(x => new NotificationRuleDto
            {
                Id = x.Id,
                WorkflowType = x.WorkflowType,
                TriggerEvent = x.TriggerEvent,
                TargetStepCodes = DeserializeList<string>(x.TargetStepCodesJson),
                RecipientMode = x.RecipientMode,
                RecipientConfigJson = x.RecipientConfigJson,
                TemplateKey = x.TemplateKey,
                TemplateTitle = x.TemplateTitle,
                TemplateBody = x.TemplateBody,
                ClearPolicy = x.ClearPolicy,
                IsActive = x.IsActive
            }).ToListAsync(ct);
    }

    public async Task<NotificationRuleDto> UpsertNotificationRuleAsync(NotificationRuleDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.WorkflowType))
            throw new InvalidOperationException("WorkflowType is required for notification rule.");
        if (string.IsNullOrWhiteSpace(dto.TriggerEvent))
            throw new InvalidOperationException("TriggerEvent is required for notification rule.");
        if (string.IsNullOrWhiteSpace(dto.RecipientMode))
            throw new InvalidOperationException("RecipientMode is required for notification rule.");
        if (string.IsNullOrWhiteSpace(dto.TemplateKey))
            throw new InvalidOperationException("TemplateKey is required for notification rule.");
        var normalizedTriggerEvent = dto.TriggerEvent.Trim();
        var requestedTargetStepCodes = dto.TargetStepCodes ?? new List<string>();
        if (StepScopedNotificationEvents.Contains(normalizedTriggerEvent) && requestedTargetStepCodes.Count == 0)
            throw new InvalidOperationException($"TargetStepCodes is required when TriggerEvent is {normalizedTriggerEvent}.");

        NotificationRule entity;
        if (dto.Id == Guid.Empty)
        {
            entity = new NotificationRule { Id = Guid.NewGuid() };
            _db.NotificationRules.Add(entity);
        }
        else
        {
            entity = await _db.NotificationRules.FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                ?? throw new InvalidOperationException("Notification rule not found.");
        }

        var normalizedTargetStepCodes = requestedTargetStepCodes
            .Select(code => code.Trim())
            .Where(code => !string.IsNullOrWhiteSpace(code))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedTargetStepCodes.Count > 0)
        {
            var latestDefinition = await _db.WorkflowDefinitions
                .Include(definition => definition.Steps)
                .Where(definition => definition.WorkflowType == dto.WorkflowType.Trim())
                .OrderByDescending(definition => definition.Version)
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Cannot validate TargetStepCodes because no workflow definition was found.");

            var knownStepCodes = latestDefinition.Steps
                .Select(step => step.StepCode)
                .ToHashSet(StringComparer.Ordinal);
            var unknownCodes = normalizedTargetStepCodes
                .Where(code => !knownStepCodes.Contains(code))
                .ToList();
            if (unknownCodes.Count > 0)
                throw new InvalidOperationException($"Unknown TargetStepCodes: {string.Join(", ", unknownCodes)}");
        }

        entity.WorkflowType = dto.WorkflowType.Trim();
        entity.TriggerEvent = normalizedTriggerEvent;
        entity.TargetStepCodesJson = JsonSerializer.Serialize(normalizedTargetStepCodes, JsonOptions);
        entity.RecipientMode = dto.RecipientMode.Trim();
        entity.RecipientConfigJson = string.IsNullOrWhiteSpace(dto.RecipientConfigJson) ? "[]" : dto.RecipientConfigJson;
        entity.TemplateKey = dto.TemplateKey.Trim();
        entity.TemplateTitle = dto.TemplateTitle?.Trim() ?? string.Empty;
        entity.TemplateBody = dto.TemplateBody?.Trim() ?? string.Empty;
        entity.ClearPolicy = string.IsNullOrWhiteSpace(dto.ClearPolicy) ? "None" : dto.ClearPolicy.Trim();
        entity.IsActive = dto.IsActive;
        await _db.SaveChangesAsync(ct);

        dto.Id = entity.Id;
        dto.TargetStepCodes = normalizedTargetStepCodes;
        return dto;
    }

    public async Task<WorkflowInstanceDto> StartWorkflowAsync(StartWorkflowRequestDto dto, CancellationToken ct = default)
    {
        var def = await _db.WorkflowDefinitions
            .Include(x => x.Steps)
            .Where(x => x.WorkflowType == dto.WorkflowType && x.IsActive)
            .OrderByDescending(x => x.Version)
            .FirstOrDefaultAsync(ct)
            ?? throw new InvalidOperationException("No active workflow definition found.");

        var startStepDef = def.Steps.FirstOrDefault(x => x.StepCode == def.StartStepCode)
            ?? throw new InvalidOperationException("Start step definition not found.");

        var instance = new WorkflowInstance
        {
            Id = Guid.NewGuid(),
            WorkflowDefinitionId = def.Id,
            WorkflowType = def.WorkflowType,
            WorkflowDefinitionVersion = def.Version,
            EntityType = dto.EntityType,
            EntityId = dto.EntityId,
            Status = startStepDef.ApprovalMode == "None" ? "InProgress" : "PendingApproval",
            CurrentStepCode = def.StartStepCode,
            StartedAtUtc = DateTime.UtcNow
        };

        var stepInstance = new WorkflowStepInstance
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            StepCode = def.StartStepCode,
            Status = startStepDef.ApprovalMode == "None" ? "InProgress" : "PendingApproval",
            StartedAtUtc = DateTime.UtcNow
        };

        _db.WorkflowInstances.Add(instance);
        _db.WorkflowStepInstances.Add(stepInstance);
        await AddEventAsync(instance.Id, "Created", null, new { dto.EntityType, dto.EntityId, def.WorkflowType, def.Version }, ct);
        await AddEventAsync(instance.Id, "StepEntered", null, new { StepCode = def.StartStepCode }, ct);
        await GenerateWorkItemsForStepEntryAsync(instance, stepInstance, startStepDef, ct);
        await _db.SaveChangesAsync(ct);
        return MapInstance(instance);
    }

    public async Task<WorkflowInstanceDto> AdvanceStepAsync(AdvanceStepRequestDto dto, CancellationToken ct = default)
    {
        return await ExecuteIdempotentAsync(dto.IdempotencyKey, "AdvanceStep", dto, async () =>
        {
            await using var tx = await BeginTransactionIfSupportedAsync(ct);
            var instance = await GetInstanceWithDefinitionAsync(dto.WorkflowInstanceId, ct);
            EnsureNonTerminal(instance);

            var currentStepDef = instance.WorkflowDefinition.Steps.FirstOrDefault(x => x.StepCode == instance.CurrentStepCode)
                ?? throw new InvalidOperationException("Current step definition not found.");
            var currentStepInstance = await _db.WorkflowStepInstances
                .Where(x => x.WorkflowInstanceId == instance.Id && x.StepCode == instance.CurrentStepCode)
                .OrderByDescending(x => x.StartedAtUtc)
                .FirstAsync(ct);

            currentStepInstance.Status = "Completed";
            currentStepInstance.EndedAtUtc = DateTime.UtcNow;
            currentStepInstance.CompletedByUserId = dto.ActorUserId;

            await AddEventAsync(instance.Id, "StepAdvanced", dto.ActorUserId, new
            {
                FromStepCode = instance.CurrentStepCode,
                ActionCode = dto.ActionCode,
                Payload = dto.PayloadJson
            }, ct);

            if (string.IsNullOrWhiteSpace(currentStepDef.OnApproveNextStepCode))
            {
                instance.Status = "Completed";
                instance.CompletedAtUtc = DateTime.UtcNow;
                await CancelOpenWorkItemsAsync(instance.Id, dto.ActorUserId, "Workflow completed", ct);
                await AddEventAsync(instance.Id, "Completed", dto.ActorUserId, new { }, ct);
            }
            else
            {
                instance.CurrentStepCode = currentStepDef.OnApproveNextStepCode!;
                instance.Status = "InProgress";
                await EnterStepAsync(instance, instance.CurrentStepCode, dto.ActorUserId, ct);
            }

            await _db.SaveChangesAsync(ct);
            if (tx != null)
                await tx.CommitAsync(ct);
            return MapInstance(instance);
        });
    }

    public async Task<WorkflowInstanceDto> ApproveStepAsync(ApproveRejectRequestDto dto, CancellationToken ct = default)
    {
        return await ExecuteIdempotentAsync(dto.IdempotencyKey, "ApproveStep", dto, async () =>
        {
            await using var tx = await BeginTransactionIfSupportedAsync(ct);
            var instance = await GetInstanceWithDefinitionAsync(dto.WorkflowInstanceId, ct);
            EnsureNonTerminal(instance);
            if (!string.Equals(instance.CurrentStepCode, dto.StepCode, StringComparison.Ordinal))
                throw new InvalidOperationException("Approve step must match current step.");

            var stepDef = instance.WorkflowDefinition.Steps.First(x => x.StepCode == instance.CurrentStepCode);
            if (stepDef.ApprovalMode == "None")
                throw new InvalidOperationException("Current step does not require approval.");
            var actor = await _db.Users
                .Where(x => x.Id == dto.ActorUserId && x.IsActive)
                .Select(x => new { x.Id, x.RoleTier })
                .FirstOrDefaultAsync(ct)
                ?? throw new InvalidOperationException("Actor user not found.");

            var currentStepInstance = await _db.WorkflowStepInstances
                .Where(x => x.WorkflowInstanceId == instance.Id && x.StepCode == instance.CurrentStepCode)
                .OrderByDescending(x => x.StartedAtUtc)
                .FirstAsync(ct);
            var approvalSlots = await _db.WorkflowStepApprovals
                .Where(x => x.WorkflowStepInstanceId == currentStepInstance.Id)
                .ToListAsync(ct);
            if (approvalSlots.Count == 0)
                throw new InvalidOperationException("No approval assignments configured for this step.");

            var alreadyApprovedByActor = approvalSlots.Any(x => x.ApprovedByUserId == actor.Id && x.Status == "Approved");
            if (alreadyApprovedByActor)
                throw new InvalidOperationException("Actor has already approved this step.");

            var matchingPendingSlots = approvalSlots
                .Where(x => x.Status == "Pending")
                .Where(x =>
                    (x.AssignmentType == "User" && x.AssignedUserId == actor.Id) ||
                    (x.AssignmentType == "Role" && x.AssignedRoleTier == actor.RoleTier))
                .ToList();
            if (matchingPendingSlots.Count == 0)
                throw new InvalidOperationException("Actor is not an eligible approver for this step.");

            var slotToApprove = matchingPendingSlots[0];
            slotToApprove.Status = "Approved";
            slotToApprove.ApprovedByUserId = actor.Id;
            slotToApprove.ApprovedAtUtc = DateTime.UtcNow;
            slotToApprove.Comments = dto.Comments;

            await AddEventAsync(instance.Id, "Approved", dto.ActorUserId, new
            {
                dto.StepCode,
                dto.Comments,
                stepDef.ApprovalMode,
                SlotId = slotToApprove.Id
            }, ct);

            var actorWorkItems = await _db.WorkItems
                .Where(x => x.WorkflowInstanceId == instance.Id && x.Status == "Open")
                .Where(x => x.AssignedUserId == actor.Id || x.AssignedRoleTier == actor.RoleTier)
                .ToListAsync(ct);
            foreach (var wi in actorWorkItems)
            {
                wi.Status = "Completed";
                wi.CompletedAtUtc = DateTime.UtcNow;
                wi.CompletedByUserId = actor.Id;
            }

            var pendingSlotsRemaining = approvalSlots.Any(x => x.Status == "Pending");
            if (stepDef.ApprovalMode == "All" && pendingSlotsRemaining)
            {
                currentStepInstance.Status = "PendingApproval";
                instance.Status = "PendingApproval";
                await AddEventAsync(instance.Id, "ApprovalPending", dto.ActorUserId, new
                {
                    dto.StepCode,
                    RemainingApprovals = approvalSlots.Count(x => x.Status == "Pending")
                }, ct);
                await _db.SaveChangesAsync(ct);
                if (tx != null)
                    await tx.CommitAsync(ct);
                return MapInstance(instance);
            }

            currentStepInstance.Status = "Approved";
            currentStepInstance.EndedAtUtc = DateTime.UtcNow;
            currentStepInstance.CompletedByUserId = dto.ActorUserId;
            currentStepInstance.Comments = dto.Comments;
            foreach (var pending in approvalSlots.Where(x => x.Status == "Pending"))
                pending.Status = "Cancelled";
            await CancelOpenWorkItemsAsync(instance.Id, dto.ActorUserId, "Approved", ct);
            if (string.IsNullOrWhiteSpace(stepDef.OnApproveNextStepCode))
            {
                instance.Status = "Completed";
                instance.CompletedAtUtc = DateTime.UtcNow;
                await AddEventAsync(instance.Id, "Completed", dto.ActorUserId, new { }, ct);
            }
            else
            {
                instance.CurrentStepCode = stepDef.OnApproveNextStepCode!;
                instance.Status = "InProgress";
                await EnterStepAsync(instance, instance.CurrentStepCode, dto.ActorUserId, ct);
            }

            await _db.SaveChangesAsync(ct);
            if (tx != null)
                await tx.CommitAsync(ct);
            return MapInstance(instance);
        });
    }

    public async Task<WorkflowInstanceDto> RejectStepAsync(ApproveRejectRequestDto dto, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Comments))
            throw new InvalidOperationException("Reject comments are required.");

        return await ExecuteIdempotentAsync(dto.IdempotencyKey, "RejectStep", dto, async () =>
        {
            await using var tx = await BeginTransactionIfSupportedAsync(ct);
            var instance = await GetInstanceWithDefinitionAsync(dto.WorkflowInstanceId, ct);
            EnsureNonTerminal(instance);
            if (!string.Equals(instance.CurrentStepCode, dto.StepCode, StringComparison.Ordinal))
                throw new InvalidOperationException("Reject step must match current step.");

            var stepDef = instance.WorkflowDefinition.Steps.First(x => x.StepCode == instance.CurrentStepCode);
            if (!stepDef.AllowReject)
                throw new InvalidOperationException("Reject is not allowed on this step.");

            var currentStepInstance = await _db.WorkflowStepInstances
                .Where(x => x.WorkflowInstanceId == instance.Id && x.StepCode == instance.CurrentStepCode)
                .OrderByDescending(x => x.StartedAtUtc)
                .FirstAsync(ct);
            currentStepInstance.Status = "Rejected";
            currentStepInstance.EndedAtUtc = DateTime.UtcNow;
            currentStepInstance.CompletedByUserId = dto.ActorUserId;
            currentStepInstance.Comments = dto.Comments;

            await CancelOpenWorkItemsAsync(instance.Id, dto.ActorUserId, "Rejected", ct);
            var approvals = await _db.WorkflowStepApprovals
                .Where(x => x.WorkflowStepInstanceId == currentStepInstance.Id && x.Status == "Pending")
                .ToListAsync(ct);
            foreach (var approval in approvals)
                approval.Status = "Cancelled";
            instance.Status = "Rejected";

            await AddEventAsync(instance.Id, "Rejected", dto.ActorUserId, new
            {
                dto.StepCode,
                dto.Comments,
                stepDef.OnRejectTargetStepCode
            }, ct);

            if (!string.IsNullOrWhiteSpace(stepDef.OnRejectTargetStepCode))
            {
                instance.CurrentStepCode = stepDef.OnRejectTargetStepCode!;
                await EnterStepAsync(instance, instance.CurrentStepCode, dto.ActorUserId, ct, preserveWorkflowStatus: true);
            }

            await _db.SaveChangesAsync(ct);
            if (tx != null)
                await tx.CommitAsync(ct);
            return MapInstance(instance);
        });
    }

    public async Task<IReadOnlyList<WorkItemDto>> GetOpenWorkItemsAsync(Guid userId, IReadOnlyList<decimal> roleTiers, CancellationToken ct = default)
    {
        return await _db.WorkItems
            .Where(x => x.Status == "Open")
            .Where(x => x.AssignedUserId == userId || (x.AssignedRoleTier.HasValue && roleTiers.Contains(x.AssignedRoleTier.Value)))
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new WorkItemDto
            {
                Id = x.Id,
                WorkflowInstanceId = x.WorkflowInstanceId,
                EntityType = x.EntityType,
                EntityId = x.EntityId,
                WorkItemType = x.WorkItemType,
                Title = x.Title,
                Instructions = x.Instructions,
                AssignedUserId = x.AssignedUserId,
                AssignedRoleTier = x.AssignedRoleTier,
                Status = x.Status,
                Priority = x.Priority,
                DueAtUtc = x.DueAtUtc,
                CreatedAtUtc = x.CreatedAtUtc
            })
            .ToListAsync(ct);
    }

    public async Task<WorkflowInstanceDto> CompleteWorkItemAsync(CompleteWorkItemRequestDto dto, CancellationToken ct = default)
    {
        return await ExecuteIdempotentAsync(dto.IdempotencyKey, "CompleteWorkItem", dto, async () =>
        {
            var wi = await _db.WorkItems.FirstOrDefaultAsync(x => x.Id == dto.WorkItemId, ct)
                ?? throw new InvalidOperationException("Work item not found.");
            if (wi.Status is "Completed" or "Cancelled")
                throw new InvalidOperationException("Work item is immutable.");

            wi.Status = "Completed";
            wi.CompletedAtUtc = DateTime.UtcNow;
            wi.CompletedByUserId = dto.ActorUserId;

            await AddEventAsync(wi.WorkflowInstanceId, "WorkItemCompleted", dto.ActorUserId, new
            {
                wi.Id,
                wi.WorkItemType,
                Payload = dto.PayloadJson
            }, ct);
            await _db.SaveChangesAsync(ct);

            var instance = await _db.WorkflowInstances.FirstAsync(x => x.Id == wi.WorkflowInstanceId, ct);
            return MapInstance(instance);
        });
    }

    public async Task<IReadOnlyList<WorkflowEventDto>> GetWorkflowEventsAsync(Guid workflowInstanceId, CancellationToken ct = default)
    {
        return await _db.WorkflowEvents
            .Where(x => x.WorkflowInstanceId == workflowInstanceId)
            .OrderBy(x => x.EventAtUtc)
            .Select(x => new WorkflowEventDto
            {
                Id = x.Id,
                EventType = x.EventType,
                EventAtUtc = x.EventAtUtc,
                ActorUserId = x.ActorUserId,
                PayloadJson = x.PayloadJson
            }).ToListAsync(ct);
    }

    public async Task CancelOpenWorkItemsAsync(Guid workflowInstanceId, Guid? actorUserId, string reason, CancellationToken ct = default)
    {
        var openItems = await _db.WorkItems
            .Where(x => x.WorkflowInstanceId == workflowInstanceId && (x.Status == "Open" || x.Status == "InProgress"))
            .ToListAsync(ct);
        foreach (var item in openItems)
            item.Status = "Cancelled";
        if (openItems.Count > 0)
            await AddEventAsync(workflowInstanceId, "WorkItemsCancelled", actorUserId, new { Count = openItems.Count, reason }, ct);
    }

    private async Task EnterStepAsync(WorkflowInstance instance, string stepCode, Guid? actorUserId, CancellationToken ct, bool preserveWorkflowStatus = false)
    {
        var stepDef = instance.WorkflowDefinition.Steps.FirstOrDefault(x => x.StepCode == stepCode)
            ?? throw new InvalidOperationException($"Target step {stepCode} does not exist.");

        var stepInstance = new WorkflowStepInstance
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instance.Id,
            StepCode = stepCode,
            Status = stepDef.ApprovalMode == "None" ? "InProgress" : "PendingApproval",
            StartedAtUtc = DateTime.UtcNow
        };
        _db.WorkflowStepInstances.Add(stepInstance);
        await AddEventAsync(instance.Id, "StepEntered", actorUserId, new { StepCode = stepCode }, ct);
        await GenerateWorkItemsForStepEntryAsync(instance, stepInstance, stepDef, ct);
        if (!preserveWorkflowStatus)
            instance.Status = stepDef.ApprovalMode == "None" ? "InProgress" : "PendingApproval";
    }

    private async Task GenerateWorkItemsForStepEntryAsync(WorkflowInstance instance, WorkflowStepInstance stepInstance, WorkflowStepDefinition stepDef, CancellationToken ct)
    {
        if (stepDef.ApprovalMode == "None")
            return;

        var assignments = DeserializeList<string>(stepDef.ApprovalAssignmentsJson);
        if (assignments.Count == 0)
            return;

        foreach (var assignment in assignments.Distinct())
        {
            Guid? assignedUserId = null;
            decimal? assignedRoleTier = null;
            if (assignment.StartsWith("user:", StringComparison.OrdinalIgnoreCase) &&
                Guid.TryParse(assignment["user:".Length..], out var uid))
            {
                assignedUserId = uid;
                _db.WorkflowStepApprovals.Add(new WorkflowStepApproval
                {
                    Id = Guid.NewGuid(),
                    WorkflowStepInstanceId = stepInstance.Id,
                    AssignmentType = "User",
                    AssignedUserId = uid,
                    Status = "Pending"
                });
            }
            else if (assignment.StartsWith("role:", StringComparison.OrdinalIgnoreCase) &&
                     decimal.TryParse(assignment["role:".Length..], out var roleTier))
            {
                assignedRoleTier = roleTier;
                _db.WorkflowStepApprovals.Add(new WorkflowStepApproval
                {
                    Id = Guid.NewGuid(),
                    WorkflowStepInstanceId = stepInstance.Id,
                    AssignmentType = "Role",
                    AssignedRoleTier = roleTier,
                    Status = "Pending"
                });
            }
            else
            {
                continue;
            }

            _db.WorkItems.Add(new WorkItem
            {
                Id = Guid.NewGuid(),
                WorkflowInstanceId = instance.Id,
                EntityType = instance.EntityType,
                EntityId = instance.EntityId,
                WorkItemType = "Approve",
                Title = $"Approve {instance.WorkflowType} at {stepDef.StepCode}",
                AssignedUserId = assignedUserId,
                AssignedRoleTier = assignedRoleTier,
                Status = "Open",
                Priority = "Normal",
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        await AddEventAsync(instance.Id, "SubmittedForApproval", null, new { StepCode = stepDef.StepCode, stepDef.ApprovalMode }, ct);
    }

    private async Task<WorkflowInstance> GetInstanceWithDefinitionAsync(Guid workflowInstanceId, CancellationToken ct)
    {
        return await _db.WorkflowInstances
            .Include(x => x.WorkflowDefinition)
            .ThenInclude(d => d.Steps)
            .FirstOrDefaultAsync(x => x.Id == workflowInstanceId, ct)
            ?? throw new InvalidOperationException("Workflow instance not found.");
    }

    private static void EnsureNonTerminal(WorkflowInstance instance)
    {
        if (instance.Status is "Completed" or "Voided")
            throw new InvalidOperationException("Terminal workflow instances are immutable.");
    }

    private async Task AddEventAsync(Guid instanceId, string eventType, Guid? actorUserId, object payload, CancellationToken ct)
    {
        _db.WorkflowEvents.Add(new WorkflowEvent
        {
            Id = Guid.NewGuid(),
            WorkflowInstanceId = instanceId,
            EventType = eventType,
            EventAtUtc = DateTime.UtcNow,
            ActorUserId = actorUserId,
            PayloadJson = JsonSerializer.Serialize(payload, JsonOptions),
            IsOutboxDispatched = false
        });
        await Task.CompletedTask;
    }

    private static void ValidateDefinition(UpsertWorkflowDefinitionDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.WorkflowType))
            throw new InvalidOperationException("WorkflowType is required.");
        if (string.IsNullOrWhiteSpace(dto.StartStepCode))
            throw new InvalidOperationException("StartStepCode is required.");
        if (dto.Steps.Count == 0)
            throw new InvalidOperationException("At least one step is required.");
        if (!dto.Steps.Any(x => x.StepCode == dto.StartStepCode))
            throw new InvalidOperationException("StartStepCode must exist in steps.");

        var duplicates = dto.Steps.GroupBy(x => x.StepCode).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicates.Count > 0)
            throw new InvalidOperationException($"Duplicate step codes found: {string.Join(", ", duplicates)}");
    }

    private static List<T> DeserializeList<T>(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<List<T>>(json, JsonOptions) ?? new List<T>();
        }
        catch
        {
            return new List<T>();
        }
    }

    private static WorkflowDefinitionDto MapDefinition(WorkflowDefinition x) => new()
    {
        Id = x.Id,
        WorkflowType = x.WorkflowType,
        Version = x.Version,
        IsActive = x.IsActive,
        StartStepCode = x.StartStepCode,
        Steps = x.Steps
            .OrderBy(s => s.Sequence)
            .Select(s => new WorkflowStepDefinitionDto
            {
                Id = s.Id,
                StepCode = s.StepCode,
                StepName = s.StepName,
                Sequence = s.Sequence,
                RequiredFields = DeserializeList<string>(s.RequiredFieldsJson),
                RequiredChecklistTemplateIds = DeserializeList<Guid>(s.RequiredChecklistTemplateIdsJson),
                ApprovalMode = s.ApprovalMode,
                ApprovalAssignments = DeserializeList<string>(s.ApprovalAssignmentsJson),
                AllowReject = s.AllowReject,
                OnApproveNextStepCode = s.OnApproveNextStepCode,
                OnRejectTargetStepCode = s.OnRejectTargetStepCode
            }).ToList()
    };

    private static WorkflowInstanceDto MapInstance(WorkflowInstance x) => new()
    {
        Id = x.Id,
        WorkflowDefinitionId = x.WorkflowDefinitionId,
        WorkflowType = x.WorkflowType,
        WorkflowDefinitionVersion = x.WorkflowDefinitionVersion,
        EntityType = x.EntityType,
        EntityId = x.EntityId,
        Status = x.Status,
        CurrentStepCode = x.CurrentStepCode,
        StartedAtUtc = x.StartedAtUtc,
        CompletedAtUtc = x.CompletedAtUtc
    };

    private static WorkItemDto MapWorkItem(WorkItem x) => new()
    {
        Id = x.Id,
        WorkflowInstanceId = x.WorkflowInstanceId,
        EntityType = x.EntityType,
        EntityId = x.EntityId,
        WorkItemType = x.WorkItemType,
        Title = x.Title,
        Instructions = x.Instructions,
        AssignedUserId = x.AssignedUserId,
        AssignedRoleTier = x.AssignedRoleTier,
        Status = x.Status,
        Priority = x.Priority,
        DueAtUtc = x.DueAtUtc,
        CreatedAtUtc = x.CreatedAtUtc
    };

    private async Task<T> ExecuteIdempotentAsync<T>(string? key, string actionName, object request, Func<Task<T>> action)
    {
        if (string.IsNullOrWhiteSpace(key))
            return await action();

        var requestJson = JsonSerializer.Serialize(request, JsonOptions);
        var requestHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(requestJson)));
        var existing = await _db.IdempotencyRecords.FirstOrDefaultAsync(x => x.Key == key);
        if (existing != null)
        {
            if (!string.Equals(existing.RequestHash, requestHash, StringComparison.Ordinal))
                throw new InvalidOperationException("Idempotency key reused with different payload.");
            return JsonSerializer.Deserialize<T>(existing.ResponseJson, JsonOptions)
                ?? throw new InvalidOperationException("Unable to restore idempotent response.");
        }

        var response = await action();
        _db.IdempotencyRecords.Add(new IdempotencyRecord
        {
            Id = Guid.NewGuid(),
            Key = key,
            ActionName = actionName,
            RequestHash = requestHash,
            ResponseJson = JsonSerializer.Serialize(response, JsonOptions),
            CreatedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();
        return response;
    }

    private async Task<IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken ct)
    {
        if (string.Equals(_db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
            return null;
        return await _db.Database.BeginTransactionAsync(ct);
    }
}
