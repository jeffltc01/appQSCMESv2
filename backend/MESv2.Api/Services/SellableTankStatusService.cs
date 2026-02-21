using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public class SellableTankStatusService : ISellableTankStatusService
{
    private readonly MesDbContext _db;

    public SellableTankStatusService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<SellableTankStatusDto>> GetStatusAsync(
        Guid siteId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var tz = await GetPlantTimeZoneAsync(siteId, cancellationToken);
        var localDate = date.ToDateTime(TimeOnly.MinValue);
        var startUtc = TimeZoneInfo.ConvertTimeToUtc(localDate, tz);
        var endUtc = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), tz);

        var sellableTypeName = "sellable";
        var sellableSnList = await _db.SerialNumbers
            .Include(s => s.Product).ThenInclude(p => p!.ProductType)
            .Where(s => s.PlantId == siteId
                && s.Product != null
                && s.Product.ProductType != null
                && s.Product.ProductType.SystemTypeName == sellableTypeName
                && s.CreatedAt >= startUtc && s.CreatedAt < endUtc)
            .ToListAsync(cancellationToken);

        if (sellableSnList.Count == 0)
            return Array.Empty<SellableTankStatusDto>();

        var sellableIds = sellableSnList.Select(s => s.Id).ToList();

        var marriageLogs = await _db.TraceabilityLogs
            .Where(t => sellableIds.Contains(t.ToSerialNumberId!.Value)
                && t.Relationship == "hydro-marriage"
                && t.FromSerialNumberId != null)
            .ToListAsync(cancellationToken);

        var assemblyIds = marriageLogs
            .Select(t => t.FromSerialNumberId!.Value)
            .Distinct().ToList();

        var shellLogs = await _db.TraceabilityLogs
            .Where(t => assemblyIds.Contains(t.ToSerialNumberId!.Value)
                && t.Relationship == "shell"
                && t.FromSerialNumberId != null)
            .ToListAsync(cancellationToken);

        var allRelatedSnIds = new HashSet<Guid>(sellableIds);
        allRelatedSnIds.UnionWith(assemblyIds);
        allRelatedSnIds.UnionWith(shellLogs.Select(t => t.FromSerialNumberId!.Value));

        var gateCheckCpIds = await _db.ControlPlans
            .Where(cp => cp.IsGateCheck && cp.IsEnabled)
            .Select(cp => cp.Id)
            .ToListAsync(cancellationToken);

        var inspections = await _db.InspectionRecords
            .Include(i => i.ControlPlan).ThenInclude(cp => cp.Characteristic)
            .Where(i => allRelatedSnIds.Contains(i.SerialNumberId)
                && gateCheckCpIds.Contains(i.ControlPlanId))
            .ToListAsync(cancellationToken);

        var result = new List<SellableTankStatusDto>();

        foreach (var sellable in sellableSnList)
        {
            var marriage = marriageLogs.FirstOrDefault(t => t.ToSerialNumberId == sellable.Id);
            var assemblyId = marriage?.FromSerialNumberId;

            var shellIds = assemblyId.HasValue
                ? shellLogs.Where(t => t.ToSerialNumberId == assemblyId.Value).Select(t => t.FromSerialNumberId!.Value).ToHashSet()
                : new HashSet<Guid>();

            var treeSnIds = new HashSet<Guid> { sellable.Id };
            if (assemblyId.HasValue) treeSnIds.Add(assemblyId.Value);
            treeSnIds.UnionWith(shellIds);

            var tankInspections = inspections.Where(i => treeSnIds.Contains(i.SerialNumberId)).ToList();

            string? rtXray = null, spotXray = null, hydro = null;
            foreach (var insp in tankInspections)
            {
                var charName = insp.ControlPlan?.Characteristic?.Name?.ToLower() ?? "";
                if (charName.Contains("rt") || charName.Contains("real") || (charName.Contains("x-ray") && !charName.Contains("spot")))
                    rtXray ??= insp.ResultText;
                else if (charName.Contains("spot"))
                    spotXray ??= insp.ResultText;
                else if (charName.Contains("hydro"))
                    hydro ??= insp.ResultText;
            }

            result.Add(new SellableTankStatusDto
            {
                SerialNumber = sellable.Serial,
                ProductNumber = sellable.Product?.ProductNumber ?? "",
                TankSize = sellable.Product?.TankSize ?? 0,
                RtXrayResult = rtXray,
                SpotXrayResult = spotXray,
                HydroResult = hydro
            });
        }

        return result;
    }

    private async Task<TimeZoneInfo> GetPlantTimeZoneAsync(Guid plantId, CancellationToken ct)
    {
        var tzId = await _db.Plants
            .Where(p => p.Id == plantId)
            .Select(p => p.TimeZoneId)
            .FirstOrDefaultAsync(ct);

        if (!string.IsNullOrEmpty(tzId))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(tzId); }
            catch (TimeZoneNotFoundException) { }
        }
        return TimeZoneInfo.Utc;
    }
}
