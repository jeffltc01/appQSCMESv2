using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

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

        if (shellLog?.ToSerialNumberId != null)
        {
            var assemblySn = await _db.SerialNumbers
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.Id == shellLog.ToSerialNumberId.Value, cancellationToken);

            if (assemblySn != null)
            {
                var shellSnIds = await _db.TraceabilityLogs
                    .Where(t => t.ToSerialNumberId == assemblySn.Id && t.Relationship == "shell" && t.FromSerialNumberId != null)
                    .Select(t => t.FromSerialNumberId!.Value)
                    .ToListAsync(cancellationToken);

                var serials = await _db.SerialNumbers
                    .Where(s => shellSnIds.Contains(s.Id))
                    .Select(s => s.Serial)
                    .ToListAsync(cancellationToken);

                HeadLotInfoDto? leftHead = null;
                HeadLotInfoDto? rightHead = null;
                var leftLog = await _db.TraceabilityLogs
                    .FirstOrDefaultAsync(t => t.ToSerialNumberId == assemblySn.Id && t.Relationship == "leftHead", cancellationToken);
                if (leftLog?.TankLocation != null)
                    leftHead = new HeadLotInfoDto { HeatNumber = leftLog.TankLocation, CoilNumber = "", ProductDescription = "" };
                var rightLog = await _db.TraceabilityLogs
                    .FirstOrDefaultAsync(t => t.ToSerialNumberId == assemblySn.Id && t.Relationship == "rightHead", cancellationToken);
                if (rightLog?.TankLocation != null)
                    rightHead = new HeadLotInfoDto { HeatNumber = rightLog.TankLocation, CoilNumber = "", ProductDescription = "" };

                existingAssembly = new ExistingAssemblyDto
                {
                    AlphaCode = assemblySn.Serial,
                    TankSize = assemblySn.Product?.TankSize ?? 0,
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
            .Include(s => s.Product).ThenInclude(p => p!.ProductType)
            .FirstOrDefaultAsync(s => s.Serial == serial, cancellationToken);
        if (sn == null) return null;

        var allSnIds = new HashSet<Guid> { sn.Id };
        var treeNodes = new List<TraceabilityNodeDto>();
        var systemType = sn.Product?.ProductType?.SystemTypeName;

        if (systemType == "sellable")
        {
            var rootNode = await BuildTreeDownFromSellable(sn, allSnIds, cancellationToken);
            treeNodes.Add(rootNode);
        }
        else if (systemType == "assembled")
        {
            var assemblyNode = await BuildAssemblyNode(sn, allSnIds, cancellationToken);
            treeNodes.Add(assemblyNode);
        }
        else
        {
            var upResult = await TryBuildTreeUp(sn, allSnIds, cancellationToken);
            if (upResult != null)
                treeNodes.Add(upResult);
            else
                treeNodes.Add(new TraceabilityNodeDto
                {
                    Id = sn.Id.ToString(),
                    Label = $"{serial} ({sn.Product?.TankType ?? "Unknown"})",
                    NodeType = "serial"
                });
        }

        var events = await CollectEvents(allSnIds, cancellationToken);

        return new SerialNumberLookupDto
        {
            SerialNumber = serial,
            TreeNodes = treeNodes,
            Events = events
        };
    }

    private async Task<TraceabilityNodeDto> BuildTreeDownFromSellable(
        SerialNumber sellableSn, HashSet<Guid> allSnIds, CancellationToken ct)
    {
        var rootNode = new TraceabilityNodeDto
        {
            Id = sellableSn.Id.ToString(),
            Label = $"{sellableSn.Serial} (Sellable)",
            NodeType = "sellable"
        };

        var marriageLog = await _db.TraceabilityLogs
            .FirstOrDefaultAsync(t => t.ToSerialNumberId == sellableSn.Id
                && t.Relationship == "hydro-marriage"
                && t.FromSerialNumberId != null, ct);

        if (marriageLog?.FromSerialNumberId != null)
        {
            var assemblySn = await _db.SerialNumbers
                .Include(s => s.Product).ThenInclude(p => p!.ProductType)
                .FirstOrDefaultAsync(s => s.Id == marriageLog.FromSerialNumberId.Value, ct);
            if (assemblySn != null)
            {
                allSnIds.Add(assemblySn.Id);
                var assemblyNode = await BuildAssemblyNode(assemblySn, allSnIds, ct);
                rootNode.Children.Add(assemblyNode);
            }
        }

        return rootNode;
    }

    private async Task<TraceabilityNodeDto> BuildAssemblyNode(
        SerialNumber assemblySn, HashSet<Guid> allSnIds, CancellationToken ct)
    {
        var assemblyNode = new TraceabilityNodeDto
        {
            Id = assemblySn.Id.ToString(),
            Label = $"{assemblySn.Serial} (Assembled)",
            NodeType = "assembled"
        };

        var childLogs = await _db.TraceabilityLogs
            .Where(t => t.ToSerialNumberId == assemblySn.Id)
            .ToListAsync(ct);

        foreach (var log in childLogs)
        {
            if (log.FromSerialNumberId.HasValue)
            {
                var childSn = await _db.SerialNumbers
                    .Include(s => s.Product)
                    .FirstOrDefaultAsync(s => s.Id == log.FromSerialNumberId.Value, ct);
                if (childSn != null)
                {
                    allSnIds.Add(childSn.Id);
                    assemblyNode.Children.Add(new TraceabilityNodeDto
                    {
                        Id = childSn.Id.ToString(),
                        Label = $"{childSn.Serial} ({childSn.Product?.TankType ?? ""})",
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

        return assemblyNode;
    }

    private async Task<TraceabilityNodeDto?> TryBuildTreeUp(
        SerialNumber sn, HashSet<Guid> allSnIds, CancellationToken ct)
    {
        var parentLog = await _db.TraceabilityLogs
            .FirstOrDefaultAsync(t => t.FromSerialNumberId == sn.Id && t.ToSerialNumberId != null, ct);
        if (parentLog?.ToSerialNumberId == null) return null;

        var parentSn = await _db.SerialNumbers
            .Include(s => s.Product).ThenInclude(p => p!.ProductType)
            .FirstOrDefaultAsync(s => s.Id == parentLog.ToSerialNumberId.Value, ct);
        if (parentSn == null) return null;

        allSnIds.Add(parentSn.Id);
        var parentType = parentSn.Product?.ProductType?.SystemTypeName;

        if (parentType == "assembled")
        {
            var assemblyNode = await BuildAssemblyNode(parentSn, allSnIds, ct);

            var sellableLog = await _db.TraceabilityLogs
                .FirstOrDefaultAsync(t => t.FromSerialNumberId == parentSn.Id
                    && t.Relationship == "hydro-marriage"
                    && t.ToSerialNumberId != null, ct);
            if (sellableLog?.ToSerialNumberId != null)
            {
                var sellableSn = await _db.SerialNumbers
                    .Include(s => s.Product).ThenInclude(p => p!.ProductType)
                    .FirstOrDefaultAsync(s => s.Id == sellableLog.ToSerialNumberId.Value, ct);
                if (sellableSn != null)
                {
                    allSnIds.Add(sellableSn.Id);
                    return new TraceabilityNodeDto
                    {
                        Id = sellableSn.Id.ToString(),
                        Label = $"{sellableSn.Serial} (Sellable)",
                        NodeType = "sellable",
                        Children = new List<TraceabilityNodeDto> { assemblyNode }
                    };
                }
            }

            return assemblyNode;
        }

        return null;
    }

    private async Task<List<ManufacturingEventDto>> CollectEvents(
        HashSet<Guid> snIds, CancellationToken ct)
    {
        var snIdList = snIds.ToList();

        var prodRecords = await _db.ProductionRecords
            .Include(r => r.WorkCenter).ThenInclude(w => w.WorkCenterType)
            .Include(r => r.Operator)
            .Include(r => r.Asset)
            .Where(r => snIdList.Contains(r.SerialNumberId))
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);

        var events = prodRecords.Select(r => new ManufacturingEventDto
        {
            Timestamp = r.Timestamp,
            WorkCenterName = r.WorkCenter?.Name ?? "",
            Type = r.WorkCenter?.WorkCenterType?.Name ?? "Manufacturing",
            CompletedBy = r.Operator?.DisplayName ?? "",
            AssetName = r.Asset?.Name,
            InspectionResult = r.InspectionResult
        }).ToList();

        var inspRecords = await _db.InspectionRecords
            .Include(i => i.WorkCenter).ThenInclude(w => w.WorkCenterType)
            .Include(i => i.Operator)
            .Include(i => i.ControlPlan).ThenInclude(cp => cp.Characteristic)
            .Where(i => snIdList.Contains(i.SerialNumberId))
            .OrderByDescending(i => i.Timestamp)
            .ToListAsync(ct);

        events.AddRange(inspRecords.Select(i => new ManufacturingEventDto
        {
            Timestamp = i.Timestamp,
            WorkCenterName = i.WorkCenter?.Name ?? "",
            Type = i.ControlPlan?.Characteristic?.Name ?? i.WorkCenter?.WorkCenterType?.Name ?? "Inspection",
            CompletedBy = i.Operator?.DisplayName ?? "",
            AssetName = null,
            InspectionResult = i.ResultText
        }));

        events.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
        return events;
    }
}
