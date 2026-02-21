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

    public async Task<SerialNumberLookupDto?> GetLookupAsync(string serial, CancellationToken cancellationToken = default)
    {
        var sn = await _db.SerialNumbers
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Serial == serial, cancellationToken);
        if (sn == null) return null;

        var treeNodes = new List<TraceabilityNodeDto>();

        var shellLog = await _db.TraceabilityLogs
            .FirstOrDefaultAsync(t => t.FromSerialNumberId == sn.Id && t.Relationship == "shell", cancellationToken);

        if (shellLog != null && !string.IsNullOrEmpty(shellLog.ToAlphaCode))
        {
            var assembly = await _db.Assemblies
                .FirstOrDefaultAsync(a => a.AlphaCode == shellLog.ToAlphaCode, cancellationToken);

            if (assembly != null)
            {
                var assemblyNode = new TraceabilityNodeDto
                {
                    Id = assembly.AlphaCode,
                    Label = $"{assembly.AlphaCode} (Assembled)",
                    NodeType = "assembled",
                    Children = new List<TraceabilityNodeDto>()
                };

                var relatedLogs = await _db.TraceabilityLogs
                    .Where(t => t.ToAlphaCode == shellLog.ToAlphaCode)
                    .ToListAsync(cancellationToken);

                foreach (var log in relatedLogs)
                {
                    if (log.FromSerialNumberId.HasValue)
                    {
                        var relatedSn = await _db.SerialNumbers
                            .Include(s => s.Product)
                            .FirstOrDefaultAsync(s => s.Id == log.FromSerialNumberId.Value, cancellationToken);
                        if (relatedSn != null)
                        {
                            var productDesc = relatedSn.Product?.TankType ?? "";
                            assemblyNode.Children.Add(new TraceabilityNodeDto
                            {
                                Id = relatedSn.Id.ToString(),
                                Label = $"{relatedSn.Serial} ({productDesc})",
                                NodeType = log.Relationship ?? "component"
                            });
                        }
                    }
                    else if (!string.IsNullOrEmpty(log.TankLocation))
                    {
                        assemblyNode.Children.Add(new TraceabilityNodeDto
                        {
                            Id = Guid.NewGuid().ToString(),
                            Label = $"Heat {log.TankLocation} ({log.Relationship})",
                            NodeType = log.Relationship ?? "head"
                        });
                    }
                }

                var sellableNode = new TraceabilityNodeDto
                {
                    Id = sn.Id.ToString(),
                    Label = $"{serial} (Sellable)",
                    NodeType = "sellable",
                    Children = new List<TraceabilityNodeDto> { assemblyNode }
                };
                treeNodes.Add(sellableNode);
            }
        }

        if (treeNodes.Count == 0)
        {
            treeNodes.Add(new TraceabilityNodeDto
            {
                Id = sn.Id.ToString(),
                Label = $"{serial} ({sn.Product?.TankType ?? "Unknown"})",
                NodeType = "serial"
            });
        }

        var records = await _db.ProductionRecords
            .Include(r => r.WorkCenter).ThenInclude(w => w.WorkCenterType)
            .Include(r => r.Operator)
            .Include(r => r.Asset)
            .Where(r => r.SerialNumberId == sn.Id)
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(cancellationToken);

        var events = records.Select(r => new ManufacturingEventDto
        {
            Timestamp = r.Timestamp,
            WorkCenterName = r.WorkCenter?.Name ?? "",
            Type = r.WorkCenter?.WorkCenterType?.Name ?? "Manufacturing",
            CompletedBy = r.Operator?.DisplayName ?? "",
            AssetName = r.Asset?.Name,
            InspectionResult = r.InspectionResult
        }).ToList();

        return new SerialNumberLookupDto
        {
            SerialNumber = serial,
            TreeNodes = treeNodes,
            Events = events
        };
    }
}
