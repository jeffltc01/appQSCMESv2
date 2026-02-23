using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class SpotXrayService : ISpotXrayService
{
    private readonly MesDbContext _db;

    private static readonly Dictionary<int, int> MaxIncrementSize = new()
    {
        [120] = 8, [250] = 6, [320] = 6, [500] = 5,
        [1000] = 4, [1450] = 4, [1990] = 4
    };

    public SpotXrayService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<SpotXrayLaneQueuesDto> GetLaneQueuesAsync(string siteCode, CancellationToken ct = default)
    {
        var fetchResult = await FetchLaneQueueData(siteCode, ct);
        if (fetchResult == null) return new SpotXrayLaneQueuesDto();

        var (laneGroups, shellsByAssembly, draftCounts) = fetchResult.Value;

        var result = new SpotXrayLaneQueuesDto();
        foreach (var (laneName, items) in laneGroups.OrderBy(kv => kv.Key))
        {
            var sorted = items.OrderBy(x => x.Record.Timestamp).ToList();
            var tanks = new List<SpotXrayQueueTankDto>();

            for (int i = 0; i < sorted.Count; i++)
            {
                var (rec, asm) = sorted[i];
                var tankSize = asm.Product?.TankSize ?? 0;
                var weldType = rec.Asset?.Name ?? "Unknown";

                var welderLogs = rec.WelderLogs
                    .OrderBy(w => w.Characteristic?.Name ?? "")
                    .ToList();
                var welderNames = welderLogs.Select(w => w.User?.DisplayName ?? "Unknown").ToList();
                var welderIds = welderLogs.Select(w => w.UserId).ToList();

                var (sizeChanged, welderChanged) = i > 0
                    ? DetectChanges(tankSize, welderIds, sorted[i - 1])
                    : (false, false);

                var shells = shellsByAssembly.GetValueOrDefault(asm.Id) ?? new List<string>();

                tanks.Add(new SpotXrayQueueTankDto
                {
                    Position = i + 1,
                    AssemblySerialNumberId = asm.Id,
                    AlphaCode = asm.Serial,
                    ShellSerials = shells,
                    TankSize = tankSize,
                    WeldType = weldType,
                    WelderNames = welderNames,
                    WelderIds = welderIds,
                    SizeChanged = sizeChanged,
                    WelderChanged = welderChanged
                });
            }

            draftCounts.TryGetValue(laneName, out var draftCount);
            result.Lanes.Add(new SpotXrayLaneDto
            {
                LaneName = laneName,
                DraftCount = draftCount,
                Tanks = tanks
            });
        }

        return result;
    }

    private async Task<(
        Dictionary<string, List<(ProductionRecord Record, SerialNumber Assembly)>> LaneGroups,
        Dictionary<Guid, List<string>> ShellsByAssembly,
        Dictionary<string, int> DraftCounts
    )?> FetchLaneQueueData(string siteCode, CancellationToken ct)
    {
        var plant = await _db.Plants.FirstOrDefaultAsync(p => p.Code == siteCode, ct)
            ?? throw new InvalidOperationException($"Plant {siteCode} not found");

        var rsType = await _db.WorkCenterTypes.FirstOrDefaultAsync(t => t.Name == "Round Seam", ct);
        if (rsType == null) return null;

        var plantLineIds = await _db.ProductionLines
            .Where(pl => pl.PlantId == plant.Id)
            .Select(pl => pl.Id)
            .ToListAsync(ct);

        var rsRecords = await _db.ProductionRecords
            .Include(r => r.Asset)
            .Include(r => r.WelderLogs).ThenInclude(w => w.User)
            .Include(r => r.WelderLogs).ThenInclude(w => w.Characteristic)
            .Include(r => r.SerialNumber).ThenInclude(s => s!.Product)
            .Where(r => r.WorkCenter.WorkCenterTypeId == rsType.Id
                     && plantLineIds.Contains(r.ProductionLineId))
            .OrderBy(r => r.Timestamp)
            .ToListAsync(ct);

        if (rsRecords.Count == 0) return null;

        var shellSnIds = rsRecords.Select(r => (Guid?)r.SerialNumberId).Distinct().ToList();
        var shellToAssembly = await _db.TraceabilityLogs
            .Where(t => shellSnIds.Contains(t.FromSerialNumberId)
                     && (t.Relationship == "ShellToAssembly" || t.Relationship == "shell")
                     && t.ToSerialNumberId.HasValue)
            .Select(t => new { FromId = t.FromSerialNumberId!.Value, AssemblyId = t.ToSerialNumberId!.Value })
            .ToListAsync(ct);

        var assemblyIds = shellToAssembly.Select(x => x.AssemblyId).Distinct().ToList();
        var assemblies = await _db.SerialNumbers
            .Include(s => s.Product)
            .Where(s => assemblyIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, ct);

        var assemblyIdNullables = assemblyIds.Select(id => (Guid?)id).ToList();
        var assemblyShells = await _db.TraceabilityLogs
            .Where(t => assemblyIdNullables.Contains(t.ToSerialNumberId)
                     && (t.Relationship == "ShellToAssembly" || t.Relationship == "shell")
                     && t.FromSerialNumberId.HasValue)
            .Join(_db.SerialNumbers, t => t.FromSerialNumberId!.Value, s => s.Id,
                  (t, s) => new { AssemblyId = t.ToSerialNumberId!.Value, ShellSerial = s.Serial })
            .ToListAsync(ct);

        var shellsByAssembly = assemblyShells
            .GroupBy(x => x.AssemblyId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ShellSerial).OrderBy(s => s).ToList());

        var usedAssemblyIds = await _db.SpotXrayIncrementTanks
            .Select(t => t.SerialNumberId)
            .ToListAsync(ct);
        var usedSet = usedAssemblyIds.ToHashSet();

        var shellToAssemblyMap = shellToAssembly.ToDictionary(x => x.FromId, x => x.AssemblyId);

        var assemblyRsRecords = new Dictionary<Guid, ProductionRecord>();
        foreach (var r in rsRecords)
        {
            if (shellToAssemblyMap.TryGetValue(r.SerialNumberId, out var asmId))
                assemblyRsRecords.TryAdd(asmId, r);
        }

        var laneGroups = new Dictionary<string, List<(ProductionRecord Record, SerialNumber Assembly)>>();
        foreach (var (asmId, rec) in assemblyRsRecords)
        {
            if (usedSet.Contains(asmId)) continue;
            if (!assemblies.TryGetValue(asmId, out var asm)) continue;

            var lane = rec.Asset?.LaneName ?? "Default";
            if (!laneGroups.ContainsKey(lane))
                laneGroups[lane] = new List<(ProductionRecord Record, SerialNumber Assembly)>();
            laneGroups[lane].Add((rec, asm));
        }

        var draftCounts = await _db.SpotXrayIncrements
            .Where(i => i.IsDraft && i.ProductionRecord.ProductionLine.PlantId == plant.Id)
            .GroupBy(i => i.LaneNo)
            .Select(g => new { Lane = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.Lane, x => x.Count, ct);

        return (laneGroups, shellsByAssembly, draftCounts);
    }

    private static (bool SizeChanged, bool WelderChanged) DetectChanges(
        int currentTankSize, List<Guid> currentWelderIds,
        (ProductionRecord Record, SerialNumber Assembly) previous)
    {
        var prevSize = previous.Assembly.Product?.TankSize ?? 0;
        var sizeChanged = currentTankSize != prevSize;

        var prevWelderIds = previous.Record.WelderLogs
            .OrderBy(w => w.Characteristic?.Name ?? "")
            .Select(w => w.UserId)
            .ToList();
        var welderChanged = !currentWelderIds.SequenceEqual(prevWelderIds);

        return (sizeChanged, welderChanged);
    }

    public async Task<CreateSpotXrayIncrementsResponse> CreateIncrementsAsync(
        CreateSpotXrayIncrementsRequest request, CancellationToken ct = default)
    {
        var plant = await _db.Plants.FirstOrDefaultAsync(
            p => p.Code == request.SiteCode, ct)
            ?? throw new InvalidOperationException("Plant not found");

        var lanes = await GetLaneQueuesAsync(request.SiteCode, ct);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var counter = await _db.XrayShotCounters
            .FirstOrDefaultAsync(c => c.PlantId == plant.Id && c.CounterDate == today, ct);
        if (counter == null)
        {
            counter = new XrayShotCounter
            {
                Id = Guid.NewGuid(),
                PlantId = plant.Id,
                CounterDate = today,
                LastShotNumber = 0,
                LastIncrementNumber = 0
            };
            _db.XrayShotCounters.Add(counter);
        }

        var response = new CreateSpotXrayIncrementsResponse();

        foreach (var sel in request.LaneSelections)
        {
            if (sel.SelectedPositions.Count == 0) continue;

            var lane = lanes.Lanes.FirstOrDefault(l => l.LaneName == sel.LaneName);
            if (lane == null) throw new InvalidOperationException($"Lane {sel.LaneName} not found");

            var positions = sel.SelectedPositions.OrderBy(p => p).ToList();
            ValidateSelection(positions, lane.Tanks);

            var selectedTanks = lane.Tanks.Where(t => positions.Contains(t.Position)).OrderBy(t => t.Position).ToList();
            var tankSize = selectedTanks[0].TankSize;

            counter.LastIncrementNumber++;
            var dateStr = today.ToString("yyMMdd");
            var incrementNo = $"{dateStr}{counter.LastIncrementNumber:D4}-{sel.LaneName.Replace(" ", "")}";

            var productionRecord = new ProductionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = selectedTanks[0].AssemblySerialNumberId,
                WorkCenterId = request.WorkCenterId,
                AssetId = null,
                ProductionLineId = request.ProductionLineId,
                OperatorId = request.OperatorId,
                Timestamp = DateTime.UtcNow
            };
            _db.ProductionRecords.Add(productionRecord);

            var increment = new SpotXrayIncrement
            {
                Id = Guid.NewGuid(),
                ManufacturingLogId = productionRecord.Id,
                IncrementNo = incrementNo,
                OverallStatus = "Pending",
                LaneNo = sel.LaneName,
                IsDraft = true,
                TankSize = tankSize,
                Welder1Id = selectedTanks[0].WelderIds.Count > 0 ? selectedTanks[0].WelderIds[0] : null,
                Welder2Id = selectedTanks[0].WelderIds.Count > 1 ? selectedTanks[0].WelderIds[1] : null,
                Welder3Id = selectedTanks[0].WelderIds.Count > 2 ? selectedTanks[0].WelderIds[2] : null,
                Welder4Id = selectedTanks[0].WelderIds.Count > 3 ? selectedTanks[0].WelderIds[3] : null,
                CreatedByUserId = request.OperatorId,
                CreatedDateTime = DateTime.UtcNow
            };
            _db.SpotXrayIncrements.Add(increment);

            for (int i = 0; i < selectedTanks.Count; i++)
            {
                _db.SpotXrayIncrementTanks.Add(new SpotXrayIncrementTank
                {
                    Id = Guid.NewGuid(),
                    SpotXrayIncrementId = increment.Id,
                    SerialNumberId = selectedTanks[i].AssemblySerialNumberId,
                    Position = i + 1
                });
            }

            response.Increments.Add(new SpotXrayIncrementSummaryDto
            {
                Id = increment.Id,
                IncrementNo = incrementNo,
                LaneNo = sel.LaneName,
                TankSize = tankSize,
                OverallStatus = "Pending",
                IsDraft = true
            });
        }

        await _db.SaveChangesAsync(ct);
        return response;
    }

    public async Task<SpotXrayIncrementDetailDto?> GetIncrementAsync(Guid incrementId, CancellationToken ct = default)
    {
        var inc = await _db.SpotXrayIncrements
            .Include(i => i.IncrementTanks).ThenInclude(t => t.SerialNumber)
            .Include(i => i.InspectTankSn)
            .Include(i => i.Welder1)
            .Include(i => i.Welder2)
            .Include(i => i.Welder3)
            .Include(i => i.Welder4)
            .Include(i => i.Seam1Trace1Tank)
            .Include(i => i.Seam1Trace2Tank)
            .Include(i => i.Seam2Trace1Tank)
            .Include(i => i.Seam2Trace2Tank)
            .Include(i => i.Seam3Trace1Tank)
            .Include(i => i.Seam3Trace2Tank)
            .Include(i => i.Seam4Trace1Tank)
            .Include(i => i.Seam4Trace2Tank)
            .FirstOrDefaultAsync(i => i.Id == incrementId, ct);

        if (inc == null) return null;

        var seamCount = (inc.TankSize ?? 0) switch
        {
            <= 500 => 2,
            <= 1000 => 3,
            _ => 4
        };

        // Get shell serials for each tank in the increment
        var tankSnIds = inc.IncrementTanks.Select(t => (Guid?)t.SerialNumberId).ToList();
        var shellsByAssembly = await _db.TraceabilityLogs
            .Where(t => tankSnIds.Contains(t.ToSerialNumberId)
                     && (t.Relationship == "ShellToAssembly" || t.Relationship == "shell")
                     && t.FromSerialNumberId.HasValue)
            .Join(_db.SerialNumbers, t => t.FromSerialNumberId!.Value, s => s.Id,
                  (t, s) => new { AssemblyId = t.ToSerialNumberId!.Value, ShellSerial = s.Serial })
            .ToListAsync(ct);

        var shellsMap = shellsByAssembly
            .GroupBy(x => x.AssemblyId)
            .ToDictionary(g => g.Key, g => g.Select(x => x.ShellSerial).OrderBy(s => s).ToList());

        var tanks = inc.IncrementTanks
            .OrderBy(t => t.Position)
            .Select(t => new SpotXrayIncrementTankDto
            {
                SerialNumberId = t.SerialNumberId,
                AlphaCode = t.SerialNumber.Serial,
                ShellSerials = shellsMap.GetValueOrDefault(t.SerialNumberId) ?? new(),
                Position = t.Position
            }).ToList();

        var seams = new List<SpotXraySeamDto>();
        for (int s = 1; s <= seamCount; s++)
        {
            seams.Add(BuildSeamDto(inc, s));
        }

        return new SpotXrayIncrementDetailDto
        {
            Id = inc.Id,
            IncrementNo = inc.IncrementNo,
            OverallStatus = inc.OverallStatus,
            LaneNo = inc.LaneNo,
            IsDraft = inc.IsDraft,
            TankSize = inc.TankSize,
            SeamCount = seamCount,
            InspectTankId = inc.InspectTankId,
            InspectTankAlpha = inc.InspectTankSn?.Serial,
            Tanks = tanks,
            Seams = seams,
            CreatedDateTime = inc.CreatedDateTime
        };
    }

    public async Task<List<SpotXrayIncrementSummaryDto>> GetRecentIncrementsAsync(
        string siteCode, CancellationToken ct = default)
    {
        var plant = await _db.Plants.FirstOrDefaultAsync(p => p.Code == siteCode, ct);
        if (plant == null) return new();

        var cutoff = DateTime.UtcNow.AddHours(-24);
        return await _db.SpotXrayIncrements
            .Where(i => i.ProductionRecord.ProductionLine.PlantId == plant.Id
                     && i.CreatedDateTime >= cutoff)
            .OrderByDescending(i => i.CreatedDateTime)
            .Select(i => new SpotXrayIncrementSummaryDto
            {
                Id = i.Id,
                IncrementNo = i.IncrementNo,
                LaneNo = i.LaneNo,
                TankSize = i.TankSize,
                OverallStatus = i.OverallStatus,
                IsDraft = i.IsDraft
            }).ToListAsync(ct);
    }

    public async Task<SpotXrayIncrementDetailDto> SaveResultsAsync(
        Guid incrementId, SaveSpotXrayResultsRequest request, CancellationToken ct = default)
    {
        var inc = await _db.SpotXrayIncrements
            .Include(i => i.IncrementTanks)
            .Include(i => i.ProductionRecord)
            .FirstOrDefaultAsync(i => i.Id == incrementId, ct)
            ?? throw new InvalidOperationException("Increment not found");

        inc.InspectTankId = request.InspectTankId;
        if (request.InspectTankId.HasValue)
        {
            var inspectSn = await _db.SerialNumbers.FindAsync(new object[] { request.InspectTankId.Value }, ct);
            inc.InspectTank = inspectSn?.Serial;
        }

        foreach (var seam in request.Seams)
        {
            ApplySeamData(inc, seam);
        }

        inc.IsDraft = request.IsDraft;
        inc.ModifiedByUserId = request.OperatorId;
        inc.ModifiedDateTime = DateTime.UtcNow;

        if (!request.IsDraft)
        {
            inc.OverallStatus = ComputeOverallStatus(inc);
            await CreateInspectionRecordsForIncrement(inc, request.OperatorId, ct);
        }

        await _db.SaveChangesAsync(ct);
        return (await GetIncrementAsync(incrementId, ct))!;
    }

    private async Task CreateInspectionRecordsForIncrement(
        SpotXrayIncrement inc, Guid operatorId, CancellationToken ct)
    {
        var prodRecord = inc.ProductionRecord;
        var wcpl = await _db.WorkCenterProductionLines
            .AsNoTracking()
            .FirstOrDefaultAsync(w => w.WorkCenterId == prodRecord.WorkCenterId
                                   && w.ProductionLineId == prodRecord.ProductionLineId, ct);
        if (wcpl == null) return;

        var controlPlan = await _db.ControlPlans
            .AsNoTracking()
            .FirstOrDefaultAsync(cp => cp.WorkCenterProductionLineId == wcpl.Id
                                    && cp.IsGateCheck && cp.IsEnabled, ct);
        if (controlPlan == null) return;

        var existing = await _db.InspectionRecords
            .Where(ir => ir.SpotIncrementId == inc.Id)
            .ToListAsync(ct);
        if (existing.Count > 0)
            _db.InspectionRecords.RemoveRange(existing);

        var now = DateTime.UtcNow;
        foreach (var tank in inc.IncrementTanks)
        {
            string resultText;
            if (inc.OverallStatus == "Reject")
                resultText = "Reject";
            else if (inc.OverallStatus == "Accept-Scrap" && tank.SerialNumberId == inc.InspectTankId)
                resultText = "Reject";
            else
                resultText = "Accept";

            _db.InspectionRecords.Add(new InspectionRecord
            {
                Id = Guid.NewGuid(),
                SerialNumberId = tank.SerialNumberId,
                ProductionRecordId = inc.ManufacturingLogId,
                WorkCenterId = prodRecord.WorkCenterId,
                OperatorId = operatorId,
                Timestamp = now,
                ControlPlanId = controlPlan.Id,
                ResultText = resultText,
                SpotIncrementId = inc.Id
            });
        }
    }

    public async Task<NextShotNumberResponse> GetNextShotNumberAsync(Guid plantId, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var counter = await _db.XrayShotCounters
            .FirstOrDefaultAsync(c => c.PlantId == plantId && c.CounterDate == today, ct);

        if (counter == null)
        {
            counter = new XrayShotCounter
            {
                Id = Guid.NewGuid(),
                PlantId = plantId,
                CounterDate = today,
                LastShotNumber = 0,
                LastIncrementNumber = 0
            };
            _db.XrayShotCounters.Add(counter);
        }

        counter.LastShotNumber++;
        await _db.SaveChangesAsync(ct);

        return new NextShotNumberResponse { ShotNumber = counter.LastShotNumber };
    }

    private static void ValidateSelection(List<int> positions, List<SpotXrayQueueTankDto> tanks)
    {
        if (positions.Count == 0)
            throw new InvalidOperationException("No tanks selected");

        // Sequential check
        for (int i = 1; i < positions.Count; i++)
        {
            if (positions[i] != positions[i - 1] + 1)
                throw new InvalidOperationException("Selected tanks must be sequential");
        }

        var selected = tanks.Where(t => positions.Contains(t.Position)).ToList();
        if (selected.Count != positions.Count)
            throw new InvalidOperationException("Invalid position(s) selected");

        // Same tank size
        var firstSize = selected[0].TankSize;
        if (selected.Any(t => t.TankSize != firstSize))
            throw new InvalidOperationException("All tanks in an increment must be the same size");

        // Same welders
        var firstWelders = selected[0].WelderIds;
        if (selected.Any(t => !t.WelderIds.SequenceEqual(firstWelders)))
            throw new InvalidOperationException("All tanks in an increment must have the same welders");

        // Max size check
        if (MaxIncrementSize.TryGetValue(firstSize, out var maxSize) && selected.Count > maxSize)
            throw new InvalidOperationException($"Max increment size for {firstSize} gal is {maxSize}");
    }

    private static string ComputeOverallStatus(SpotXrayIncrement inc)
    {
        var seamCount = (inc.TankSize ?? 0) switch { <= 500 => 2, <= 1000 => 3, _ => 4 };
        bool anyReject = false;
        bool anyFinalReject = false;
        bool anyIncomplete = false;

        for (int s = 1; s <= seamCount; s++)
        {
            var (result, trc1Result, trc2Result, finalResult) = GetSeamResults(inc, s);

            if (string.IsNullOrEmpty(result))
            {
                anyIncomplete = true;
                continue;
            }

            if (result == "Accept")
                continue;

            // Initial is Reject — trace flow must resolve it
            if (string.IsNullOrEmpty(trc1Result)) { anyReject = true; continue; }
            if (trc1Result == "Reject") { anyReject = true; continue; }

            // Trace 1 Accept → need Trace 2
            if (string.IsNullOrEmpty(trc2Result)) { anyIncomplete = true; continue; }
            if (trc2Result == "Reject") { anyReject = true; continue; }

            // Trace 2 Accept → need Final
            if (string.IsNullOrEmpty(finalResult)) { anyIncomplete = true; continue; }
            if (finalResult == "Reject") { anyFinalReject = true; continue; }

            // Final Accept → seam resolved
        }

        if (anyReject) return "Reject";
        if (anyIncomplete) return "Pending";
        if (anyFinalReject) return "Accept-Scrap";
        return "Accept";
    }

    private static (string?, string?, string?, string?) GetSeamResults(SpotXrayIncrement inc, int seam)
    {
        return seam switch
        {
            1 => (inc.Seam1Result, inc.Seam1Trace1Result, inc.Seam1Trace2Result, inc.Seam1FinalResult),
            2 => (inc.Seam2Result, inc.Seam2Trace1Result, inc.Seam2Trace2Result, inc.Seam2FinalResult),
            3 => (inc.Seam3Result, inc.Seam3Trace1Result, inc.Seam3Trace2Result, inc.Seam3FinalResult),
            4 => (inc.Seam4Result, inc.Seam4Trace1Result, inc.Seam4Trace2Result, inc.Seam4FinalResult),
            _ => (null, null, null, null)
        };
    }

    private static readonly Action<SpotXrayIncrement, SaveSpotXraySeamDto, DateTime>[] SeamAppliers =
    [
        (inc, s, now) =>
        {
            inc.Seam1ShotNo = s.ShotNo;   if (!string.IsNullOrEmpty(s.ShotNo)) inc.Seam1ShotDateTime = now;
            inc.Seam1Result = s.Result;
            inc.Seam1Trace1ShotNo = s.Trace1ShotNo; if (!string.IsNullOrEmpty(s.Trace1ShotNo)) inc.Seam1Trace1DateTime = now;
            inc.Seam1Trace1TankId = s.Trace1TankId;  inc.Seam1Trace1Result = s.Trace1Result;
            inc.Seam1Trace2ShotNo = s.Trace2ShotNo; if (!string.IsNullOrEmpty(s.Trace2ShotNo)) inc.Seam1Trace2DateTime = now;
            inc.Seam1Trace2TankId = s.Trace2TankId;  inc.Seam1Trace2Result = s.Trace2Result;
            inc.Seam1FinalShotNo = s.FinalShotNo;   if (!string.IsNullOrEmpty(s.FinalShotNo)) inc.Seam1FinalDateTime = now;
            inc.Seam1FinalResult = s.FinalResult;
        },
        (inc, s, now) =>
        {
            inc.Seam2ShotNo = s.ShotNo;   if (!string.IsNullOrEmpty(s.ShotNo)) inc.Seam2ShotDateTime = now;
            inc.Seam2Result = s.Result;
            inc.Seam2Trace1ShotNo = s.Trace1ShotNo; if (!string.IsNullOrEmpty(s.Trace1ShotNo)) inc.Seam2Trace1DateTime = now;
            inc.Seam2Trace1TankId = s.Trace1TankId;  inc.Seam2Trace1Result = s.Trace1Result;
            inc.Seam2Trace2ShotNo = s.Trace2ShotNo; if (!string.IsNullOrEmpty(s.Trace2ShotNo)) inc.Seam2Trace2DateTime = now;
            inc.Seam2Trace2TankId = s.Trace2TankId;  inc.Seam2Trace2Result = s.Trace2Result;
            inc.Seam2FinalShotNo = s.FinalShotNo;   if (!string.IsNullOrEmpty(s.FinalShotNo)) inc.Seam2FinalDateTime = now;
            inc.Seam2FinalResult = s.FinalResult;
        },
        (inc, s, now) =>
        {
            inc.Seam3ShotNo = s.ShotNo;   if (!string.IsNullOrEmpty(s.ShotNo)) inc.Seam3ShotDateTime = now;
            inc.Seam3Result = s.Result;
            inc.Seam3Trace1ShotNo = s.Trace1ShotNo; if (!string.IsNullOrEmpty(s.Trace1ShotNo)) inc.Seam3Trace1DateTime = now;
            inc.Seam3Trace1TankId = s.Trace1TankId;  inc.Seam3Trace1Result = s.Trace1Result;
            inc.Seam3Trace2ShotNo = s.Trace2ShotNo; if (!string.IsNullOrEmpty(s.Trace2ShotNo)) inc.Seam3Trace2DateTime = now;
            inc.Seam3Trace2TankId = s.Trace2TankId;  inc.Seam3Trace2Result = s.Trace2Result;
            inc.Seam3FinalShotNo = s.FinalShotNo;   if (!string.IsNullOrEmpty(s.FinalShotNo)) inc.Seam3FinalDateTime = now;
            inc.Seam3FinalResult = s.FinalResult;
        },
        (inc, s, now) =>
        {
            inc.Seam4ShotNo = s.ShotNo;   if (!string.IsNullOrEmpty(s.ShotNo)) inc.Seam4ShotDateTime = now;
            inc.Seam4Result = s.Result;
            inc.Seam4Trace1ShotNo = s.Trace1ShotNo; if (!string.IsNullOrEmpty(s.Trace1ShotNo)) inc.Seam4Trace1DateTime = now;
            inc.Seam4Trace1TankId = s.Trace1TankId;  inc.Seam4Trace1Result = s.Trace1Result;
            inc.Seam4Trace2ShotNo = s.Trace2ShotNo; if (!string.IsNullOrEmpty(s.Trace2ShotNo)) inc.Seam4Trace2DateTime = now;
            inc.Seam4Trace2TankId = s.Trace2TankId;  inc.Seam4Trace2Result = s.Trace2Result;
            inc.Seam4FinalShotNo = s.FinalShotNo;   if (!string.IsNullOrEmpty(s.FinalShotNo)) inc.Seam4FinalDateTime = now;
            inc.Seam4FinalResult = s.FinalResult;
        },
    ];

    private static void ApplySeamData(SpotXrayIncrement inc, SaveSpotXraySeamDto seam)
    {
        var idx = seam.SeamNumber - 1;
        if (idx >= 0 && idx < SeamAppliers.Length)
            SeamAppliers[idx](inc, seam, DateTime.UtcNow);
    }

    private static readonly Func<SpotXrayIncrement, SpotXraySeamDto>[] SeamBuilders =
    [
        inc => new SpotXraySeamDto
        {
            SeamNumber = 1, WelderName = inc.Welder1?.DisplayName, WelderId = inc.Welder1Id,
            ShotNo = inc.Seam1ShotNo, ShotDateTime = inc.Seam1ShotDateTime, Result = inc.Seam1Result,
            Trace1ShotNo = inc.Seam1Trace1ShotNo, Trace1DateTime = inc.Seam1Trace1DateTime,
            Trace1TankId = inc.Seam1Trace1TankId, Trace1TankAlpha = inc.Seam1Trace1Tank?.Serial, Trace1Result = inc.Seam1Trace1Result,
            Trace2ShotNo = inc.Seam1Trace2ShotNo, Trace2DateTime = inc.Seam1Trace2DateTime,
            Trace2TankId = inc.Seam1Trace2TankId, Trace2TankAlpha = inc.Seam1Trace2Tank?.Serial, Trace2Result = inc.Seam1Trace2Result,
            FinalShotNo = inc.Seam1FinalShotNo, FinalDateTime = inc.Seam1FinalDateTime, FinalResult = inc.Seam1FinalResult
        },
        inc => new SpotXraySeamDto
        {
            SeamNumber = 2, WelderName = inc.Welder2?.DisplayName, WelderId = inc.Welder2Id,
            ShotNo = inc.Seam2ShotNo, ShotDateTime = inc.Seam2ShotDateTime, Result = inc.Seam2Result,
            Trace1ShotNo = inc.Seam2Trace1ShotNo, Trace1DateTime = inc.Seam2Trace1DateTime,
            Trace1TankId = inc.Seam2Trace1TankId, Trace1TankAlpha = inc.Seam2Trace1Tank?.Serial, Trace1Result = inc.Seam2Trace1Result,
            Trace2ShotNo = inc.Seam2Trace2ShotNo, Trace2DateTime = inc.Seam2Trace2DateTime,
            Trace2TankId = inc.Seam2Trace2TankId, Trace2TankAlpha = inc.Seam2Trace2Tank?.Serial, Trace2Result = inc.Seam2Trace2Result,
            FinalShotNo = inc.Seam2FinalShotNo, FinalDateTime = inc.Seam2FinalDateTime, FinalResult = inc.Seam2FinalResult
        },
        inc => new SpotXraySeamDto
        {
            SeamNumber = 3, WelderName = inc.Welder3?.DisplayName, WelderId = inc.Welder3Id,
            ShotNo = inc.Seam3ShotNo, ShotDateTime = inc.Seam3ShotDateTime, Result = inc.Seam3Result,
            Trace1ShotNo = inc.Seam3Trace1ShotNo, Trace1DateTime = inc.Seam3Trace1DateTime,
            Trace1TankId = inc.Seam3Trace1TankId, Trace1TankAlpha = inc.Seam3Trace1Tank?.Serial, Trace1Result = inc.Seam3Trace1Result,
            Trace2ShotNo = inc.Seam3Trace2ShotNo, Trace2DateTime = inc.Seam3Trace2DateTime,
            Trace2TankId = inc.Seam3Trace2TankId, Trace2TankAlpha = inc.Seam3Trace2Tank?.Serial, Trace2Result = inc.Seam3Trace2Result,
            FinalShotNo = inc.Seam3FinalShotNo, FinalDateTime = inc.Seam3FinalDateTime, FinalResult = inc.Seam3FinalResult
        },
        inc => new SpotXraySeamDto
        {
            SeamNumber = 4, WelderName = inc.Welder4?.DisplayName, WelderId = inc.Welder4Id,
            ShotNo = inc.Seam4ShotNo, ShotDateTime = inc.Seam4ShotDateTime, Result = inc.Seam4Result,
            Trace1ShotNo = inc.Seam4Trace1ShotNo, Trace1DateTime = inc.Seam4Trace1DateTime,
            Trace1TankId = inc.Seam4Trace1TankId, Trace1TankAlpha = inc.Seam4Trace1Tank?.Serial, Trace1Result = inc.Seam4Trace1Result,
            Trace2ShotNo = inc.Seam4Trace2ShotNo, Trace2DateTime = inc.Seam4Trace2DateTime,
            Trace2TankId = inc.Seam4Trace2TankId, Trace2TankAlpha = inc.Seam4Trace2Tank?.Serial, Trace2Result = inc.Seam4Trace2Result,
            FinalShotNo = inc.Seam4FinalShotNo, FinalDateTime = inc.Seam4FinalDateTime, FinalResult = inc.Seam4FinalResult
        },
    ];

    private static SpotXraySeamDto BuildSeamDto(SpotXrayIncrement inc, int seamNumber)
    {
        var idx = seamNumber - 1;
        return (idx >= 0 && idx < SeamBuilders.Length)
            ? SeamBuilders[idx](inc)
            : new SpotXraySeamDto { SeamNumber = seamNumber };
    }
}
