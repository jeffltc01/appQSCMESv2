using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class HoldTagService : IHoldTagService
{
    private readonly MesDbContext _db;
    private readonly IWorkflowEngineService _workflow;

    public HoldTagService(MesDbContext db, IWorkflowEngineService workflow)
    {
        _db = db;
        _workflow = workflow;
    }

    public async Task<HoldTagDto> CreateHoldTagAsync(CreateHoldTagRequestDto dto, CancellationToken ct = default)
    {
        _ = await RequireAnyUserAsync(dto.ActorUserId, ct);
        if (string.IsNullOrWhiteSpace(dto.SiteCode))
            throw new InvalidOperationException("SiteCode is required.");
        if (dto.SerialNumberMasterId == Guid.Empty && string.IsNullOrWhiteSpace(dto.SerialNumberText))
            throw new InvalidOperationException("SerialNumberMasterId or SerialNumberText is required.");
        if (string.IsNullOrWhiteSpace(dto.ProblemDescription))
            throw new InvalidOperationException("ProblemDescription is required.");

        var serialNumberMasterId = dto.SerialNumberMasterId;
        if (serialNumberMasterId == Guid.Empty && !string.IsNullOrWhiteSpace(dto.SerialNumberText))
        {
            serialNumberMasterId = await _db.SerialNumbers
                .Where(x => x.Serial == dto.SerialNumberText)
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => x.Id)
                .FirstOrDefaultAsync(ct);
            if (serialNumberMasterId == Guid.Empty)
                throw new InvalidOperationException("Serial number was not found.");
        }

        await using var tx = await BeginTransactionIfSupportedAsync(ct);
        var number = await GetNextSequenceValueAsync("HoldTagNumber", ct);
        var holdTag = new HoldTag
        {
            Id = Guid.NewGuid(),
            HoldTagNumber = number,
            SiteCode = dto.SiteCode,
            ProductionLineId = dto.ProductionLineId,
            WorkCenterId = dto.WorkCenterId,
            SerialNumberMasterId = serialNumberMasterId,
            ProblemDescription = dto.ProblemDescription,
            DefectCodeId = dto.DefectCodeId,
            CreatedByUserId = dto.ActorUserId,
            CreatedAtUtc = DateTime.UtcNow,
            LastModifiedByUserId = dto.ActorUserId,
            LastModifiedAtUtc = DateTime.UtcNow,
            BusinessStatus = "Open"
        };

        _db.HoldTags.Add(holdTag);
        await _db.SaveChangesAsync(ct);

        var wf = await _workflow.StartWorkflowAsync(new StartWorkflowRequestDto
        {
            EntityType = "HoldTag",
            EntityId = holdTag.Id,
            WorkflowType = "HoldTag"
        }, ct);
        holdTag.WorkflowInstanceId = wf.Id;

        await _db.SaveChangesAsync(ct);
        if (tx != null)
            await tx.CommitAsync(ct);
        return Map(holdTag);
    }

    public async Task<HoldTagDto> SetDispositionAsync(SetHoldTagDispositionRequestDto dto, CancellationToken ct = default)
    {
        var actor = await RequireQualityAsync(dto.ActorUserId, ct);
        var holdTag = await _db.HoldTags.FirstOrDefaultAsync(x => x.Id == dto.HoldTagId, ct)
            ?? throw new InvalidOperationException("Hold tag not found.");
        EnsureNonTerminal(holdTag);

        holdTag.Disposition = dto.Disposition;
        holdTag.DispositionSetByUserId = dto.ActorUserId;
        holdTag.DispositionSetAtUtc = DateTime.UtcNow;
        holdTag.DispositionNotes = dto.DispositionNotes;
        holdTag.ReleaseJustification = dto.ReleaseJustification;
        holdTag.RepairInstructionTemplateId = dto.RepairInstructionTemplateId;
        holdTag.RepairInstructionNotes = dto.RepairInstructionNotes;
        holdTag.ScrapReasonCode = dto.ScrapReasonCode;
        holdTag.ScrapReasonText = dto.ScrapReasonText;
        holdTag.LastModifiedByUserId = actor.Id;
        holdTag.LastModifiedAtUtc = DateTime.UtcNow;

        if (dto.Disposition == "ReleaseAsIs")
        {
            if (string.IsNullOrWhiteSpace(dto.ReleaseJustification))
                throw new InvalidOperationException("ReleaseJustification is required for ReleaseAsIs.");
            if (string.IsNullOrWhiteSpace(dto.DispositionNotes))
                throw new InvalidOperationException("DispositionNotes are required for ReleaseAsIs.");
        }
        if (dto.Disposition == "Repair" && !dto.RepairInstructionTemplateId.HasValue)
            throw new InvalidOperationException("RepairInstructionTemplateId is required for Repair.");
        if (dto.Disposition == "Scrap" && string.IsNullOrWhiteSpace(dto.ScrapReasonCode) && string.IsNullOrWhiteSpace(dto.ScrapReasonText))
            throw new InvalidOperationException("ScrapReasonCode or ScrapReasonText is required for Scrap.");

        await _db.SaveChangesAsync(ct);
        return Map(holdTag);
    }

    public async Task<HoldTagDto> LinkNcrAsync(LinkHoldTagNcrRequestDto dto, CancellationToken ct = default)
    {
        _ = await RequireQualityAsync(dto.ActorUserId, ct);
        var holdTag = await _db.HoldTags.FirstOrDefaultAsync(x => x.Id == dto.HoldTagId, ct)
            ?? throw new InvalidOperationException("Hold tag not found.");
        EnsureNonTerminal(holdTag);
        var ncrExists = await _db.Ncrs.AnyAsync(x => x.Id == dto.LinkedNcrId, ct);
        if (!ncrExists)
            throw new InvalidOperationException("Linked NCR was not found.");

        holdTag.LinkedNcrId = dto.LinkedNcrId;
        holdTag.LastModifiedByUserId = dto.ActorUserId;
        holdTag.LastModifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(holdTag);
    }

    public async Task<HoldTagDto> ResolveAsync(ResolveHoldTagRequestDto dto, CancellationToken ct = default)
    {
        _ = await RequireQualityAsync(dto.ActorUserId, ct);
        var holdTag = await _db.HoldTags.FirstOrDefaultAsync(x => x.Id == dto.HoldTagId, ct)
            ?? throw new InvalidOperationException("Hold tag not found.");
        EnsureNonTerminal(holdTag);
        if (string.IsNullOrWhiteSpace(holdTag.Disposition))
            throw new InvalidOperationException("Disposition must be set before resolve.");
        if ((holdTag.Disposition == "Repair" || holdTag.Disposition == "Scrap") && !holdTag.LinkedNcrId.HasValue)
            throw new InvalidOperationException("LinkedNcrId is required before closure for Repair/Scrap.");

        await _workflow.AdvanceStepAsync(new AdvanceStepRequestDto
        {
            WorkflowInstanceId = holdTag.WorkflowInstanceId,
            ActionCode = "ResolveHoldTag",
            ActorUserId = dto.ActorUserId,
            PayloadJson = "{}"
        }, ct);
        holdTag.BusinessStatus = "Resolved";
        holdTag.LastModifiedByUserId = dto.ActorUserId;
        holdTag.LastModifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(holdTag);
    }

    public async Task<HoldTagDto> VoidAsync(VoidHoldTagRequestDto dto, CancellationToken ct = default)
    {
        _ = await RequireQualityAsync(dto.ActorUserId, ct);
        if (string.IsNullOrWhiteSpace(dto.Reason))
            throw new InvalidOperationException("Void reason is required.");
        var holdTag = await _db.HoldTags.FirstOrDefaultAsync(x => x.Id == dto.HoldTagId, ct)
            ?? throw new InvalidOperationException("Hold tag not found.");
        EnsureNonTerminal(holdTag);

        await _workflow.RejectStepAsync(new ApproveRejectRequestDto
        {
            WorkflowInstanceId = holdTag.WorkflowInstanceId,
            StepCode = (await _db.WorkflowInstances.Where(x => x.Id == holdTag.WorkflowInstanceId).Select(x => x.CurrentStepCode).FirstAsync(ct)),
            ActorUserId = dto.ActorUserId,
            Comments = $"VoidHoldTag: {dto.Reason}"
        }, ct);
        await _workflow.CancelOpenWorkItemsAsync(holdTag.WorkflowInstanceId, dto.ActorUserId, "HoldTag voided", ct);
        holdTag.BusinessStatus = "Voided";
        holdTag.LastModifiedByUserId = dto.ActorUserId;
        holdTag.LastModifiedAtUtc = DateTime.UtcNow;
        await _db.SaveChangesAsync(ct);
        return Map(holdTag);
    }

    public async Task<HoldTagDto?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var item = await _db.HoldTags.FirstOrDefaultAsync(x => x.Id == id, ct);
        return item == null ? null : Map(item);
    }

    public async Task<IReadOnlyList<HoldTagDto>> GetListAsync(string? siteCode, CancellationToken ct = default)
    {
        var query = _db.HoldTags.AsQueryable();
        if (!string.IsNullOrWhiteSpace(siteCode))
            query = query.Where(x => x.SiteCode == siteCode);
        return await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Select(x => new HoldTagDto
            {
                Id = x.Id,
                HoldTagNumber = x.HoldTagNumber,
                SiteCode = x.SiteCode,
                ProductionLineId = x.ProductionLineId,
                WorkCenterId = x.WorkCenterId,
                SerialNumberMasterId = x.SerialNumberMasterId,
                ProblemDescription = x.ProblemDescription,
                DefectCodeId = x.DefectCodeId,
                Disposition = x.Disposition,
                BusinessStatus = x.BusinessStatus,
                WorkflowInstanceId = x.WorkflowInstanceId,
                LinkedNcrId = x.LinkedNcrId,
                CreatedAtUtc = x.CreatedAtUtc
            }).ToListAsync(ct);
    }

    private static HoldTagDto Map(HoldTag x) => new()
    {
        Id = x.Id,
        HoldTagNumber = x.HoldTagNumber,
        SiteCode = x.SiteCode,
        ProductionLineId = x.ProductionLineId,
        WorkCenterId = x.WorkCenterId,
        SerialNumberMasterId = x.SerialNumberMasterId,
        ProblemDescription = x.ProblemDescription,
        DefectCodeId = x.DefectCodeId,
        Disposition = x.Disposition,
        BusinessStatus = x.BusinessStatus,
        WorkflowInstanceId = x.WorkflowInstanceId,
        LinkedNcrId = x.LinkedNcrId,
        CreatedAtUtc = x.CreatedAtUtc
    };

    private static void EnsureNonTerminal(HoldTag holdTag)
    {
        if (holdTag.BusinessStatus is "Resolved" or "Voided")
            throw new InvalidOperationException("Terminal hold tags are immutable.");
    }

    private async Task<User> RequireAnyUserAsync(Guid userId, CancellationToken ct)
    {
        return await _db.Users.FirstOrDefaultAsync(x => x.Id == userId && x.IsActive, ct)
            ?? throw new InvalidOperationException("Actor user not found.");
    }

    private async Task<User> RequireQualityAsync(Guid userId, CancellationToken ct)
    {
        var user = await RequireAnyUserAsync(userId, ct);
        if (!user.RoleName.Contains("Quality", StringComparison.OrdinalIgnoreCase) && user.RoleTier > 3m)
            throw new InvalidOperationException("Quality role required.");
        return user;
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

    private async Task<Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction?> BeginTransactionIfSupportedAsync(CancellationToken ct)
    {
        if (string.Equals(_db.Database.ProviderName, "Microsoft.EntityFrameworkCore.InMemory", StringComparison.Ordinal))
            return null;
        return await _db.Database.BeginTransactionAsync(ct);
    }
}
