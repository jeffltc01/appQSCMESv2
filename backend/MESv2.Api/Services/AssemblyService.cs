using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class AssemblyService : IAssemblyService
{
    private const string ShellToAssemblyRelationship = "ShellToAssembly";
    private const string HeadToAssemblyRelationship = "HeadToAssembly";
    private const string LineageRelationship = "ReassembledTo";
    private readonly MesDbContext _db;

    public AssemblyService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<string> GetNextAlphaCodeAsync(Guid plantId, CancellationToken cancellationToken = default)
    {
        var plant = await _db.Plants.FirstAsync(p => p.Id == plantId, cancellationToken);
        var current = plant.NextTankAlphaCode;

        plant.NextTankAlphaCode = AdvanceAlphaCode(current);

        return current;
    }

    public static string AdvanceAlphaCode(string code)
    {
        var high = code[0] - 'A';
        var low = code[1] - 'A';
        var nextIndex = (high * 26 + low + 1) % 676;
        return $"{(char)('A' + nextIndex / 26)}{(char)('A' + nextIndex % 26)}";
    }

    public async Task<CreateAssemblyResponseDto> CreateAsync(CreateAssemblyDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.WorkCenterId == Guid.Empty)
            throw new ArgumentException("Work center is required.");
        if (dto.ProductionLineId == Guid.Empty)
            throw new ArgumentException("Production line is required. Please select a production line before saving.");
        if (dto.OperatorId == Guid.Empty)
            throw new ArgumentException("Operator is required.");

        var wc = await _db.WorkCenters
            .FirstOrDefaultAsync(w => w.Id == dto.WorkCenterId, cancellationToken);
        if (wc == null)
            throw new ArgumentException("Work center not found.");

        var plantId = await _db.ProductionLines
            .Where(p => p.Id == dto.ProductionLineId)
            .Select(p => p.PlantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (plantId == Guid.Empty)
            throw new ArgumentException("Production line not found.");

        var alphaCode = await GetNextAlphaCodeAsync(plantId, cancellationToken);

        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == dto.TankSize, cancellationToken)
            ?? throw new ArgumentException($"No assembled tank product found for tank size {dto.TankSize}.");

        var assemblySn = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = alphaCode,
            ProductId = product.Id,
            PlantId = plantId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = dto.OperatorId
        };
        _db.SerialNumbers.Add(assemblySn);

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = assemblySn.Id,
            WorkCenterId = dto.WorkCenterId,
            AssetId = dto.AssetId == Guid.Empty ? null : dto.AssetId,
            ProductionLineId = dto.ProductionLineId,
            OperatorId = dto.OperatorId,
            Timestamp = DateTime.UtcNow
        };
        _db.ProductionRecords.Add(record);

        for (int i = 0; i < dto.Shells.Count; i++)
        {
            var sn = await _db.SerialNumbers.FirstOrDefaultAsync(s => s.Serial == dto.Shells[i], cancellationToken);
            if (sn != null)
            {
                _db.TraceabilityLogs.Add(new TraceabilityLog
                {
                    Id = Guid.NewGuid(),
                    FromSerialNumberId = sn.Id,
                    ToSerialNumberId = assemblySn.Id,
                    ProductionRecordId = record.Id,
                    Relationship = "ShellToAssembly",
                    TankLocation = $"Shell {i + 1}",
                    Quantity = 1,
                    Timestamp = DateTime.UtcNow
                });
            }
        }

        AddHeadTrace(dto.LeftHeadLotId, dto.LeftHeadHeatNumber, dto.LeftHeadCoilNumber, dto.LeftHeadLotNumber,
            assemblySn, record.Id, plantId, "Head 1");
        AddHeadTrace(dto.RightHeadLotId, dto.RightHeadHeatNumber, dto.RightHeadCoilNumber, dto.RightHeadLotNumber,
            assemblySn, record.Id, plantId, "Head 2");

        foreach (var welderId in dto.WelderIds)
        {
            _db.WelderLogs.Add(new WelderLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = record.Id,
                UserId = welderId,
                CharacteristicId = null
            });
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new CreateAssemblyResponseDto
        {
            Id = assemblySn.Id,
            AlphaCode = assemblySn.Serial,
            Timestamp = record.Timestamp
        };
    }

    public async Task<ReassembleResponseDto> ReassembleAsync(string alphaCode, ReassemblyDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.WorkCenterId == Guid.Empty)
            throw new ArgumentException("Work center is required.");
        if (dto.ProductionLineId == Guid.Empty)
            throw new ArgumentException("Production line is required.");
        if (dto.OperatorId == Guid.Empty)
            throw new ArgumentException("Operator is required.");
        if (dto.PrimaryAssembly == null)
            throw new ArgumentException("PrimaryAssembly is required.");

        var assemblySn = await _db.SerialNumbers
            .Where(s => s.Serial == alphaCode && !s.IsObsolete && s.Product!.ProductType!.SystemTypeName == "assembled")
            .OrderByDescending(s => s.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (assemblySn == null)
            throw new ArgumentException("Assembly not found.");

        var sourceComposition = await LoadAssemblyCompositionAsync(assemblySn.Id, cancellationToken);
        var primaryComposition = BuildComposition(dto.PrimaryAssembly, sourceComposition);
        var secondaryComposition = dto.SecondaryAssembly == null
            ? null
            : BuildComposition(dto.SecondaryAssembly, sourceComposition);

        if (string.Equals(dto.OperationType, "split", StringComparison.OrdinalIgnoreCase))
        {
            if (secondaryComposition == null)
                throw new ArgumentException("SecondaryAssembly is required for split operations.");
            if (secondaryComposition.Shells.Count == 0 || primaryComposition.Shells.Count == 0)
                throw new ArgumentException("Split requires shells for both resulting assemblies.");
        }
        else if (secondaryComposition != null)
        {
            throw new ArgumentException("SecondaryAssembly is only valid for split operations.");
        }

        if (string.Equals(dto.OperationType, "replace", StringComparison.OrdinalIgnoreCase)
            && CompositionEquals(primaryComposition, sourceComposition))
        {
            throw new ArgumentException("No changes detected. Replace requires at least one modified component.");
        }

        var operationIsSplit = string.Equals(dto.OperationType, "split", StringComparison.OrdinalIgnoreCase);
        var providerName = _db.Database.ProviderName ?? string.Empty;
        var useTransaction = !providerName.Contains("InMemory", StringComparison.OrdinalIgnoreCase);
        var tx = useTransaction
            ? await _db.Database.BeginTransactionAsync(cancellationToken)
            : null;

        var createdAssemblies = new List<CreateAssemblyResponseDto>();
        var primaryCreated = await CreateFromCompositionAsync(
            primaryComposition,
            dto.WorkCenterId,
            dto.AssetId,
            dto.ProductionLineId,
            dto.OperatorId,
            dto.WelderIds,
            cancellationToken);
        createdAssemblies.Add(primaryCreated);

        if (operationIsSplit && secondaryComposition != null)
        {
            var secondaryCreated = await CreateFromCompositionAsync(
                secondaryComposition,
                dto.WorkCenterId,
                dto.AssetId,
                dto.ProductionLineId,
                dto.OperatorId,
                dto.WelderIds,
                cancellationToken);
            createdAssemblies.Add(secondaryCreated);
        }

        foreach (var created in createdAssemblies)
        {
            _db.TraceabilityLogs.Add(new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = assemblySn.Id,
                ToSerialNumberId = created.Id,
                Relationship = LineageRelationship,
                Quantity = 1,
                Timestamp = DateTime.UtcNow
            });
        }

        assemblySn.IsObsolete = true;
        assemblySn.ReplaceBySNId = createdAssemblies[0].Id;
        assemblySn.ModifiedByUserId = dto.OperatorId;
        assemblySn.ModifiedDateTime = DateTime.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        if (tx != null)
        {
            await tx.CommitAsync(cancellationToken);
            await tx.DisposeAsync();
        }

        return new ReassembleResponseDto
        {
            SourceAlphaCode = assemblySn.Serial,
            CreatedAssemblies = createdAssemblies
        };
    }

    private void AddHeadTrace(string? lotId, string? heatNumber, string? coilNumber, string? lotNumber,
        SerialNumber assemblySn, Guid productionRecordId, Guid plantId, string tankLocation)
    {
        if (string.IsNullOrEmpty(lotId) && string.IsNullOrEmpty(heatNumber) && string.IsNullOrEmpty(coilNumber) && string.IsNullOrEmpty(lotNumber))
            return;

        Guid? headSnId = null;
        var hasHeatCoil = !string.IsNullOrEmpty(heatNumber) || !string.IsNullOrEmpty(coilNumber);
        var hasLot = !string.IsNullOrEmpty(lotNumber);

        if (hasHeatCoil || hasLot)
        {
            var serial = hasHeatCoil
                ? $"Head {heatNumber ?? ""}/{coilNumber ?? ""}"
                : $"Lot:{lotNumber}";
            var headSn = new SerialNumber
            {
                Id = Guid.NewGuid(),
                Serial = serial,
                CoilNumber = coilNumber,
                HeatNumber = heatNumber,
                LotNumber = lotNumber,
                PlantId = plantId,
                CreatedAt = DateTime.UtcNow
            };
            _db.SerialNumbers.Add(headSn);
            headSnId = headSn.Id;
        }

        _db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(),
            FromSerialNumberId = headSnId,
            ToSerialNumberId = assemblySn.Id,
            ProductionRecordId = productionRecordId,
            Relationship = HeadToAssemblyRelationship,
            TankLocation = tankLocation,
            Timestamp = DateTime.UtcNow
        });
    }

    private sealed class HeadSnapshot
    {
        public string? LotId { get; set; }
        public string? HeatNumber { get; set; }
        public string? CoilNumber { get; set; }
        public string? LotNumber { get; set; }
    }

    private sealed class AssemblyComposition
    {
        public List<string> Shells { get; set; } = new();
        public int TankSize { get; set; }
        public HeadSnapshot? LeftHead { get; set; }
        public HeadSnapshot? RightHead { get; set; }
    }

    private static AssemblyComposition BuildComposition(ReassemblyAssemblyDto dto, AssemblyComposition source)
    {
        if (dto.Shells == null || dto.Shells.Count == 0)
            throw new ArgumentException("At least one shell is required.");

        return new AssemblyComposition
        {
            Shells = dto.Shells.Select(s => s.Trim()).Where(s => s.Length > 0).ToList(),
            TankSize = dto.TankSize > 0 ? dto.TankSize : source.TankSize,
            LeftHead = MergeHead(dto.LeftHead, source.LeftHead),
            RightHead = MergeHead(dto.RightHead, source.RightHead)
        };
    }

    private static HeadSnapshot? MergeHead(ReassemblyHeadDto? incoming, HeadSnapshot? source)
    {
        if (incoming == null)
            return source == null ? null : new HeadSnapshot
            {
                LotId = source.LotId,
                HeatNumber = source.HeatNumber,
                CoilNumber = source.CoilNumber,
                LotNumber = source.LotNumber
            };

        return new HeadSnapshot
        {
            LotId = incoming.LotId ?? source?.LotId,
            HeatNumber = incoming.HeatNumber ?? source?.HeatNumber,
            CoilNumber = incoming.CoilNumber ?? source?.CoilNumber,
            LotNumber = incoming.LotNumber ?? source?.LotNumber
        };
    }

    private static bool CompositionEquals(AssemblyComposition left, AssemblyComposition right)
    {
        if (left.TankSize != right.TankSize)
            return false;
        if (!left.Shells.SequenceEqual(right.Shells, StringComparer.OrdinalIgnoreCase))
            return false;
        return HeadEquals(left.LeftHead, right.LeftHead) && HeadEquals(left.RightHead, right.RightHead);
    }

    private static bool HeadEquals(HeadSnapshot? left, HeadSnapshot? right)
    {
        if (left == null && right == null) return true;
        if (left == null || right == null) return false;
        return string.Equals(left.LotId, right.LotId, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.HeatNumber, right.HeatNumber, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.CoilNumber, right.CoilNumber, StringComparison.OrdinalIgnoreCase)
            && string.Equals(left.LotNumber, right.LotNumber, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<AssemblyComposition> LoadAssemblyCompositionAsync(Guid assemblySnId, CancellationToken cancellationToken)
    {
        var assembly = await _db.SerialNumbers
            .Include(s => s.Product)
            .FirstAsync(s => s.Id == assemblySnId, cancellationToken);

        var shellLogs = await _db.TraceabilityLogs
            .Include(t => t.FromSerialNumber)
            .Where(t => t.ToSerialNumberId == assemblySnId
                && (t.Relationship == ShellToAssemblyRelationship || t.Relationship == "shell"))
            .OrderBy(t => t.TankLocation)
            .ToListAsync(cancellationToken);

        var headLogs = await _db.TraceabilityLogs
            .Include(t => t.FromSerialNumber)
            .Where(t => t.ToSerialNumberId == assemblySnId
                && (t.Relationship == HeadToAssemblyRelationship || t.Relationship == "leftHead" || t.Relationship == "rightHead"))
            .OrderBy(t => t.TankLocation)
            .ToListAsync(cancellationToken);

        var leftHeadLog = headLogs.FirstOrDefault(h => h.TankLocation == "Head 1" || h.Relationship == "leftHead");
        var rightHeadLog = headLogs.FirstOrDefault(h => h.TankLocation == "Head 2" || h.Relationship == "rightHead");

        return new AssemblyComposition
        {
            TankSize = assembly.Product?.TankSize ?? 0,
            Shells = shellLogs
                .Select(s => s.FromSerialNumber?.Serial)
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => s!)
                .ToList(),
            LeftHead = MapHead(leftHeadLog),
            RightHead = MapHead(rightHeadLog)
        };
    }

    private static HeadSnapshot? MapHead(TraceabilityLog? headLog)
    {
        if (headLog == null)
            return null;

        return new HeadSnapshot
        {
            HeatNumber = headLog.FromSerialNumber?.HeatNumber,
            CoilNumber = headLog.FromSerialNumber?.CoilNumber,
            LotNumber = headLog.FromSerialNumber?.LotNumber
        };
    }

    private async Task<CreateAssemblyResponseDto> CreateFromCompositionAsync(
        AssemblyComposition composition,
        Guid workCenterId,
        Guid? assetId,
        Guid productionLineId,
        Guid operatorId,
        List<Guid> welderIds,
        CancellationToken cancellationToken)
    {
        if (composition.TankSize <= 0)
            throw new ArgumentException("Tank size is required for each resulting assembly.");

        var plantId = await _db.ProductionLines
            .Where(p => p.Id == productionLineId)
            .Select(p => p.PlantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (plantId == Guid.Empty)
            throw new ArgumentException("Production line not found.");

        var alphaCode = await GetNextAlphaCodeAsync(plantId, cancellationToken);
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.ProductType!.SystemTypeName == "assembled" && p.TankSize == composition.TankSize, cancellationToken)
            ?? throw new ArgumentException($"No assembled tank product found for tank size {composition.TankSize}.");

        var assemblySn = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = alphaCode,
            ProductId = product.Id,
            PlantId = plantId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = operatorId
        };
        _db.SerialNumbers.Add(assemblySn);

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = assemblySn.Id,
            WorkCenterId = workCenterId,
            AssetId = assetId == Guid.Empty ? null : assetId,
            ProductionLineId = productionLineId,
            OperatorId = operatorId,
            Timestamp = DateTime.UtcNow
        };
        _db.ProductionRecords.Add(record);

        for (int i = 0; i < composition.Shells.Count; i++)
        {
            var shellSerial = composition.Shells[i];
            var shellSn = await _db.SerialNumbers
                .OrderByDescending(s => s.CreatedAt)
                .FirstOrDefaultAsync(s => s.Serial == shellSerial, cancellationToken);
            if (shellSn == null)
                throw new ArgumentException($"Shell {shellSerial} not found.");

            _db.TraceabilityLogs.Add(new TraceabilityLog
            {
                Id = Guid.NewGuid(),
                FromSerialNumberId = shellSn.Id,
                ToSerialNumberId = assemblySn.Id,
                ProductionRecordId = record.Id,
                Relationship = ShellToAssemblyRelationship,
                TankLocation = $"Shell {i + 1}",
                Quantity = 1,
                Timestamp = DateTime.UtcNow
            });
        }

        AddHeadTrace(
            composition.LeftHead?.LotId,
            composition.LeftHead?.HeatNumber,
            composition.LeftHead?.CoilNumber,
            composition.LeftHead?.LotNumber,
            assemblySn,
            record.Id,
            plantId,
            "Head 1");
        AddHeadTrace(
            composition.RightHead?.LotId,
            composition.RightHead?.HeatNumber,
            composition.RightHead?.CoilNumber,
            composition.RightHead?.LotNumber,
            assemblySn,
            record.Id,
            plantId,
            "Head 2");

        foreach (var welderId in welderIds)
        {
            _db.WelderLogs.Add(new WelderLog
            {
                Id = Guid.NewGuid(),
                ProductionRecordId = record.Id,
                UserId = welderId,
                CharacteristicId = null
            });
        }

        return new CreateAssemblyResponseDto
        {
            Id = assemblySn.Id,
            AlphaCode = assemblySn.Serial,
            Timestamp = record.Timestamp
        };
    }
}
