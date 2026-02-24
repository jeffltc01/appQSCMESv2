using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class DemoShellFlowService : IDemoShellFlowService
{
    private readonly MesDbContext _db;

    public DemoShellFlowService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<DemoShellCurrentDto> GetCurrentAsync(Guid workCenterId, Guid callerUserId, CancellationToken ct = default)
    {
        var (caller, stage) = await ValidateAndResolveContextAsync(workCenterId, callerUserId, ct);
        return await GetCurrentForStageAsync(caller.DefaultSiteId, stage, caller.Id, autoCreateAtRolls: true, ct);
    }

    public async Task<DemoShellCurrentDto> AdvanceAsync(Guid workCenterId, Guid callerUserId, CancellationToken ct = default)
    {
        var (caller, stage) = await ValidateAndResolveContextAsync(workCenterId, callerUserId, ct);
        var now = DateTime.UtcNow;

        if (stage == DemoShellStage.Rolls)
        {
            var current = await EnsureCurrentAtRollsAsync(caller.DefaultSiteId, caller.Id, ct);
            current.CurrentStage = DemoShellStage.LongSeam;
            current.StageEnteredAtUtc = now;

            await CreateNextRollsShellAsync(caller.DefaultSiteId, caller.Id, ct);
            await _db.SaveChangesAsync(ct);
        }
        else if (stage == DemoShellStage.LongSeam)
        {
            var current = await _db.DemoShellFlows
                .Where(x => x.PlantId == caller.DefaultSiteId && x.CurrentStage == DemoShellStage.LongSeam)
                .OrderBy(x => x.ShellNumber)
                .FirstOrDefaultAsync(ct);

            if (current is not null)
            {
                current.CurrentStage = DemoShellStage.LongSeamInspection;
                current.StageEnteredAtUtc = now;
                await _db.SaveChangesAsync(ct);
            }
        }
        else if (stage == DemoShellStage.LongSeamInspection)
        {
            var current = await _db.DemoShellFlows
                .Where(x => x.PlantId == caller.DefaultSiteId && x.CurrentStage == DemoShellStage.LongSeamInspection)
                .OrderBy(x => x.ShellNumber)
                .FirstOrDefaultAsync(ct);

            if (current is not null)
            {
                current.CurrentStage = DemoShellStage.Completed;
                current.StageEnteredAtUtc = now;
                current.CompletedAtUtc = now;
                await _db.SaveChangesAsync(ct);
            }
        }
        else
        {
            throw new ArgumentException("Work center is not supported for demo shell flow.");
        }

        return await GetCurrentForStageAsync(caller.DefaultSiteId, stage, caller.Id, autoCreateAtRolls: true, ct);
    }

    private async Task<(User Caller, string Stage)> ValidateAndResolveContextAsync(Guid workCenterId, Guid callerUserId, CancellationToken ct)
    {
        var caller = await _db.Users
            .AsNoTracking()
            .Where(u => u.Id == callerUserId && u.IsActive)
            .Select(u => new User
            {
                Id = u.Id,
                RoleTier = u.RoleTier,
                DemoMode = u.DemoMode,
                DefaultSiteId = u.DefaultSiteId,
            })
            .FirstOrDefaultAsync(ct);

        if (caller is null)
            throw new UnauthorizedAccessException("Caller is not valid.");

        if (caller.RoleTier > 1.0m)
            throw new UnauthorizedAccessException("Demo shell flow is restricted to Admin users.");

        if (!caller.DemoMode)
            throw new UnauthorizedAccessException("Demo mode is disabled for this user.");

        var dataEntryType = await _db.WorkCenters
            .AsNoTracking()
            .Where(wc => wc.Id == workCenterId)
            .Select(wc => wc.DataEntryType)
            .FirstOrDefaultAsync(ct);

        if (string.IsNullOrWhiteSpace(dataEntryType))
            throw new ArgumentException("Work center is missing data entry type.");

        return (caller, ResolveStage(dataEntryType));
    }

    private static string ResolveStage(string dataEntryType)
    {
        return dataEntryType switch
        {
            "Rolls" => DemoShellStage.Rolls,
            "Barcode-LongSeam" => DemoShellStage.LongSeam,
            "Barcode-LongSeamInsp" => DemoShellStage.LongSeamInspection,
            _ => throw new ArgumentException($"Data entry type '{dataEntryType}' is not supported for demo shell flow."),
        };
    }

    private async Task<DemoShellCurrentDto> GetCurrentForStageAsync(
        Guid plantId,
        string stage,
        Guid callerUserId,
        bool autoCreateAtRolls,
        CancellationToken ct)
    {
        if (autoCreateAtRolls && stage == DemoShellStage.Rolls)
            await EnsureCurrentAtRollsAsync(plantId, callerUserId, ct);

        var queue = _db.DemoShellFlows
            .AsNoTracking()
            .Where(x => x.PlantId == plantId && x.CurrentStage == stage)
            .OrderBy(x => x.ShellNumber);

        var current = await queue.FirstOrDefaultAsync(ct);
        var count = await queue.CountAsync(ct);

        if (current is null)
        {
            return new DemoShellCurrentDto
            {
                Stage = stage,
                HasCurrent = false,
                StageQueueCount = 0,
            };
        }

        return new DemoShellCurrentDto
        {
            Stage = stage,
            HasCurrent = true,
            BarcodeRaw = $"SC;{current.SerialNumber}",
            SerialNumber = current.SerialNumber,
            ShellNumber = current.ShellNumber,
            StageQueueCount = count,
        };
    }

    private async Task<DemoShellFlow> EnsureCurrentAtRollsAsync(Guid plantId, Guid callerUserId, CancellationToken ct)
    {
        var existing = await _db.DemoShellFlows
            .Where(x => x.PlantId == plantId && x.CurrentStage == DemoShellStage.Rolls)
            .OrderBy(x => x.ShellNumber)
            .FirstOrDefaultAsync(ct);

        if (existing is not null)
            return existing;

        return await CreateNextRollsShellAsync(plantId, callerUserId, ct);
    }

    private async Task<DemoShellFlow> CreateNextRollsShellAsync(Guid plantId, Guid callerUserId, CancellationToken ct)
    {
        var nextNumber = (await _db.DemoShellFlows
            .Where(x => x.PlantId == plantId)
            .MaxAsync(x => (int?)x.ShellNumber, ct) ?? 0) + 1;

        var now = DateTime.UtcNow;
        var entity = new DemoShellFlow
        {
            Id = Guid.NewGuid(),
            PlantId = plantId,
            ShellNumber = nextNumber,
            SerialNumber = nextNumber.ToString("D6"),
            CurrentStage = DemoShellStage.Rolls,
            CreatedByUserId = callerUserId,
            CreatedAtUtc = now,
            StageEnteredAtUtc = now,
        };

        _db.DemoShellFlows.Add(entity);
        await _db.SaveChangesAsync(ct);
        return entity;
    }
}
