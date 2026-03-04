using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public class SerialProcessingGateService : ISerialProcessingGateService
{
    private readonly MesDbContext _db;

    public SerialProcessingGateService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<SerialProcessingBlockResultDto> EvaluateBySerialIdAsync(Guid serialNumberId, CancellationToken cancellationToken = default)
    {
        var openHoldTags = await _db.HoldTags
            .Where(x => x.SerialNumberMasterId == serialNumberId)
            .Where(x => x.BusinessStatus != "Resolved" && x.BusinessStatus != "Voided")
            .OrderBy(x => x.HoldTagNumber)
            .Select(x => x.HoldTagNumber)
            .Distinct()
            .ToListAsync(cancellationToken);

        var relatedHoldTagIds = await _db.HoldTags
            .Where(x => x.SerialNumberMasterId == serialNumberId)
            .Select(x => x.Id)
            .ToListAsync(cancellationToken);

        var openNcrs = await _db.Ncrs
            .Where(n => (n.SourceType == "HoldTagEscalation" && n.SourceEntityId.HasValue && relatedHoldTagIds.Contains(n.SourceEntityId.Value))
                     || (n.SourceType == "DirectQuality" && n.SourceEntityId == serialNumberId))
            .Join(
                _db.WorkflowInstances,
                n => n.WorkflowInstanceId,
                wi => wi.Id,
                (n, wi) => new { n.NcrNumber, wi.Status })
            .Where(x => x.Status != "Completed" && x.Status != "Voided")
            .OrderBy(x => x.NcrNumber)
            .Select(x => x.NcrNumber)
            .Distinct()
            .ToListAsync(cancellationToken);

        var reasons = new List<string>();
        if (openHoldTags.Count > 0)
            reasons.Add($"Open Hold Tags: {string.Join(", ", openHoldTags.Select(x => $"HT-{x}"))}");
        if (openNcrs.Count > 0)
            reasons.Add($"Open NCRs: {string.Join(", ", openNcrs.Select(x => $"NCR-{x}"))}");

        return new SerialProcessingBlockResultDto
        {
            IsBlocked = reasons.Count > 0,
            OpenHoldTagNumbers = openHoldTags,
            OpenNcrNumbers = openNcrs,
            Reasons = reasons
        };
    }
}
