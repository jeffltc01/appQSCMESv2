using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class NcrService : INcrService
{
    private readonly MesDbContext _db;
    private readonly IWorkflowEngineService _workflow;

    public NcrService(MesDbContext db, IWorkflowEngineService workflow)
    {
        _db = db;
        _workflow = workflow;
    }

    public async Task<IReadOnlyList<NcrTypeDto>> GetNcrTypesAsync(bool includeInactive, CancellationToken ct = default)
    {
        return await _db.NcrTypes
            .Where(x => includeInactive || x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new NcrTypeDto
            {
                Id = x.Id,
                Code = x.Code,
                Name = x.Name,
                IsActive = x.IsActive,
                IsVendorRelated = x.IsVendorRelated,
                Description = x.Description,
                WorkflowDefinitionId = x.WorkflowDefinitionId
            }).ToListAsync(ct);
    }

    public async Task<NcrTypeDto> UpsertNcrTypeAsync(UpsertNcrTypeRequestDto dto, CancellationToken ct = default)
    {
        var existsWorkflow = await _db.WorkflowDefinitions.AnyAsync(x => x.Id == dto.WorkflowDefinitionId, ct);
        if (!existsWorkflow)
            throw new InvalidOperationException("WorkflowDefinitionId is invalid.");

        NcrType entity;
        if (dto.Id.HasValue && dto.Id.Value != Guid.Empty)
        {
            entity = await _db.NcrTypes.FirstOrDefaultAsync(x => x.Id == dto.Id, ct)
                ?? throw new InvalidOperationException("NCR Type not found.");
        }
        else
        {
            entity = new NcrType { Id = Guid.NewGuid() };
            _db.NcrTypes.Add(entity);
        }

        entity.Code = dto.Code.Trim();
        entity.Name = dto.Name.Trim();
        entity.IsActive = dto.IsActive;
        entity.IsVendorRelated = dto.IsVendorRelated;
        entity.Description = dto.Description;
        entity.WorkflowDefinitionId = dto.WorkflowDefinitionId;
        await _db.SaveChangesAsync(ct);

        return new NcrTypeDto
        {
            Id = entity.Id,
            Code = entity.Code,
            Name = entity.Name,
            IsActive = entity.IsActive,
            IsVendorRelated = entity.IsVendorRelated,
            Description = entity.Description,
            WorkflowDefinitionId = entity.WorkflowDefinitionId
        };
    }

    public async Task<IReadOnlyList<NcrDto>> GetNcrsAsync(string? siteCode, CancellationToken ct = default)
    {
        var q = _db.Ncrs.AsQueryable();
        if (!string.IsNullOrWhiteSpace(siteCode))
            q = q.Where(x => x.SiteCode == siteCode);
        return await q.OrderByDescending(x => x.CreatedAtUtc).Select(MapProjection).ToListAsync(ct);
    }

    public async Task<NcrDto?> GetNcrByIdAsync(Guid id, CancellationToken ct = default)
    {
        return await _db.Ncrs.Where(x => x.Id == id).Select(MapProjection).FirstOrDefaultAsync(ct);
    }

    public async Task<NcrDto> CreateNcrAsync(CreateNcrRequestDto dto, CancellationToken ct = default)
    {
        var user = await RequireQualityAsync(dto.CreatedByUserId, ct);
        var ncrType = await _db.NcrTypes.FirstOrDefaultAsync(x => x.Id == dto.NcrTypeId && x.IsActive, ct)
            ?? throw new InvalidOperationException("NcrTypeId is invalid.");
        if (dto.SourceType == "HoldTagEscalation" && !dto.SourceEntityId.HasValue)
            throw new InvalidOperationException("SourceEntityId is required for HoldTagEscalation.");
        if (ncrType.IsVendorRelated)
            ValidateVendorFields(dto.VendorId, dto.PoNumber, dto.Quantity, dto.HeatNumber, dto.CoilOrSlabNumber);

        await using var tx = await BeginTransactionIfSupportedAsync(ct);
        var ncrNumber = await GetNextSequenceValueAsync("NcrNumber", ct);
        var wfDef = await _db.WorkflowDefinitions.FirstOrDefaultAsync(x => x.Id == ncrType.WorkflowDefinitionId, ct)
            ?? throw new InvalidOperationException("Mapped workflow definition not found.");

        var ncr = new Ncr
        {
            Id = Guid.NewGuid(),
            NcrNumber = ncrNumber,
            SourceType = dto.SourceType,
            SourceEntityId = dto.SourceEntityId,
            SiteCode = dto.SiteCode,
            DetectedByUserId = dto.DetectedByUserId,
            SubmitterUserId = dto.SubmitterUserId,
            CoordinatorUserId = dto.CoordinatorUserId,
            NcrTypeId = dto.NcrTypeId,
            DateUtc = dto.DateUtc,
            ProblemDescription = dto.ProblemDescription,
            CreatedByUserId = user.Id,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedByUserId = user.Id,
            LastModifiedAtUtc = DateTime.UtcNow,
            CurrentStepCode = wfDef.StartStepCode,
            VendorId = dto.VendorId,
            PoNumber = dto.PoNumber,
            Quantity = dto.Quantity,
            HeatNumber = dto.HeatNumber,
            CoilOrSlabNumber = dto.CoilOrSlabNumber
        };
        _db.Ncrs.Add(ncr);
        await _db.SaveChangesAsync(ct);

        var wf = await _workflow.StartWorkflowAsync(new StartWorkflowRequestDto
        {
            EntityType = "Ncr",
            EntityId = ncr.Id,
            WorkflowType = "Ncr"
        }, ct);
        ncr.WorkflowInstanceId = wf.Id;
        ncr.CurrentStepCode = wf.CurrentStepCode;

        if (dto.SourceType == "HoldTagEscalation" && dto.SourceEntityId.HasValue)
        {
            var holdTag = await _db.HoldTags.FirstOrDefaultAsync(x => x.Id == dto.SourceEntityId.Value, ct);
            if (holdTag != null)
                holdTag.LinkedNcrId = ncr.Id;
        }

        await _db.SaveChangesAsync(ct);
        if (tx != null)
            await tx.CommitAsync(ct);
        return await GetNcrByIdAsync(ncr.Id, ct) ?? throw new InvalidOperationException("NCR create failed.");
    }

    public async Task<NcrDto> UpdateNcrDataAsync(UpdateNcrDataRequestDto dto, CancellationToken ct = default)
    {
        var actor = await RequireQualityAsync(dto.ActorUserId, ct);
        var ncr = await _db.Ncrs.Include(x => x.NcrType).FirstOrDefaultAsync(x => x.Id == dto.NcrId, ct)
            ?? throw new InvalidOperationException("NCR not found.");
        await EnsureWorkflowIsMutableAsync(ncr.WorkflowInstanceId, ct);

        if (ncr.NcrType.IsVendorRelated)
            ValidateVendorFields(dto.VendorId, dto.PoNumber, dto.Quantity, dto.HeatNumber, dto.CoilOrSlabNumber);

        ncr.CoordinatorUserId = dto.CoordinatorUserId;
        ncr.ProblemDescription = dto.ProblemDescription;
        ncr.VendorId = dto.VendorId;
        ncr.PoNumber = dto.PoNumber;
        ncr.Quantity = dto.Quantity;
        ncr.HeatNumber = dto.HeatNumber;
        ncr.CoilOrSlabNumber = dto.CoilOrSlabNumber;
        ncr.LastModifiedByUserId = actor.Id;
        ncr.LastModifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(ncr);
    }

    public async Task<NcrDto> SubmitNcrStepAsync(SubmitNcrStepRequestDto dto, CancellationToken ct = default)
    {
        var ncr = await _db.Ncrs.FirstOrDefaultAsync(x => x.Id == dto.NcrId, ct)
            ?? throw new InvalidOperationException("NCR not found.");

        await EnsureAttachmentBeforeApprovalGateAsync(ncr, ct);

        var wf = await _workflow.AdvanceStepAsync(new AdvanceStepRequestDto
        {
            WorkflowInstanceId = ncr.WorkflowInstanceId,
            ActionCode = dto.ActionCode,
            ActorUserId = dto.ActorUserId,
            PayloadJson = dto.PayloadJson
        }, ct);
        ncr.CurrentStepCode = wf.CurrentStepCode;
        ncr.LastModifiedByUserId = dto.ActorUserId;
        ncr.LastModifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(ncr);
    }

    public async Task<NcrDto> ApproveNcrStepAsync(NcrDecisionRequestDto dto, CancellationToken ct = default)
    {
        _ = await RequireQualityAsync(dto.ActorUserId, ct);
        var ncr = await _db.Ncrs.FirstOrDefaultAsync(x => x.Id == dto.NcrId, ct)
            ?? throw new InvalidOperationException("NCR not found.");
        await EnsureAttachmentBeforeApprovalGateAsync(ncr, ct);

        var wf = await _workflow.ApproveStepAsync(new ApproveRejectRequestDto
        {
            WorkflowInstanceId = ncr.WorkflowInstanceId,
            StepCode = dto.StepCode,
            ActorUserId = dto.ActorUserId,
            Comments = dto.Comments
        }, ct);
        ncr.CurrentStepCode = wf.CurrentStepCode;
        ncr.LastModifiedByUserId = dto.ActorUserId;
        ncr.LastModifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(ncr);
    }

    public async Task<NcrDto> RejectNcrStepAsync(NcrDecisionRequestDto dto, CancellationToken ct = default)
    {
        _ = await RequireQualityAsync(dto.ActorUserId, ct);
        if (string.IsNullOrWhiteSpace(dto.Comments))
            throw new InvalidOperationException("Reject comments are required.");
        var ncr = await _db.Ncrs.FirstOrDefaultAsync(x => x.Id == dto.NcrId, ct)
            ?? throw new InvalidOperationException("NCR not found.");

        var wf = await _workflow.RejectStepAsync(new ApproveRejectRequestDto
        {
            WorkflowInstanceId = ncr.WorkflowInstanceId,
            StepCode = dto.StepCode,
            ActorUserId = dto.ActorUserId,
            Comments = dto.Comments
        }, ct);
        ncr.CurrentStepCode = wf.CurrentStepCode;
        ncr.LastModifiedByUserId = dto.ActorUserId;
        ncr.LastModifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(ncr);
    }

    public async Task<NcrDto> VoidNcrAsync(VoidNcrRequestDto dto, CancellationToken ct = default)
    {
        _ = await RequireQualityAsync(dto.ActorUserId, ct);
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Void reason is required.");
        var ncr = await _db.Ncrs.FirstOrDefaultAsync(x => x.Id == dto.NcrId, ct)
            ?? throw new InvalidOperationException("NCR not found.");

        var stepCode = await _db.WorkflowInstances
            .Where(x => x.Id == ncr.WorkflowInstanceId)
            .Select(x => x.CurrentStepCode)
            .FirstAsync(ct);
        await _workflow.RejectStepAsync(new ApproveRejectRequestDto
        {
            WorkflowInstanceId = ncr.WorkflowInstanceId,
            StepCode = stepCode,
            ActorUserId = dto.ActorUserId,
            Comments = $"VoidNcr: {dto.Reason}"
        }, ct);
        await _workflow.CancelOpenWorkItemsAsync(ncr.WorkflowInstanceId, dto.ActorUserId, "NCR voided", ct);
        ncr.LastModifiedByUserId = dto.ActorUserId;
        ncr.LastModifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(ncr);
    }

    public async Task AddAttachmentAsync(AddNcrAttachmentRequestDto dto, CancellationToken ct = default)
    {
        var ncr = await _db.Ncrs.FirstOrDefaultAsync(x => x.Id == dto.NcrId, ct)
            ?? throw new InvalidOperationException("NCR not found.");
        await EnsureWorkflowIsMutableAsync(ncr.WorkflowInstanceId, ct);

        _db.NcrAttachments.Add(new NcrAttachment
        {
            Id = Guid.NewGuid(),
            NcrId = dto.NcrId,
            FileName = dto.FileName,
            ContentType = dto.ContentType,
            StoragePath = dto.StoragePath,
            UploadedByUserId = dto.UploadedByUserId,
            UploadedAtUtc = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(ct);
    }

    private async Task EnsureAttachmentBeforeApprovalGateAsync(Ncr ncr, CancellationToken ct)
    {
        var wf = await _db.WorkflowInstances.FirstOrDefaultAsync(x => x.Id == ncr.WorkflowInstanceId, ct)
            ?? throw new InvalidOperationException("Workflow instance not found.");
        var stepDef = await _db.WorkflowStepDefinitions
            .Where(x => x.WorkflowDefinitionId == wf.WorkflowDefinitionId && x.StepCode == wf.CurrentStepCode)
            .FirstOrDefaultAsync(ct);
        if (stepDef != null && stepDef.ApprovalMode != "None")
        {
            var hasAnyImage = await _db.NcrAttachments.AnyAsync(x => x.NcrId == ncr.Id, ct);
            if (!hasAnyImage)
                throw new InvalidOperationException("At least one image attachment is required before approval.");
        }
    }

    private async Task EnsureWorkflowIsMutableAsync(Guid workflowInstanceId, CancellationToken ct)
    {
        var status = await _db.WorkflowInstances
            .Where(x => x.Id == workflowInstanceId)
            .Select(x => x.Status)
            .FirstOrDefaultAsync(ct);
        if (status is "Completed" or "Voided")
            throw new InvalidOperationException("Terminal NCR records are read-only.");
    }

    private async Task<User> RequireQualityAsync(Guid userId, CancellationToken ct)
    {
        var user = await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, ct)
            ?? throw new InvalidOperationException("Actor user not found.");
        if (!user.RoleName.Contains("Quality", StringComparison.OrdinalIgnoreCase) && user.RoleTier > 3m)
            throw new InvalidOperationException("Quality role required.");
        return user;
    }

    private static void ValidateVendorFields(Guid? vendorId, string? poNumber, decimal? quantity, string? heatNumber, string? coilOrSlabNumber)
    {
        if (!vendorId.HasValue || string.IsNullOrWhiteSpace(poNumber) || !quantity.HasValue
            || string.IsNullOrWhiteSpace(heatNumber) || string.IsNullOrWhiteSpace(coilOrSlabNumber))
        {
            throw new InvalidOperationException("Vendor-related NCR types require VendorId, PoNumber, Quantity, HeatNumber, and CoilOrSlabNumber.");
        }
    }

    private async Task<int> GetNextSequenceValueAsync(string sequenceName, CancellationToken ct)
    {
        var sequence = await _db.SequenceCounters.FirstOrDefaultAsync(x => x.Name == sequenceName, ct);
        if (sequence == null)
        {
            sequence = new SequenceCounter
            {
                Id = Guid.NewGuid(),
                Name = sequenceName,
                CurrentValue = 1
            };
            _db.SequenceCounters.Add(sequence);
            return sequence.CurrentValue;
        }

        sequence.CurrentValue += 1;
        return sequence.CurrentValue;
    }

    private static readonly System.Linq.Expressions.Expression<Func<Ncr, NcrDto>> MapProjection = x => new NcrDto
    {
        Id = x.Id,
        NcrNumber = x.NcrNumber,
        SourceType = x.SourceType,
        SourceEntityId = x.SourceEntityId,
        SiteCode = x.SiteCode,
        SubmitterUserId = x.SubmitterUserId,
        CoordinatorUserId = x.CoordinatorUserId,
        NcrTypeId = x.NcrTypeId,
        CurrentStepCode = x.CurrentStepCode,
        WorkflowInstanceId = x.WorkflowInstanceId,
        ProblemDescription = x.ProblemDescription,
        DateUtc = x.DateUtc
    };

    private static NcrDto Map(Ncr x) => new()
    {
        Id = x.Id,
        NcrNumber = x.NcrNumber,
        SourceType = x.SourceType,
        SourceEntityId = x.SourceEntityId,
        SiteCode = x.SiteCode,
        SubmitterUserId = x.SubmitterUserId,
        CoordinatorUserId = x.CoordinatorUserId,
        NcrTypeId = x.NcrTypeId,
        CurrentStepCode = x.CurrentStepCode,
        WorkflowInstanceId = x.WorkflowInstanceId,
        ProblemDescription = x.ProblemDescription,
        DateUtc = x.DateUtc
    };

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken ct)
    {
        if (string.Equals(_db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
            return null;
        return await _db.Database.BeginTransactionAsync(ct);
    }
}
