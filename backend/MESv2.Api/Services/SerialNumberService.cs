using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;

namespace MESv2.Api.Services;

public class SerialNumberService : ISerialNumberService
{
    private readonly MesDbContext _db;

    public SerialNumberService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<SerialNumberContextDto?> GetContextAsync(string serial, CancellationToken cancellationToken = default)
    {
        var sn = await _db.SerialNumbers
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Serial == serial, cancellationToken);
        if (sn == null)
            return null;

        var tankSize = sn.Product?.TankSize ?? 0;
        var shellSize = sn.Product?.TankType;

        ExistingAssemblyDto? existingAssembly = null;
        var shellLog = await _db.TraceabilityLogs
            .FirstOrDefaultAsync(t => t.FromSerialNumberId == sn.Id && t.Relationship == "shell", cancellationToken);
        if (shellLog != null && !string.IsNullOrEmpty(shellLog.ToAlphaCode))
        {
            var assembly = await _db.Assemblies
                .FirstOrDefaultAsync(a => a.AlphaCode == shellLog.ToAlphaCode, cancellationToken);
            if (assembly != null)
            {
                var shellSerials = await _db.TraceabilityLogs
                    .Where(t => t.ToAlphaCode == shellLog.ToAlphaCode && t.Relationship == "shell" && t.FromSerialNumberId != null)
                    .Select(t => t.FromSerialNumberId)
                    .ToListAsync(cancellationToken);

                var serials = new List<string>();
                foreach (var id in shellSerials)
                {
                    if (id == null) continue;
                    var s = await _db.SerialNumbers.FindAsync(new object[] { id }, cancellationToken);
                    if (s != null) serials.Add(s.Serial);
                }

                HeadLotInfoDto? leftHead = null;
                HeadLotInfoDto? rightHead = null;
                var leftLog = await _db.TraceabilityLogs
                    .FirstOrDefaultAsync(t => t.ToAlphaCode == shellLog.ToAlphaCode && t.Relationship == "leftHead", cancellationToken);
                if (leftLog?.TankLocation != null)
                    leftHead = new HeadLotInfoDto { HeatNumber = leftLog.TankLocation, CoilNumber = "", ProductDescription = "" };
                var rightLog = await _db.TraceabilityLogs
                    .FirstOrDefaultAsync(t => t.ToAlphaCode == shellLog.ToAlphaCode && t.Relationship == "rightHead", cancellationToken);
                if (rightLog?.TankLocation != null)
                    rightHead = new HeadLotInfoDto { HeatNumber = rightLog.TankLocation, CoilNumber = "", ProductDescription = "" };

                existingAssembly = new ExistingAssemblyDto
                {
                    AlphaCode = assembly.AlphaCode,
                    TankSize = assembly.TankSize,
                    Shells = serials,
                    LeftHeadInfo = leftHead,
                    RightHeadInfo = rightHead
                };
            }
        }

        return new SerialNumberContextDto
        {
            SerialNumber = sn.Serial,
            TankSize = tankSize,
            ShellSize = shellSize,
            ExistingAssembly = existingAssembly
        };
    }
}
