using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public class WhereUsedService : IWhereUsedService
{
    private readonly MesDbContext _db;

    public WhereUsedService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WhereUsedResultDto>> SearchAsync(
        string? heatNumber,
        string? coilNumber,
        string? lotNumber,
        Guid? siteId,
        CancellationToken cancellationToken = default)
    {
        var heat = NormalizeInput(heatNumber);
        var coil = NormalizeInput(coilNumber);
        var lot = NormalizeInput(lotNumber);
        if (heat == null && coil == null && lot == null)
            return Array.Empty<WhereUsedResultDto>();

        var materialMatches = _db.SerialNumbers
            .AsNoTracking()
            .Where(s => !s.IsObsolete);

        if (heat != null)
            materialMatches = materialMatches.Where(s => s.HeatNumber == heat);
        if (coil != null)
            materialMatches = materialMatches.Where(s => s.CoilNumber == coil);
        if (lot != null)
            materialMatches = materialMatches.Where(s => s.LotNumber == lot);

        var matchedMaterialIds = await materialMatches
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        if (matchedMaterialIds.Count == 0)
            return Array.Empty<WhereUsedResultDto>();

        var connectedSerialIds = await ExpandConnectedSerialsAsync(matchedMaterialIds, cancellationToken);
        if (connectedSerialIds.Count == 0)
            return Array.Empty<WhereUsedResultDto>();

        var sellables = await _db.SerialNumbers
            .AsNoTracking()
            .Include(s => s.Product).ThenInclude(p => p!.ProductType)
            .Where(s => connectedSerialIds.Contains(s.Id) && !s.IsObsolete)
            .ToListAsync(cancellationToken);

        sellables = sellables
            .Where(s => NormalizeSystemType(s.Product?.ProductType?.SystemTypeName) == "sellable")
            .Where(s => !siteId.HasValue || s.PlantId == siteId.Value)
            .ToList();

        if (sellables.Count == 0)
            return Array.Empty<WhereUsedResultDto>();

        var sellableIds = sellables.Select(s => s.Id).Distinct().ToList();
        var plantMap = await _db.Plants
            .AsNoTracking()
            .Where(p => sellables.Select(s => s.PlantId).Distinct().Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var hydroInspectionBySellable = await _db.InspectionRecords
            .AsNoTracking()
            .Include(i => i.WorkCenter)
            .Where(i => sellableIds.Contains(i.SerialNumberId) && i.WorkCenter.DataEntryType == "Hydro")
            .GroupBy(i => i.SerialNumberId)
            .Select(g => new { g.Key, CompletedAt = g.Max(i => i.Timestamp) })
            .ToDictionaryAsync(x => x.Key, x => x.CompletedAt, cancellationToken);

        var hydroProductionBySellable = await _db.ProductionRecords
            .AsNoTracking()
            .Include(r => r.WorkCenter)
            .Where(r => sellableIds.Contains(r.SerialNumberId) && r.WorkCenter.DataEntryType == "Hydro")
            .GroupBy(r => r.SerialNumberId)
            .Select(g => new { g.Key, CompletedAt = g.Max(r => r.Timestamp) })
            .ToDictionaryAsync(x => x.Key, x => x.CompletedAt, cancellationToken);

        return sellables
            .Select(s =>
            {
                var plant = plantMap.GetValueOrDefault(s.PlantId);
                var plantText = plant == null ? string.Empty : $"{plant.Name} ({plant.Code})";
                var hydroCompletedAt = hydroInspectionBySellable.GetValueOrDefault(s.Id);
                if (hydroCompletedAt == default)
                    hydroCompletedAt = hydroProductionBySellable.GetValueOrDefault(s.Id);

                return new WhereUsedResultDto
                {
                    Plant = plantText,
                    SerialNumber = s.Serial,
                    ProductionNumber = s.Product?.ProductNumber ?? string.Empty,
                    TankSize = s.Product?.TankSize ?? 0,
                    HydroCompletedAt = hydroCompletedAt == default ? null : hydroCompletedAt
                };
            })
            .OrderByDescending(x => x.HydroCompletedAt)
            .ThenBy(x => x.SerialNumber)
            .ToList();
    }

    private async Task<HashSet<Guid>> ExpandConnectedSerialsAsync(
        IReadOnlyCollection<Guid> seedSerialIds,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<Guid>(seedSerialIds);
        var frontier = new HashSet<Guid>(seedSerialIds);
        var maxDepth = 8;

        for (var depth = 0; depth < maxDepth && frontier.Count > 0; depth++)
        {
            var frontierList = frontier.ToList();
            var edges = await _db.TraceabilityLogs
                .AsNoTracking()
                .Where(t =>
                    (t.FromSerialNumberId.HasValue && frontierList.Contains(t.FromSerialNumberId.Value)) ||
                    (t.ToSerialNumberId.HasValue && frontierList.Contains(t.ToSerialNumberId.Value)))
                .Select(t => new { t.FromSerialNumberId, t.ToSerialNumberId })
                .ToListAsync(cancellationToken);

            var nextFrontier = new HashSet<Guid>();
            foreach (var edge in edges)
            {
                if (edge.FromSerialNumberId.HasValue && visited.Add(edge.FromSerialNumberId.Value))
                    nextFrontier.Add(edge.FromSerialNumberId.Value);
                if (edge.ToSerialNumberId.HasValue && visited.Add(edge.ToSerialNumberId.Value))
                    nextFrontier.Add(edge.ToSerialNumberId.Value);
            }

            frontier = nextFrontier;
        }

        return visited;
    }

    private static string? NormalizeInput(string? value)
    {
        var trimmed = value?.Trim();
        return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed;
    }

    private static string? NormalizeSystemType(string? systemTypeName)
    {
        if (string.IsNullOrWhiteSpace(systemTypeName))
            return null;

        var normalized = systemTypeName.Trim().ToLowerInvariant();
        return normalized switch
        {
            "assembeled" => "assembled",
            _ => normalized
        };
    }
}
