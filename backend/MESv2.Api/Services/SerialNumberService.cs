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
            .Include(s => s.MillVendor)
            .Include(s => s.ProcessorVendor)
            .Include(s => s.HeadsVendor)
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
            {
                var standaloneNode = BuildNodeDto(sn, systemType ?? "serial");
                await AddPlateChildren(standaloneNode, sn.Id, allSnIds, cancellationToken);
                treeNodes.Add(standaloneNode);
            }
        }

        await AttachEventsToNodes(treeNodes, cancellationToken);

        return new SerialNumberLookupDto
        {
            SerialNumber = serial,
            TreeNodes = treeNodes
        };
    }

    private static TraceabilityNodeDto BuildNodeDto(SerialNumber sn, string nodeType)
    {
        var vendorName = sn.MillVendor?.Name ?? sn.ProcessorVendor?.Name ?? sn.HeadsVendor?.Name;
        return new TraceabilityNodeDto
        {
            Id = sn.Id.ToString(),
            Label = $"{sn.Serial} ({sn.Product?.TankType ?? nodeType})",
            NodeType = nodeType,
            Serial = sn.Serial,
            ProductName = sn.Product?.NameplateNumber ?? sn.Product?.ProductNumber,
            TankSize = sn.Product?.TankSize,
            TankType = sn.Product?.TankType,
            VendorName = vendorName,
            CoilNumber = sn.CoilNumber,
            HeatNumber = sn.HeatNumber,
            LotNumber = sn.LotNumber,
            CreatedAt = sn.CreatedAt
        };
    }

    private async Task<SerialNumber?> LoadFullSn(Guid id, CancellationToken ct)
    {
        return await _db.SerialNumbers
            .Include(s => s.Product).ThenInclude(p => p!.ProductType)
            .Include(s => s.MillVendor)
            .Include(s => s.ProcessorVendor)
            .Include(s => s.HeadsVendor)
            .FirstOrDefaultAsync(s => s.Id == id, ct);
    }

    private async Task<TraceabilityNodeDto> BuildTreeDownFromSellable(
        SerialNumber sellableSn, HashSet<Guid> allSnIds, CancellationToken ct)
    {
        var rootNode = BuildNodeDto(sellableSn, "sellable");
        rootNode.Label = $"{sellableSn.Serial} (Sellable)";

        var marriageLog = await _db.TraceabilityLogs
            .FirstOrDefaultAsync(t => t.ToSerialNumberId == sellableSn.Id
                && t.Relationship == "hydro-marriage"
                && t.FromSerialNumberId != null, ct);

        if (marriageLog?.FromSerialNumberId != null)
        {
            var assemblySn = await LoadFullSn(marriageLog.FromSerialNumberId.Value, ct);
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
        var assemblyNode = BuildNodeDto(assemblySn, "assembled");
        assemblyNode.Label = $"{assemblySn.Serial} (Assembled)";

        var childLogs = await _db.TraceabilityLogs
            .Where(t => t.ToSerialNumberId == assemblySn.Id)
            .ToListAsync(ct);

        foreach (var log in childLogs)
        {
            if (log.FromSerialNumberId.HasValue)
            {
                var childSn = await LoadFullSn(log.FromSerialNumberId.Value, ct);
                if (childSn != null)
                {
                    allSnIds.Add(childSn.Id);
                    var childNode = BuildNodeDto(childSn, log.Relationship ?? "component");

                    if (log.Relationship == "shell")
                        await AddPlateChildren(childNode, childSn.Id, allSnIds, ct);

                    assemblyNode.Children.Add(childNode);
                }
            }
            else if (!string.IsNullOrEmpty(log.TankLocation))
            {
                assemblyNode.Children.Add(new TraceabilityNodeDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Serial = log.TankLocation,
                    Label = $"Heat {log.TankLocation} ({log.Relationship})",
                    NodeType = log.Relationship ?? "head",
                    HeatNumber = log.TankLocation
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

        var parentSn = await LoadFullSn(parentLog.ToSerialNumberId.Value, ct);
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
                var sellableSn = await LoadFullSn(sellableLog.ToSerialNumberId.Value, ct);
                if (sellableSn != null)
                {
                    allSnIds.Add(sellableSn.Id);
                    var sellableNode = BuildNodeDto(sellableSn, "sellable");
                    sellableNode.Label = $"{sellableSn.Serial} (Sellable)";
                    sellableNode.Children = new List<TraceabilityNodeDto> { assemblyNode };
                    return sellableNode;
                }
            }

            return assemblyNode;
        }

        return null;
    }

    private async Task AddPlateChildren(TraceabilityNodeDto shellNode, Guid shellId,
        HashSet<Guid> allSnIds, CancellationToken ct)
    {
        var plateLogs = await _db.TraceabilityLogs
            .Where(t => t.ToSerialNumberId == shellId && t.Relationship == "plate" && t.FromSerialNumberId != null)
            .ToListAsync(ct);

        foreach (var log in plateLogs)
        {
            var plateSn = await LoadFullSn(log.FromSerialNumberId!.Value, ct);
            if (plateSn != null)
            {
                allSnIds.Add(plateSn.Id);
                var plateNode = BuildNodeDto(plateSn, "plate");
                shellNode.Children.Add(plateNode);
            }
        }
    }

    private async Task AttachEventsToNodes(List<TraceabilityNodeDto> nodes, CancellationToken ct)
    {
        var allNodeIds = new List<(TraceabilityNodeDto node, Guid? snId)>();
        void Collect(List<TraceabilityNodeDto> list)
        {
            foreach (var n in list)
            {
                Guid.TryParse(n.Id, out var parsed);
                allNodeIds.Add((n, parsed != Guid.Empty ? parsed : null));
                if (n.Children.Count > 0) Collect(n.Children);
            }
        }
        Collect(nodes);

        var guidIds = allNodeIds.Where(x => x.snId.HasValue).Select(x => x.snId!.Value).ToList();
        if (guidIds.Count == 0) return;

        var prodRecords = await _db.ProductionRecords
            .Include(r => r.WorkCenter).ThenInclude(w => w.WorkCenterType)
            .Include(r => r.Operator)
            .Include(r => r.Asset)
            .Include(r => r.SerialNumber)
            .Where(r => guidIds.Contains(r.SerialNumberId))
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);

        var inspRecords = await _db.InspectionRecords
            .Include(i => i.WorkCenter).ThenInclude(w => w.WorkCenterType)
            .Include(i => i.Operator)
            .Include(i => i.ControlPlan).ThenInclude(cp => cp!.Characteristic)
            .Include(i => i.SerialNumber)
            .Where(i => guidIds.Contains(i.SerialNumberId))
            .OrderByDescending(i => i.Timestamp)
            .ToListAsync(ct);

        var prodBySnId = prodRecords.GroupBy(r => r.SerialNumberId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var inspBySnId = inspRecords.GroupBy(i => i.SerialNumberId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (node, snId) in allNodeIds)
        {
            if (!snId.HasValue) continue;
            var events = new List<ManufacturingEventDto>();

            if (prodBySnId.TryGetValue(snId.Value, out var prods))
            {
                events.AddRange(prods.Select(r => new ManufacturingEventDto
                {
                    SerialNumberId = r.SerialNumberId.ToString(),
                    SerialNumberSerial = r.SerialNumber?.Serial ?? "",
                    Timestamp = r.Timestamp,
                    WorkCenterName = r.WorkCenter?.Name ?? "",
                    Type = r.WorkCenter?.WorkCenterType?.Name ?? "Manufacturing",
                    CompletedBy = r.Operator?.DisplayName ?? "",
                    AssetName = r.Asset?.Name,
                    InspectionResult = r.InspectionResult
                }));
            }

            if (inspBySnId.TryGetValue(snId.Value, out var insps))
            {
                events.AddRange(insps.Select(i => new ManufacturingEventDto
                {
                    SerialNumberId = i.SerialNumberId.ToString(),
                    SerialNumberSerial = i.SerialNumber?.Serial ?? "",
                    Timestamp = i.Timestamp,
                    WorkCenterName = i.WorkCenter?.Name ?? "",
                    Type = i.ControlPlan?.Characteristic?.Name ?? i.WorkCenter?.WorkCenterType?.Name ?? "Inspection",
                    CompletedBy = i.Operator?.DisplayName ?? "",
                    AssetName = null,
                    InspectionResult = i.ResultText
                }));
            }

            events.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            node.Events = events;
        }
    }
}
