using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public class DigitalTwinService : IDigitalTwinService
{
    private static readonly Dictionary<string, int> ProductionSequence = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rolls"] = 1,
        ["Barcode-LongSeam"] = 2,
        ["Barcode-LongSeamInsp"] = 3,
        ["MatQueue-Shell"] = 4,
        ["Fitup"] = 5,
        ["Barcode-RoundSeam"] = 6,
        ["Barcode-RoundSeamInsp"] = 7,
        ["Spot"] = 8,
        ["Hydro"] = 9,
    };

    private static readonly Dictionary<string, string> DisplayNames = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rolls"] = "Rolls",
        ["Barcode-LongSeam"] = "Long Seam",
        ["Barcode-LongSeamInsp"] = "LS Inspect",
        ["MatQueue-Shell"] = "RT X-ray",
        ["Fitup"] = "Fitup",
        ["Barcode-RoundSeam"] = "Round Seam",
        ["Barcode-RoundSeamInsp"] = "RS Inspect",
        ["Spot"] = "Spot X-ray",
        ["Hydro"] = "Hydro",
    };

    private static readonly HashSet<string> GateCheckTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "MatQueue-Shell", "Spot", "Hydro",
    };

    private static readonly List<string> ProductionDataEntryTypes = ProductionSequence.Keys.ToList();

    private static readonly TimeSpan ActiveThreshold = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan SlowThreshold = TimeSpan.FromMinutes(60);

    private readonly MesDbContext _db;
    private readonly ILogger<DigitalTwinService> _logger;

    public DigitalTwinService(MesDbContext db, ILogger<DigitalTwinService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<DigitalTwinSnapshotDto> GetSnapshotAsync(
        Guid plantId, Guid productionLineId, CancellationToken cancellationToken = default)
    {
        var tz = await GetPlantTimeZoneAsync(plantId, cancellationToken);
        var utcNow = DateTime.UtcNow;
        var localNow = TimeZoneInfo.ConvertTimeFromUtc(utcNow, tz);
        var localToday = localNow.Date;
        var startOfDay = TimeZoneInfo.ConvertTimeToUtc(localToday, tz);
        var endOfDay = TimeZoneInfo.ConvertTimeToUtc(localToday.AddDays(1), tz);
        var startOfYesterday = TimeZoneInfo.ConvertTimeToUtc(localToday.AddDays(-1), tz);
        var wipCutoff = utcNow.AddDays(-3);

        var workCenters = await _db.WorkCenters
            .Where(w => w.DataEntryType != null && ProductionDataEntryTypes.Contains(w.DataEntryType))
            .Select(w => new { w.Id, w.Name, w.DataEntryType })
            .ToListAsync(cancellationToken);

        var stationWcMap = workCenters
            .Where(w => w.DataEntryType != null)
            .ToDictionary(w => w.Id, w => w);

        var stationIds = stationWcMap.Keys.ToList();

        var recentRecords = await _db.ProductionRecords
            .Where(r => r.ProductionLineId == productionLineId
                        && r.Timestamp >= wipCutoff
                        && stationIds.Contains(r.WorkCenterId))
            .Select(r => new
            {
                r.SerialNumberId,
                r.WorkCenterId,
                r.Timestamp,
                r.OperatorId,
                OperatorName = r.Operator.DisplayName,
                Serial = r.SerialNumber.Serial,
                ProductName = r.SerialNumber.Product != null
                    ? r.SerialNumber.Product.TankSize + " " + r.SerialNumber.Product.TankType
                    : null,
            })
            .ToListAsync(cancellationToken);

        var consumedShellSnIds = await _db.TraceabilityLogs
            .Where(t => (t.Relationship == "ShellToAssembly" || t.Relationship == "shell") && t.FromSerialNumberId.HasValue)
            .Select(t => t.FromSerialNumberId!.Value)
            .ToListAsync(cancellationToken);
        var consumedSet = consumedShellSnIds.ToHashSet();

        var activeRecords = recentRecords
            .Where(r => !consumedSet.Contains(r.SerialNumberId))
            .ToList();

        var todayRecords = recentRecords
            .Where(r => r.Timestamp >= startOfDay && r.Timestamp < endOfDay)
            .ToList();

        // --- WIP per station ---
        var wipCounts = activeRecords
            .GroupBy(r => r.SerialNumberId)
            .Select(g => g.OrderByDescending(r => r.Timestamp).First())
            .GroupBy(r => r.WorkCenterId)
            .ToDictionary(g => g.Key, g => g.Count());

        // --- Units today per station ---
        var unitsTodayByStation = todayRecords
            .GroupBy(r => r.WorkCenterId)
            .ToDictionary(g => g.Key, g => g.Select(r => r.SerialNumberId).Distinct().Count());

        // --- Latest record per station (for status) ---
        var latestByStation = recentRecords
            .GroupBy(r => r.WorkCenterId)
            .ToDictionary(g => g.Key, g => g.OrderByDescending(r => r.Timestamp).First());

        // --- Most active operator per station today ---
        var operatorByStation = todayRecords
            .GroupBy(r => r.WorkCenterId)
            .ToDictionary(g => g.Key, g =>
                g.GroupBy(r => r.OperatorName)
                 .OrderByDescending(og => og.Count())
                 .Select(og => og.Key)
                 .FirstOrDefault());

        // --- Average time between scans per station today ---
        var avgCycleByStation = new Dictionary<Guid, decimal>();
        foreach (var stationGroup in todayRecords.GroupBy(r => r.WorkCenterId))
        {
            var timestamps = stationGroup.Select(r => r.Timestamp).OrderBy(t => t).ToList();
            if (timestamps.Count >= 2)
            {
                var total = 0.0;
                for (var i = 1; i < timestamps.Count; i++)
                    total += (timestamps[i] - timestamps[i - 1]).TotalMinutes;
                avgCycleByStation[stationGroup.Key] = Math.Round((decimal)(total / (timestamps.Count - 1)), 1);
            }
        }

        // --- Open downtime events ---
        var openDowntimeWcplIds = await _db.DowntimeEvents
            .Where(e => e.EndedAt == DateTime.MinValue
                        && e.WorkCenterProductionLine.ProductionLineId == productionLineId)
            .Select(e => e.WorkCenterProductionLine.WorkCenterId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var openDowntimeSet = openDowntimeWcplIds.ToHashSet();

        // --- Build station list ---
        var stations = workCenters
            .Where(w => w.DataEntryType != null && ProductionSequence.ContainsKey(w.DataEntryType))
            .OrderBy(w => ProductionSequence[w.DataEntryType!])
            .Select(w =>
            {
                var wip = wipCounts.GetValueOrDefault(w.Id);
                var latestRecord = latestByStation.GetValueOrDefault(w.Id);
                var timeSinceLast = latestRecord != null ? utcNow - latestRecord.Timestamp : (TimeSpan?)null;

                string status;
                if (openDowntimeSet.Contains(w.Id))
                    status = "Down";
                else if (timeSinceLast.HasValue && timeSinceLast.Value <= ActiveThreshold)
                    status = "Active";
                else if (timeSinceLast.HasValue && timeSinceLast.Value <= SlowThreshold)
                    status = "Slow";
                else
                    status = "Idle";

                return new StationStatusDto
                {
                    WorkCenterId = w.Id,
                    Name = DisplayNames.GetValueOrDefault(w.DataEntryType!, w.Name),
                    Sequence = ProductionSequence[w.DataEntryType!],
                    WipCount = wip,
                    Status = status,
                    IsBottleneck = false,
                    IsGateCheck = GateCheckTypes.Contains(w.DataEntryType!),
                    CurrentOperator = operatorByStation.GetValueOrDefault(w.Id),
                    UnitsToday = unitsTodayByStation.GetValueOrDefault(w.Id),
                    AvgCycleTimeMinutes = avgCycleByStation.GetValueOrDefault(w.Id),
                    FirstPassYieldPercent = null,
                };
            })
            .ToList();

        // --- Bottleneck detection ---
        var maxWip = stations.Where(s => s.Status is "Active" or "Slow").MaxBy(s => s.WipCount);
        if (maxWip is { WipCount: > 0 })
            maxWip.IsBottleneck = true;

        // --- Line throughput ---
        var hydroStation = stations.FirstOrDefault(s => s.Sequence == ProductionSequence["Hydro"]);
        var hydroWcId = workCenters.FirstOrDefault(w => w.DataEntryType == "Hydro")?.Id;
        var hydroToday = hydroWcId.HasValue
            ? todayRecords.Count(r => r.WorkCenterId == hydroWcId.Value)
            : 0;

        var hydroYesterday = 0;
        if (hydroWcId.HasValue)
        {
            hydroYesterday = await _db.ProductionRecords
                .CountAsync(r => r.ProductionLineId == productionLineId
                                 && r.WorkCenterId == hydroWcId.Value
                                 && r.Timestamp >= startOfYesterday
                                 && r.Timestamp < startOfDay, cancellationToken);
        }

        var hoursElapsed = (decimal)(utcNow - startOfDay).TotalHours;
        var throughput = new LineThroughputDto
        {
            UnitsToday = hydroToday,
            UnitsDelta = hydroToday - hydroYesterday,
            UnitsPerHour = hoursElapsed > 0 ? Math.Round(hydroToday / hoursElapsed, 1) : 0,
        };

        // --- Average end-to-end cycle time ---
        var rollsWcId = workCenters.FirstOrDefault(w => w.DataEntryType == "Rolls")?.Id;
        decimal avgCycleTime = 0;
        if (rollsWcId.HasValue && hydroWcId.HasValue)
        {
            var hydroSerials = todayRecords
                .Where(r => r.WorkCenterId == hydroWcId.Value)
                .Select(r => r.SerialNumberId)
                .Distinct()
                .ToList();

            if (hydroSerials.Count > 0)
            {
                var rollsTimestamps = recentRecords
                    .Where(r => r.WorkCenterId == rollsWcId.Value && hydroSerials.Contains(r.SerialNumberId))
                    .GroupBy(r => r.SerialNumberId)
                    .Select(g => new
                    {
                        SerialNumberId = g.Key,
                        RollsTime = g.Min(r => r.Timestamp),
                    })
                    .ToDictionary(x => x.SerialNumberId, x => x.RollsTime);

                var hydroTimestamps = todayRecords
                    .Where(r => r.WorkCenterId == hydroWcId.Value)
                    .GroupBy(r => r.SerialNumberId)
                    .Select(g => new
                    {
                        SerialNumberId = g.Key,
                        HydroTime = g.Max(r => r.Timestamp),
                    })
                    .ToDictionary(x => x.SerialNumberId, x => x.HydroTime);

                var cycleTimes = new List<double>();
                foreach (var serial in hydroSerials)
                {
                    if (rollsTimestamps.TryGetValue(serial, out var rollsTime)
                        && hydroTimestamps.TryGetValue(serial, out var hydroTime))
                    {
                        cycleTimes.Add((hydroTime - rollsTime).TotalMinutes);
                    }
                }

                if (cycleTimes.Count > 0)
                    avgCycleTime = Math.Round((decimal)cycleTimes.Average(), 1);
            }
        }

        // --- Line efficiency ---
        const decimal targetUnitsPerHour = 6m;
        var theoreticalMax = hoursElapsed > 0 ? targetUnitsPerHour * hoursElapsed : 1m;
        var efficiency = Math.Min(100, Math.Round(hydroToday / theoreticalMax * 100, 0));

        // --- Material feeds ---
        var materialFeeds = new List<MaterialFeedDto>();

        // Material queue work centers aren't in our ProductionSequence, so fetch them separately.
        // Items are stored under the production WC (MaterialQueueForWCId), not the queue WC itself.
        var queueWcIds = await _db.WorkCenters
            .Where(w => w.DataEntryType == "MatQueue-Material" || w.DataEntryType == "MatQueue-Fitup")
            .Select(w => new { w.Id, w.DataEntryType, w.MaterialQueueForWCId })
            .ToListAsync(cancellationToken);

        foreach (var qwc in queueWcIds)
        {
            var countWcId = qwc.MaterialQueueForWCId ?? qwc.Id;
            var count = await _db.MaterialQueueItems
                .CountAsync(m => m.WorkCenterId == countWcId
                                 && (m.Status == "queued" || m.Status == "active"),
                    cancellationToken);

            var isRollsMaterial = qwc.DataEntryType == "MatQueue-Material";
            materialFeeds.Add(new MaterialFeedDto
            {
                WorkCenterName = isRollsMaterial ? "Rolls Material" : "Heads Queue",
                QueueLabel = $"{count} lots",
                ItemCount = count,
                FeedsIntoStation = isRollsMaterial ? "Rolls" : "Fitup",
            });
        }

        // --- Unit tracker (exclude shells consumed into assemblies) ---
        var unitTracker = todayRecords
            .Where(r => !consumedSet.Contains(r.SerialNumberId))
            .GroupBy(r => r.SerialNumberId)
            .Select(g =>
            {
                var latest = g.OrderByDescending(r => r.Timestamp).First();
                var wc = stationWcMap.GetValueOrDefault(latest.WorkCenterId);
                var dataEntryType = wc?.DataEntryType ?? "";
                return new UnitPositionDto
                {
                    SerialNumber = latest.Serial,
                    ProductName = latest.ProductName,
                    CurrentStationName = DisplayNames.GetValueOrDefault(dataEntryType, wc?.Name ?? "Unknown"),
                    CurrentStationSequence = ProductionSequence.GetValueOrDefault(dataEntryType),
                    EnteredCurrentStationAt = latest.Timestamp,
                    IsAssembly = ProductionSequence.GetValueOrDefault(dataEntryType) >= 5,
                };
            })
            .OrderByDescending(u => u.EnteredCurrentStationAt)
            .Take(15)
            .ToList();

        return new DigitalTwinSnapshotDto
        {
            Stations = stations,
            MaterialFeeds = materialFeeds,
            Throughput = throughput,
            AvgCycleTimeMinutes = avgCycleTime,
            LineEfficiencyPercent = efficiency,
            UnitTracker = unitTracker,
        };
    }

    private async Task<TimeZoneInfo> GetPlantTimeZoneAsync(
        Guid plantId, CancellationToken cancellationToken)
    {
        var tzId = await _db.Plants
            .Where(p => p.Id == plantId)
            .Select(p => p.TimeZoneId)
            .FirstOrDefaultAsync(cancellationToken);

        if (!string.IsNullOrEmpty(tzId))
        {
            try { return TimeZoneInfo.FindSystemTimeZoneById(tzId); }
            catch (TimeZoneNotFoundException) { }
        }

        return TimeZoneInfo.Utc;
    }
}
