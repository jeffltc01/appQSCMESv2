using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public class LogViewerService : ILogViewerService
{
    private readonly MesDbContext _db;

    public LogViewerService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<List<RollsLogEntryDto>> GetRollsLogAsync(
        Guid siteId, string startDate, string endDate, CancellationToken ct = default)
    {
        var (utcStart, utcEnd, tz) = await ResolveDateRangeAsync(siteId, startDate, endDate, ct);

        var wcTypeId = await GetWcTypeIdAsync("Rolls", ct);
        if (wcTypeId == null) return new List<RollsLogEntryDto>();

        var records = await _db.ProductionRecords
            .Include(r => r.SerialNumber).ThenInclude(s => s.Product)
            .Include(r => r.WelderLogs).ThenInclude(w => w.User)
            .Include(r => r.Annotations).ThenInclude(a => a.AnnotationType)
            .Include(r => r.WorkCenter)
            .Where(r => r.WorkCenter.WorkCenterTypeId == wcTypeId.Value)
            .Where(r => r.ProductionLine.PlantId == siteId)
            .Where(r => r.Timestamp >= utcStart && r.Timestamp < utcEnd)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);

        var traceMap = await GetTraceabilityMapAsync(
            records.Select(r => r.Id).ToList(), ct);

        var recordIds = records.Select(r => r.Id).ToList();
        var inspResultMap = await GetInspectionResultMapAsync(recordIds, ct);

        return records.Select(r =>
        {
            var sn = r.SerialNumber;
            var coilHeat = BuildCoilHeatLot(sn);

            return new RollsLogEntryDto
            {
                Id = r.Id,
                Timestamp = ToLocal(r.Timestamp, tz),
                CoilHeatLot = coilHeat,
                Thickness = inspResultMap.GetValueOrDefault(r.Id),
                ShellCode = sn?.Serial ?? "",
                TankSize = sn?.Product?.TankSize,
                Welders = DistinctWelderNames(r.WelderLogs),
                Annotations = MapAnnotationBadges(r.Annotations)
            };
        }).ToList();
    }

    public async Task<List<FitupLogEntryDto>> GetFitupLogAsync(
        Guid siteId, string startDate, string endDate, CancellationToken ct = default)
    {
        var (utcStart, utcEnd, tz) = await ResolveDateRangeAsync(siteId, startDate, endDate, ct);

        var wcTypeId = await GetWcTypeIdAsync("Fitup", ct);
        if (wcTypeId == null) return new List<FitupLogEntryDto>();

        var records = await _db.ProductionRecords
            .Include(r => r.SerialNumber).ThenInclude(s => s.Product)
            .Include(r => r.WelderLogs).ThenInclude(w => w.User)
            .Include(r => r.Annotations).ThenInclude(a => a.AnnotationType)
            .Include(r => r.WorkCenter)
            .Where(r => r.WorkCenter.WorkCenterTypeId == wcTypeId.Value)
            .Where(r => r.ProductionLine.PlantId == siteId)
            .Where(r => r.Timestamp >= utcStart && r.Timestamp < utcEnd)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);

        var recordIds = records.Select(r => r.Id).ToList();
        var traceMap = await GetTraceabilityMapAsync(recordIds, ct);

        var headRelationships = new List<string> { "Head", "HeadToAssembly", "leftHead", "rightHead" };
        bool IsHeadRelationship(string rel) => headRelationships.Contains(rel, StringComparer.OrdinalIgnoreCase);

        // Fallback: for records missing head traces via ProductionRecordId,
        // look up traceability entries linked by ToSerialNumberId (assembly SN).
        var missingHeadSnIds = records
            .Where(r =>
            {
                var traces = traceMap.GetValueOrDefault(r.Id);
                return traces == null || !traces.Any(t => IsHeadRelationship(t.Relationship));
            })
            .ToDictionary(r => r.SerialNumberId, r => r.Id);

        if (missingHeadSnIds.Count > 0)
        {
            var snIds = missingHeadSnIds.Keys.ToList();
            var fallbackTraces = await _db.TraceabilityLogs
                .Include(t => t.FromSerialNumber)
                .Where(t => t.ToSerialNumberId != null && snIds.Contains(t.ToSerialNumberId.Value))
                .Where(t => headRelationships.Contains(t.Relationship))
                .ToListAsync(ct);

            foreach (var trace in fallbackTraces)
            {
                if (trace.ToSerialNumberId == null) continue;
                if (!missingHeadSnIds.TryGetValue(trace.ToSerialNumberId.Value, out var recId)) continue;

                if (!traceMap.ContainsKey(recId))
                    traceMap[recId] = new List<Models.TraceabilityLog>();
                traceMap[recId].Add(trace);
            }
        }

        return records.Select(r =>
        {
            var traces = traceMap.GetValueOrDefault(r.Id) ?? new List<Models.TraceabilityLog>();
            var heads = traces
                .Where(t => IsHeadRelationship(t.Relationship))
                .OrderBy(t => t.TankLocation)
                .Select(t => FormatHeadInfo(t))
                .ToList();
            var shells = traces
                .Where(t => t.Relationship == "ShellToAssembly" || t.Relationship == "Shell")
                .OrderBy(t => t.TankLocation)
                .Select(t => t.FromSerialNumber?.Serial ?? "")
                .ToList();

            return new FitupLogEntryDto
            {
                Id = r.Id,
                Timestamp = ToLocal(r.Timestamp, tz),
                HeadNo1 = heads.ElementAtOrDefault(0),
                HeadNo2 = heads.ElementAtOrDefault(1),
                ShellNo1 = shells.ElementAtOrDefault(0),
                ShellNo2 = shells.ElementAtOrDefault(1),
                ShellNo3 = shells.ElementAtOrDefault(2),
                AlphaCode = r.SerialNumber?.Serial,
                TankSize = r.SerialNumber?.Product?.TankSize,
                Welders = DistinctWelderNames(r.WelderLogs),
                Annotations = MapAnnotationBadges(r.Annotations)
            };
        }).ToList();
    }

    public async Task<List<HydroLogEntryDto>> GetHydroLogAsync(
        Guid siteId, string startDate, string endDate, CancellationToken ct = default)
    {
        var (utcStart, utcEnd, tz) = await ResolveDateRangeAsync(siteId, startDate, endDate, ct);

        var wcTypeId = await GetWcTypeIdAsync("Hydro", ct);
        if (wcTypeId == null) return new List<HydroLogEntryDto>();

        var records = await _db.ProductionRecords
            .Include(r => r.SerialNumber).ThenInclude(s => s.Product)
            .Include(r => r.Operator)
            .Include(r => r.WelderLogs).ThenInclude(w => w.User)
            .Include(r => r.DefectLogs)
            .Include(r => r.Annotations).ThenInclude(a => a.AnnotationType)
            .Include(r => r.WorkCenter)
            .Where(r => r.WorkCenter.WorkCenterTypeId == wcTypeId.Value)
            .Where(r => r.ProductionLine.PlantId == siteId)
            .Where(r => r.Timestamp >= utcStart && r.Timestamp < utcEnd)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);

        var recordIds = records.Select(r => r.Id).ToList();
        var traceMap = await GetTraceabilityMapAsync(recordIds, ct);
        var inspResultMap = await GetInspectionResultMapAsync(recordIds, ct);

        return records.Select(r =>
        {
            var traces = traceMap.GetValueOrDefault(r.Id) ?? new List<Models.TraceabilityLog>();
            var nameplateSn = traces
                .Where(t => t.Relationship == "NameplateToAssembly" || t.Relationship == "Nameplate")
                .Select(t => t.FromSerialNumber?.Serial)
                .FirstOrDefault();
            var alphaCode = traces
                .Where(t => t.Relationship == "hydro-marriage")
                .Select(t => t.FromSerialNumber?.Serial)
                .FirstOrDefault();

            return new HydroLogEntryDto
            {
                Id = r.Id,
                Timestamp = ToLocal(r.Timestamp, tz),
                Nameplate = nameplateSn ?? r.SerialNumber?.Serial,
                AlphaCode = alphaCode,
                TankSize = r.SerialNumber?.Product?.TankSize,
                Operator = r.Operator?.DisplayName ?? "",
                Welders = DistinctWelderNames(r.WelderLogs),
                Result = inspResultMap.GetValueOrDefault(r.Id),
                DefectCount = r.DefectLogs.Count,
                Annotations = MapAnnotationBadges(r.Annotations)
            };
        }).ToList();
    }

    public async Task<List<RtXrayLogEntryDto>> GetRtXrayLogAsync(
        Guid siteId, string startDate, string endDate, CancellationToken ct = default)
    {
        var (utcStart, utcEnd, tz) = await ResolveDateRangeAsync(siteId, startDate, endDate, ct);

        var wcTypeId = await GetWcTypeIdAsync("X-Ray", ct);
        if (wcTypeId == null) return new List<RtXrayLogEntryDto>();

        var records = await _db.ProductionRecords
            .Include(r => r.SerialNumber).ThenInclude(s => s.Product)
            .Include(r => r.Operator)
            .Include(r => r.DefectLogs).ThenInclude(d => d.DefectCode)
            .Include(r => r.Annotations).ThenInclude(a => a.AnnotationType)
            .Include(r => r.WorkCenter)
            .Where(r => r.WorkCenter.WorkCenterTypeId == wcTypeId.Value)
            .Where(r => r.ProductionLine.PlantId == siteId)
            .Where(r => r.Timestamp >= utcStart && r.Timestamp < utcEnd)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);

        var rtRecordIds = records.Select(r => r.Id).ToList();
        var rtInspResultMap = await GetInspectionResultMapAsync(rtRecordIds, ct);

        return records.Select(r => new RtXrayLogEntryDto
        {
            Id = r.Id,
            Timestamp = ToLocal(r.Timestamp, tz),
            ShellCode = r.SerialNumber?.Serial ?? "",
            TankSize = r.SerialNumber?.Product?.TankSize,
            Operator = r.Operator?.DisplayName ?? "",
            Result = rtInspResultMap.GetValueOrDefault(r.Id),
            Defects = r.DefectLogs.Any()
                ? string.Join(", ", r.DefectLogs.Select(d => d.DefectCode?.Code ?? d.DefectCodeId.ToString()[..4]))
                : null,
            Annotations = MapAnnotationBadges(r.Annotations)
        }).ToList();
    }

    public async Task<SpotXrayLogResponseDto> GetSpotXrayLogAsync(
        Guid siteId, string startDate, string endDate, CancellationToken ct = default)
    {
        var (utcStart, utcEnd, tz) = await ResolveDateRangeAsync(siteId, startDate, endDate, ct);

        var wcTypeId = await GetWcTypeIdAsync("Spot X-Ray", ct);
        if (wcTypeId == null) return new SpotXrayLogResponseDto();

        var records = await _db.ProductionRecords
            .Include(r => r.SerialNumber).ThenInclude(s => s.Product)
            .Include(r => r.Operator)
            .Include(r => r.Annotations).ThenInclude(a => a.AnnotationType)
            .Include(r => r.WorkCenter)
            .Where(r => r.WorkCenter.WorkCenterTypeId == wcTypeId.Value)
            .Where(r => r.ProductionLine.PlantId == siteId)
            .Where(r => r.Timestamp >= utcStart && r.Timestamp < utcEnd)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);

        var recordIds = records.Select(r => r.Id).ToList();

        var increments = await _db.SpotXrayIncrements
            .Where(i => recordIds.Contains(i.ManufacturingLogId))
            .ToListAsync(ct);

        var incByRecord = increments
            .GroupBy(i => i.ManufacturingLogId)
            .ToDictionary(g => g.Key, g => g.ToList());

        var entries = records.Select(r =>
        {
            var incs = incByRecord.GetValueOrDefault(r.Id) ?? new List<Models.SpotXrayIncrement>();
            var tanks = incs.Select(i => i.InspectTank).Where(t => t != null).Distinct();
            var shots = BuildShotSummary(incs);

            return new SpotXrayLogEntryDto
            {
                Id = r.Id,
                Timestamp = ToLocal(r.Timestamp, tz),
                Tanks = string.Join(", ", tanks),
                Inspected = incs.FirstOrDefault()?.InspectTank,
                TankSize = incs.FirstOrDefault()?.TankSize ?? r.SerialNumber?.Product?.TankSize,
                Operator = r.Operator?.DisplayName ?? "",
                Result = incs.FirstOrDefault()?.OverallStatus,
                Shots = shots,
                Annotations = MapAnnotationBadges(r.Annotations)
            };
        }).ToList();

        var shotCounts = ComputeShotCounts(increments, tz);

        return new SpotXrayLogResponseDto
        {
            ShotCounts = shotCounts,
            Entries = entries
        };
    }

    // --- helpers ---

    private async Task<Dictionary<Guid, string?>> GetInspectionResultMapAsync(
        List<Guid> productionRecordIds, CancellationToken ct)
    {
        if (productionRecordIds.Count == 0)
            return new Dictionary<Guid, string?>();

        var records = await _db.InspectionRecords
            .Where(i => productionRecordIds.Contains(i.ProductionRecordId) && i.ResultText != null)
            .Select(i => new { i.ProductionRecordId, i.ResultText })
            .ToListAsync(ct);

        return records
            .GroupBy(r => r.ProductionRecordId)
            .ToDictionary(g => g.Key, g => g.First().ResultText);
    }

    private async Task<Guid?> GetWcTypeIdAsync(string typeName, CancellationToken ct)
    {
        return await _db.WorkCenterTypes
            .Where(t => t.Name == typeName)
            .Select(t => (Guid?)t.Id)
            .FirstOrDefaultAsync(ct);
    }

    private async Task<(DateTime utcStart, DateTime utcEnd, TimeZoneInfo tz)> ResolveDateRangeAsync(
        Guid siteId, string startDate, string endDate, CancellationToken ct)
    {
        var tz = await GetPlantTimeZoneAsync(siteId, ct);

        if (!DateTime.TryParse(startDate, out var localStart))
            localStart = DateTime.UtcNow.Date.AddDays(-7);
        if (!DateTime.TryParse(endDate, out var localEnd))
            localEnd = DateTime.UtcNow.Date;

        var utcStart = TimeZoneInfo.ConvertTimeToUtc(localStart.Date, tz);
        var utcEnd = TimeZoneInfo.ConvertTimeToUtc(localEnd.Date.AddDays(1), tz);

        return (utcStart, utcEnd, tz);
    }

    private static DateTime ToLocal(DateTime utc, TimeZoneInfo tz)
    {
        var spec = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(spec, tz);
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

    private async Task<Dictionary<Guid, List<Models.TraceabilityLog>>> GetTraceabilityMapAsync(
        List<Guid> recordIds, CancellationToken ct)
    {
        if (recordIds.Count == 0)
            return new Dictionary<Guid, List<Models.TraceabilityLog>>();

        var traces = await _db.TraceabilityLogs
            .Include(t => t.FromSerialNumber)
            .Where(t => t.ProductionRecordId != null && recordIds.Contains(t.ProductionRecordId!.Value))
            .ToListAsync(ct);

        return traces
            .Where(t => t.ProductionRecordId.HasValue)
            .GroupBy(t => t.ProductionRecordId!.Value)
            .ToDictionary(g => g.Key, g => g.ToList());
    }

    private static List<string> DistinctWelderNames(ICollection<Models.WelderLog> welderLogs)
    {
        return welderLogs
            .GroupBy(w => w.UserId)
            .Select(g => g.First().User.DisplayName)
            .ToList();
    }

    private static string BuildCoilHeatLot(Models.SerialNumber? sn)
    {
        if (sn == null) return "";
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(sn.CoilNumber)) parts.Add($"Coil:{sn.CoilNumber}");
        if (!string.IsNullOrEmpty(sn.HeatNumber)) parts.Add($"Heat:{sn.HeatNumber}");
        if (!string.IsNullOrEmpty(sn.LotNumber)) parts.Add($"Lot:{sn.LotNumber}");
        return string.Join(" ", parts);
    }

    private static string FormatHeadInfo(Models.TraceabilityLog trace)
    {
        var sn = trace.FromSerialNumber;
        if (sn == null) return "";
        var parts = new List<string>();
        if (!string.IsNullOrEmpty(sn.CoilNumber)) parts.Add($"Coil:{sn.CoilNumber}");
        if (!string.IsNullOrEmpty(sn.HeatNumber)) parts.Add($"Heat:{sn.HeatNumber}");
        return string.Join(" ", parts);
    }

    private static List<LogAnnotationBadgeDto> MapAnnotationBadges(ICollection<Models.Annotation> annotations)
    {
        return annotations
            .OrderByDescending(a => a.AnnotationType.RequiresResolution)
            .ThenByDescending(a => a.CreatedAt)
            .Select(a => new LogAnnotationBadgeDto
            {
                Abbreviation = a.AnnotationType.Abbreviation ?? a.AnnotationType.Name[..1],
                Color = a.AnnotationType.DisplayColor ?? "#212529"
            })
            .ToList();
    }

    private static string BuildShotSummary(List<Models.SpotXrayIncrement> increments)
    {
        if (increments.Count == 0) return "";

        var seamParts = new List<string>();
        foreach (var inc in increments)
        {
            for (int seam = 1; seam <= 4; seam++)
            {
                var shotNo = seam switch
                {
                    1 => inc.Seam1ShotNo,
                    2 => inc.Seam2ShotNo,
                    3 => inc.Seam3ShotNo,
                    4 => inc.Seam4ShotNo,
                    _ => null
                };
                var shotDate = seam switch
                {
                    1 => inc.Seam1ShotDateTime,
                    2 => inc.Seam2ShotDateTime,
                    3 => inc.Seam3ShotDateTime,
                    4 => inc.Seam4ShotDateTime,
                    _ => null
                };

                if (!string.IsNullOrEmpty(shotNo))
                {
                    var dateStr = !string.IsNullOrEmpty(shotDate) ? $" ({shotDate})" : "";
                    seamParts.Add($"Seam {seam}: {shotNo}{dateStr}");
                }
            }
        }

        return string.Join(", ", seamParts);
    }

    private static List<SpotXrayShotCountDto> ComputeShotCounts(
        List<Models.SpotXrayIncrement> allIncrements, TimeZoneInfo tz)
    {
        var shotsByDate = new Dictionary<string, int>();

        foreach (var inc in allIncrements)
        {
            for (int seam = 1; seam <= 4; seam++)
            {
                var shotDateStr = seam switch
                {
                    1 => inc.Seam1ShotDateTime,
                    2 => inc.Seam2ShotDateTime,
                    3 => inc.Seam3ShotDateTime,
                    4 => inc.Seam4ShotDateTime,
                    _ => null
                };
                var shotNo = seam switch
                {
                    1 => inc.Seam1ShotNo,
                    2 => inc.Seam2ShotNo,
                    3 => inc.Seam3ShotNo,
                    4 => inc.Seam4ShotNo,
                    _ => null
                };

                if (string.IsNullOrEmpty(shotNo)) continue;

                string dateKey;
                if (!string.IsNullOrEmpty(shotDateStr) && DateTime.TryParse(shotDateStr, out var shotDt))
                {
                    var local = TimeZoneInfo.ConvertTimeFromUtc(shotDt.Kind == DateTimeKind.Utc ? shotDt : DateTime.SpecifyKind(shotDt, DateTimeKind.Utc), tz);
                    dateKey = local.ToString("MM/dd/yyyy");
                }
                else if (inc.CreatedDateTime.HasValue)
                {
                    var local = TimeZoneInfo.ConvertTimeFromUtc(
                        inc.CreatedDateTime.Value.Kind == DateTimeKind.Utc
                            ? inc.CreatedDateTime.Value
                            : DateTime.SpecifyKind(inc.CreatedDateTime.Value, DateTimeKind.Utc), tz);
                    dateKey = local.ToString("MM/dd/yyyy");
                }
                else
                {
                    continue;
                }

                shotsByDate.TryGetValue(dateKey, out var count);
                shotsByDate[dateKey] = count + 1;
            }
        }

        return shotsByDate
            .OrderBy(kv => kv.Key)
            .Select(kv => new SpotXrayShotCountDto { Date = kv.Key, Count = kv.Value })
            .ToList();
    }
}
