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
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(s => s.Serial == serial, cancellationToken);
        if (sn == null)
            return null;

        var tankSize = sn.Product?.TankSize ?? 0;
        var shellSize = sn.Product?.TankType;

        ExistingAssemblyDto? existingAssembly = null;
        var shellLog = await _db.TraceabilityLogs
            .FirstOrDefaultAsync(t => t.FromSerialNumberId == sn.Id
                && (t.Relationship == "ShellToAssembly" || t.Relationship == "shell"), cancellationToken);

        if (shellLog?.ToSerialNumberId != null)
        {
            var assemblySn = await _db.SerialNumbers
                .Include(s => s.Product)
                .FirstOrDefaultAsync(s => s.Id == shellLog.ToSerialNumberId.Value, cancellationToken);

            if (assemblySn != null)
            {
                var shellSnIds = await _db.TraceabilityLogs
                    .Where(t => t.ToSerialNumberId == assemblySn.Id
                        && (t.Relationship == "ShellToAssembly" || t.Relationship == "shell")
                        && t.FromSerialNumberId != null)
                    .Select(t => t.FromSerialNumberId!.Value)
                    .ToListAsync(cancellationToken);

                var serials = await _db.SerialNumbers
                    .Where(s => shellSnIds.Contains(s.Id))
                    .Select(s => s.Serial)
                    .ToListAsync(cancellationToken);

                HeadLotInfoDto? leftHead = null;
                HeadLotInfoDto? rightHead = null;
                var headLogs = await _db.TraceabilityLogs
                    .Include(t => t.FromSerialNumber)
                    .Where(t => t.ToSerialNumberId == assemblySn.Id
                        && (t.Relationship == "HeadToAssembly" || t.Relationship == "leftHead" || t.Relationship == "rightHead"))
                    .OrderBy(t => t.TankLocation)
                    .ToListAsync(cancellationToken);
                var leftLog = headLogs.FirstOrDefault(t => t.TankLocation == "Head 1" || t.Relationship == "leftHead");
                if (leftLog != null)
                    leftHead = new HeadLotInfoDto
                    {
                        HeatNumber = leftLog.FromSerialNumber?.HeatNumber ?? leftLog.TankLocation ?? "",
                        CoilNumber = leftLog.FromSerialNumber?.CoilNumber ?? "",
                        ProductDescription = ""
                    };
                var rightLog = headLogs.FirstOrDefault(t => t.TankLocation == "Head 2" || t.Relationship == "rightHead");
                if (rightLog != null)
                    rightHead = new HeadLotInfoDto
                    {
                        HeatNumber = rightLog.FromSerialNumber?.HeatNumber ?? rightLog.TankLocation ?? "",
                        CoilNumber = rightLog.FromSerialNumber?.CoilNumber ?? "",
                        ProductDescription = ""
                    };

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
            .OrderByDescending(s => s.CreatedAt)
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
        await AttachCountsToNodes(treeNodes, cancellationToken);
        AttachChildSerials(treeNodes);

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
                && (t.Relationship == "hydro-marriage" || t.Relationship == "component")
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
                if (allSnIds.Contains(log.FromSerialNumberId.Value))
                    continue;

                var childSn = await LoadFullSn(log.FromSerialNumberId.Value, ct);
                if (childSn != null)
                {
                    allSnIds.Add(childSn.Id);
                    var nodeType = NormalizeRelationship(log.Relationship, log.TankLocation);
                    var resolvedNodeType = nodeType == "component"
                        ? InferNodeTypeFromProduct(childSn, log.TankLocation)
                        : nodeType;
                    var childNode = BuildNodeDto(childSn, resolvedNodeType);

                    if (resolvedNodeType == "shell")
                        await AddPlateChildren(childNode, childSn.Id, allSnIds, ct);

                    assemblyNode.Children.Add(childNode);
                }
            }
            else if (!string.IsNullOrEmpty(log.TankLocation))
            {
                var headType = NormalizeRelationship(log.Relationship, log.TankLocation);
                assemblyNode.Children.Add(new TraceabilityNodeDto
                {
                    Id = Guid.NewGuid().ToString(),
                    Serial = log.TankLocation,
                    Label = $"Heat {log.TankLocation} ({headType})",
                    NodeType = headType,
                    HeatNumber = log.TankLocation
                });
            }
        }

        return assemblyNode;
    }

    internal static string NormalizeRelationship(string? relationship, string? tankLocation)
    {
        return relationship switch
        {
            "ShellToAssembly" or "shell" => "shell",
            "HeadToAssembly" => tankLocation switch
            {
                "Head 1" => "leftHead",
                "Head 2" => "rightHead",
                _ => "leftHead"
            },
            "leftHead" or "rightHead" => relationship,
            "plate" => "plate",
            "NameplateToAssembly" or "Nameplate" => "nameplate",
            "component" => tankLocation switch
            {
                "Head 1" => "leftHead",
                "Head 2" => "rightHead",
                var l when !string.IsNullOrWhiteSpace(l) && l.StartsWith("Shell", StringComparison.OrdinalIgnoreCase) => "shell",
                _ => "component"
            },
            _ => relationship ?? "component"
        };
    }

    private static string InferNodeTypeFromProduct(SerialNumber childSn, string? tankLocation)
    {
        var systemType = childSn.Product?.ProductType?.SystemTypeName;
        return systemType switch
        {
            "shell" => "shell",
            "plate" => "plate",
            "sellable" => "nameplate",
            "head" => tankLocation == "Head 2" ? "rightHead" : "leftHead",
            _ => "component"
        };
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
                    && (t.Relationship == "hydro-marriage" || t.Relationship == "component")
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
            .Where(t => t.ToSerialNumberId == shellId
                && (t.Relationship == "plate" || t.Relationship == "component")
                && t.FromSerialNumberId != null)
            .ToListAsync(ct);

        foreach (var log in plateLogs)
        {
            var plateSn = await LoadFullSn(log.FromSerialNumberId!.Value, ct);
            var isPlate = plateSn?.Product?.ProductType?.SystemTypeName == "plate";
            if (plateSn != null && (log.Relationship == "plate" || isPlate))
            {
                allSnIds.Add(plateSn.Id);
                var plateNode = BuildNodeDto(plateSn, "plate");
                shellNode.Children.Add(plateNode);
            }
        }
    }

    private async Task AttachEventsToNodes(List<TraceabilityNodeDto> nodes, CancellationToken ct)
    {
        var allNodeIds = CollectNodeIds(nodes);

        var guidIds = allNodeIds.Where(x => x.snId.HasValue).Select(x => x.snId!.Value).ToList();
        if (guidIds.Count == 0) return;

        var prodRecords = await LoadProductionRecords(guidIds, ct);
        var inspRecords = await LoadInspectionRecords(guidIds, ct);

        var prodRecordIdsWithInspection = new HashSet<Guid>(
            inspRecords.Select(i => i.ProductionRecordId));

        var allProdRecordIds = prodRecords.Select(r => r.Id)
            .Union(inspRecords.Select(i => i.ProductionRecordId))
            .Distinct().ToList();

        var annotationsByProdId = await LoadAnnotationBadges(allProdRecordIds, ct);

        var prodBySnId = prodRecords.GroupBy(r => r.SerialNumberId)
            .ToDictionary(g => g.Key, g => g.ToList());
        var inspBySnId = inspRecords.GroupBy(i => i.SerialNumberId)
            .ToDictionary(g => g.Key, g => g.ToList());

        foreach (var (node, snId) in allNodeIds)
        {
            if (!snId.HasValue) continue;
            var events = new List<ManufacturingEventDto>();

            prodBySnId.TryGetValue(snId.Value, out var prods);
            if (prods != null)
                events.AddRange(MapProductionEvents(prods, prodRecordIdsWithInspection, annotationsByProdId));

            if (inspBySnId.TryGetValue(snId.Value, out var insps))
                events.AddRange(MapInspectionEvents(insps, prods, annotationsByProdId));

            events.Sort((a, b) => b.Timestamp.CompareTo(a.Timestamp));
            node.Events = events;
        }
    }

    private static List<(TraceabilityNodeDto node, Guid? snId)> CollectNodeIds(
        List<TraceabilityNodeDto> nodes)
    {
        var result = new List<(TraceabilityNodeDto node, Guid? snId)>();
        void Collect(List<TraceabilityNodeDto> list)
        {
            foreach (var n in list)
            {
                Guid.TryParse(n.Id, out var parsed);
                result.Add((n, parsed != Guid.Empty ? parsed : null));
                if (n.Children.Count > 0) Collect(n.Children);
            }
        }
        Collect(nodes);
        return result;
    }

    private async Task<List<ProductionRecord>> LoadProductionRecords(
        List<Guid> snIds, CancellationToken ct)
    {
        return await _db.ProductionRecords
            .Include(r => r.WorkCenter).ThenInclude(w => w.WorkCenterType)
            .Include(r => r.Operator)
            .Include(r => r.Asset)
            .Include(r => r.SerialNumber)
            .Where(r => snIds.Contains(r.SerialNumberId))
            .OrderByDescending(r => r.Timestamp)
            .ToListAsync(ct);
    }

    private async Task<List<InspectionRecord>> LoadInspectionRecords(
        List<Guid> snIds, CancellationToken ct)
    {
        return await _db.InspectionRecords
            .Include(i => i.WorkCenter).ThenInclude(w => w.WorkCenterType)
            .Include(i => i.Operator)
            .Include(i => i.ControlPlan).ThenInclude(cp => cp!.Characteristic)
            .Include(i => i.SerialNumber)
            .Where(i => snIds.Contains(i.SerialNumberId))
            .OrderByDescending(i => i.Timestamp)
            .ToListAsync(ct);
    }

    private async Task<Dictionary<Guid, List<LogAnnotationBadgeDto>>> LoadAnnotationBadges(
        List<Guid> prodRecordIds, CancellationToken ct)
    {
        if (prodRecordIds.Count == 0)
            return new Dictionary<Guid, List<LogAnnotationBadgeDto>>();

        var annotations = await _db.Annotations
            .Include(a => a.AnnotationType)
            .Include(a => a.InitiatedByUser)
            .Include(a => a.ResolvedByUser)
            .Where(a => a.ProductionRecordId.HasValue && prodRecordIds.Contains(a.ProductionRecordId.Value))
            .ToListAsync(ct);

        return annotations
            .GroupBy(a => a.ProductionRecordId!.Value)
            .ToDictionary(g => g.Key, g => g
                .OrderByDescending(a => a.AnnotationType.RequiresResolution)
                .ThenByDescending(a => a.CreatedAt)
                .Select(a => new LogAnnotationBadgeDto
                {
                    Id = a.Id,
                    Abbreviation = a.AnnotationType.Abbreviation ?? a.AnnotationType.Name[..1],
                    Color = a.AnnotationType.DisplayColor ?? "#212529",
                    TypeName = a.AnnotationType.Name,
                    Status = a.Status.ToString(),
                    Notes = a.Notes,
                    InitiatedByName = a.InitiatedByUser?.DisplayName ?? "",
                    ResolvedByName = a.ResolvedByUser?.DisplayName,
                    ResolvedNotes = a.ResolvedNotes,
                    CreatedAt = a.CreatedAt
                }).ToList());
    }

    private static IEnumerable<ManufacturingEventDto> MapProductionEvents(
        List<ProductionRecord> prods,
        HashSet<Guid> prodRecordIdsWithInspection,
        Dictionary<Guid, List<LogAnnotationBadgeDto>> annotationsByProdId)
    {
        return prods
            .Where(r => !prodRecordIdsWithInspection.Contains(r.Id))
            .Select(r => new ManufacturingEventDto
            {
                SerialNumberId = r.SerialNumberId.ToString(),
                SerialNumberSerial = r.SerialNumber?.Serial ?? "",
                Timestamp = r.Timestamp,
                WorkCenterName = r.WorkCenter?.Name ?? "",
                Type = r.WorkCenter?.WorkCenterType?.Name ?? "Manufacturing",
                CompletedBy = r.Operator?.DisplayName ?? "",
                AssetName = r.Asset?.Name,
                InspectionResult = null,
                Annotations = annotationsByProdId.GetValueOrDefault(r.Id) ?? new()
            });
    }

    private static IEnumerable<ManufacturingEventDto> MapInspectionEvents(
        List<InspectionRecord> insps,
        List<ProductionRecord>? prods,
        Dictionary<Guid, List<LogAnnotationBadgeDto>> annotationsByProdId)
    {
        var inspProdIds = new HashSet<Guid>(insps.Select(i => i.ProductionRecordId));
        var assetLookup = prods != null
            ? prods.Where(r => inspProdIds.Contains(r.Id)).ToDictionary(r => r.Id, r => r.Asset?.Name)
            : new Dictionary<Guid, string?>();

        return insps.Select(i => new ManufacturingEventDto
        {
            SerialNumberId = i.SerialNumberId.ToString(),
            SerialNumberSerial = i.SerialNumber?.Serial ?? "",
            Timestamp = i.Timestamp,
            WorkCenterName = i.WorkCenter?.Name ?? "",
            Type = i.ControlPlan?.Characteristic?.Name ?? i.WorkCenter?.WorkCenterType?.Name ?? "Inspection",
            CompletedBy = i.Operator?.DisplayName ?? "",
            AssetName = assetLookup.GetValueOrDefault(i.ProductionRecordId),
            InspectionResult = i.ResultText,
            Annotations = annotationsByProdId.GetValueOrDefault(i.ProductionRecordId) ?? new()
        });
    }

    private async Task AttachCountsToNodes(List<TraceabilityNodeDto> nodes, CancellationToken ct)
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

        var defectCounts = await _db.DefectLogs
            .Where(d => guidIds.Contains(d.SerialNumberId))
            .GroupBy(d => d.SerialNumberId)
            .Select(g => new { SnId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SnId, x => x.Count, ct);

        var annotationCounts = await _db.Annotations
            .Where(a => a.SerialNumberId.HasValue && guidIds.Contains(a.SerialNumberId.Value))
            .GroupBy(a => a.SerialNumberId!.Value)
            .Select(g => new { SnId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.SnId, x => x.Count, ct);

        foreach (var (node, snId) in allNodeIds)
        {
            if (!snId.HasValue) continue;
            node.DefectCount = defectCounts.GetValueOrDefault(snId.Value);
            node.AnnotationCount = annotationCounts.GetValueOrDefault(snId.Value);
        }
    }

    private static void AttachChildSerials(List<TraceabilityNodeDto> nodes)
    {
        void Walk(TraceabilityNodeDto node)
        {
            if (node.NodeType == "assembled")
            {
                node.ChildSerials = node.Children
                    .Where(c => c.NodeType == "shell")
                    .Select(c => c.Serial)
                    .ToList();
            }
            foreach (var child in node.Children) Walk(child);
        }
        foreach (var n in nodes) Walk(n);
    }
}
