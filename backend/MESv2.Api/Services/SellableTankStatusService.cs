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
                && (t.Relationship == "ShellToAssembly" || t.Relationship == "shell")
                && t.FromSerialNumberId != null)
            .ToListAsync(cancellationToken);

        var allShellIds = shellLogs.Select(t => t.FromSerialNumberId!.Value).Distinct().ToList();

        var assemblySerials = await _db.SerialNumbers
            .Where(s => assemblyIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Serial })
            .ToListAsync(cancellationToken);
        var assemblySerialMap = assemblySerials.ToDictionary(a => a.Id, a => a.Serial);

        var shellSerials = await _db.SerialNumbers
            .Where(s => allShellIds.Contains(s.Id))
            .Select(s => new { s.Id, s.Serial })
            .ToListAsync(cancellationToken);
        var shellSerialMap = shellSerials.ToDictionary(s => s.Id, s => s.Serial);

        var allRelatedSnIds = new HashSet<Guid>(sellableIds);
        allRelatedSnIds.UnionWith(assemblyIds);
        allRelatedSnIds.UnionWith(allShellIds);

        var gateCheckCpIds = await _db.ControlPlans
            .Where(cp => cp.IsGateCheck && cp.IsEnabled && cp.IsActive)
            .Select(cp => cp.Id)
            .ToListAsync(cancellationToken);

        var inspections = await _db.InspectionRecords
            .Include(i => i.ControlPlan).ThenInclude(cp => cp.Characteristic)
            .Where(i => allRelatedSnIds.Contains(i.SerialNumberId)
                && gateCheckCpIds.Contains(i.ControlPlanId))
            .ToListAsync(cancellationToken);

        // Spot X-ray: query increments directly for assemblies
        var spotIncrementTanks = assemblyIds.Count > 0
            ? await _db.SpotXrayIncrementTanks
                .Include(t => t.SpotXrayIncrement)
                .Where(t => assemblyIds.Contains(t.SerialNumberId)
                         && !t.SpotXrayIncrement.IsDraft)
                .ToListAsync(cancellationToken)
            : new List<Models.SpotXrayIncrementTank>();

        var spotResultByAssembly = new Dictionary<Guid, string>();
        foreach (var sit in spotIncrementTanks)
        {
            var inc = sit.SpotXrayIncrement;
            string tankResult;
            if (inc.OverallStatus == "Reject")
                tankResult = "Reject";
            else if (inc.OverallStatus == "Accept-Scrap" && sit.SerialNumberId == inc.InspectTankId)
                tankResult = "Reject";
            else if (inc.OverallStatus == "Accept" || inc.OverallStatus == "Accept-Scrap")
                tankResult = "Accept";
            else
                continue;
            spotResultByAssembly.TryAdd(sit.SerialNumberId, tankResult);
        }

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

            string? rtXray = null, spotXray = null, hydro = null;

            foreach (var insp in inspections.Where(i => treeSnIds.Contains(i.SerialNumberId)))
            {
                var charName = insp.ControlPlan.Characteristic?.Name?.ToLower() ?? "";
                ClassifyGateResult(charName, insp.ResultText, ref rtXray, ref spotXray, ref hydro);
            }

            if (assemblyId.HasValue && spotResultByAssembly.TryGetValue(assemblyId.Value, out var spotRes))
                spotXray ??= spotRes;

            string? alphaCode = null;
            var shellSerialList = new List<string>();
            if (assemblyId.HasValue)
            {
                assemblySerialMap.TryGetValue(assemblyId.Value, out alphaCode);
                shellSerialList = shellIds
                    .Select(sid => shellSerialMap.GetValueOrDefault(sid, ""))
                    .Where(s => !string.IsNullOrEmpty(s))
                    .ToList();
            }

            result.Add(new SellableTankStatusDto
            {
                SerialNumber = sellable.Serial,
                AlphaCode = alphaCode,
                ShellSerials = shellSerialList,
                ProductNumber = sellable.Product?.ProductNumber ?? "",
                TankSize = sellable.Product?.TankSize ?? 0,
                RtXrayResult = rtXray,
                SpotXrayResult = spotXray,
                HydroResult = hydro
            });
        }

        return result;
    }

    private static void ClassifyGateResult(string charName, string? resultText,
        ref string? rtXray, ref string? spotXray, ref string? hydro)
    {
        if (string.IsNullOrEmpty(charName) || string.IsNullOrEmpty(resultText)) return;
        if (charName.Contains("rt") || charName.Contains("real") || (charName.Contains("x-ray") && !charName.Contains("spot")))
            rtXray ??= resultText;
        else if (charName.Contains("spot"))
            spotXray ??= resultText;
        else if (charName.Contains("hydro"))
            hydro ??= resultText;
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
