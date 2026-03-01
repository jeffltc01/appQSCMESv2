using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class WorkCenterService : IWorkCenterService
{
    private const int MaxQueueItemsPerWorkCenter = 5;
    private readonly MesDbContext _db;
    private readonly ILogger<WorkCenterService> _logger;

    public WorkCenterService(MesDbContext db, ILogger<WorkCenterService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IReadOnlyList<WorkCenterDto>> GetWorkCentersAsync(CancellationToken cancellationToken = default)
    {
        var list = await _db.WorkCenters
            .Include(w => w.WorkCenterType)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

        return list.Select(w => new WorkCenterDto
        {
            Id = w.Id,
            Name = w.Name,
            WorkCenterTypeId = w.WorkCenterTypeId,
            WorkCenterTypeName = w.WorkCenterType.Name,
            NumberOfWelders = w.NumberOfWelders,
            DataEntryType = w.DataEntryType,
            MaterialQueueForWCId = w.MaterialQueueForWCId
        }).ToList();
    }

    public async Task<WelderDto?> LookupWelderAsync(string empNo, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.EmployeeNumber == empNo && u.IsActive, cancellationToken);
        if (user == null) return null;
        return new WelderDto { UserId = user.Id, DisplayName = user.DisplayName, EmployeeNumber = user.EmployeeNumber };
    }

    public async Task<WCHistoryDto> GetHistoryAsync(Guid wcId, Guid plantId, Guid productionLineId, string? date, int limit, Guid? assetId = null, CancellationToken cancellationToken = default)
    {
        var tz = await GetPlantTimeZoneAsync(plantId, cancellationToken);

        DateTime localDate;
        if (!string.IsNullOrWhiteSpace(date) && DateTime.TryParse(date, out var dateParsed))
            localDate = dateParsed.Date;
        else
            localDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz).Date;

        var startOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate, tz);
        var endOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), tz);

        var baseFilter = _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId)
            .Where(r => r.ProductionLineId == productionLineId)
            .Where(r => r.ProductionLine.PlantId == plantId);

        if (assetId.HasValue)
            baseFilter = baseFilter.Where(r => r.AssetId == assetId.Value);

        var dayCount = await baseFilter
            .Where(r => r.Timestamp >= startOfDay && r.Timestamp < endOfDay)
            .CountAsync(cancellationToken);
        var dayHourlyCounts = await BuildProductionHourlyCountsAsync(baseFilter, startOfDay, endOfDay, tz, cancellationToken);

        var recentProdRecords = await baseFilter
            .Include(r => r.SerialNumber)
            .Include(r => r.SerialNumber!.Product)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);

        return await GetProductionHistoryItems(wcId, dayCount, dayHourlyCounts, recentProdRecords, tz, cancellationToken);
    }

    private static List<HourlyCountDto> BuildHourlyCountDtos(IEnumerable<DateTime> timestampsUtc, TimeZoneInfo tz)
    {
        var grouped = timestampsUtc
            .GroupBy(t => TimeZoneInfo.ConvertTimeFromUtc(
                t.Kind == DateTimeKind.Utc ? t : DateTime.SpecifyKind(t, DateTimeKind.Utc), tz).Hour)
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<HourlyCountDto>(24);
        for (var h = 0; h < 24; h++)
            result.Add(new HourlyCountDto { Hour = h, Count = grouped.GetValueOrDefault(h) });

        return result;
    }

    private async Task<List<HourlyCountDto>> BuildProductionHourlyCountsAsync(
        IQueryable<ProductionRecord> baseFilter,
        DateTime startOfDay,
        DateTime endOfDay,
        TimeZoneInfo tz,
        CancellationToken cancellationToken)
    {
        var timestamps = await baseFilter
            .Where(r => r.Timestamp >= startOfDay && r.Timestamp < endOfDay)
            .Select(r => r.Timestamp)
            .ToListAsync(cancellationToken);

        return BuildHourlyCountDtos(timestamps, tz);
    }

    private async Task<WCHistoryDto> GetProductionHistoryItems(
        Guid wcId, int dayCount, List<HourlyCountDto> dayHourlyCounts, List<ProductionRecord> recentProdRecords,
        TimeZoneInfo tz, CancellationToken cancellationToken)
    {
        var recordIds = recentProdRecords.Select(r => r.Id).ToList();
        var annotationColors = await GetAnnotationColorsByRecordAsync(recordIds, cancellationToken);

        var wcDataEntryType = await _db.WorkCenters
            .Where(w => w.Id == wcId)
            .Select(w => w.DataEntryType)
            .FirstOrDefaultAsync(cancellationToken);
        var isFitup = string.Equals(wcDataEntryType, "Fitup", StringComparison.OrdinalIgnoreCase);
        var isRolls = string.Equals(wcDataEntryType, "Rolls", StringComparison.OrdinalIgnoreCase);
        var isLongSeam = string.Equals(wcDataEntryType, "Barcode-LongSeam", StringComparison.OrdinalIgnoreCase);
        var isLongSeamInspection = string.Equals(wcDataEntryType, "Barcode-LongSeamInsp", StringComparison.OrdinalIgnoreCase);
        var shouldWrapShellWithAssemblyAlpha = !isFitup && !isRolls && !isLongSeam && !isLongSeamInspection;

        var shellsByAssembly = new Dictionary<Guid, List<string>>();
        if (isFitup)
        {
            var assemblySnIds = recentProdRecords
                .Select(r => r.SerialNumberId)
                .ToList();

            var shellLogs = await _db.TraceabilityLogs
                .Include(t => t.FromSerialNumber)
                .Where(t => t.ToSerialNumberId.HasValue
                    && assemblySnIds.Contains(t.ToSerialNumberId.Value)
                    && (t.Relationship == "ShellToAssembly" || t.Relationship == "shell"))
                .ToListAsync(cancellationToken);

            foreach (var log in shellLogs)
            {
                if (log.ToSerialNumberId == null) continue;
                if (!shellsByAssembly.ContainsKey(log.ToSerialNumberId.Value))
                    shellsByAssembly[log.ToSerialNumberId.Value] = new List<string>();
                if (log.FromSerialNumber?.Serial != null)
                    shellsByAssembly[log.ToSerialNumberId.Value].Add(log.FromSerialNumber.Serial);
            }
        }

        var assemblyByShell = !shouldWrapShellWithAssemblyAlpha
            ? new Dictionary<Guid, string>()
            : await ResolveAssemblyByShellAsync(
                recentProdRecords.Select(r => r.SerialNumberId).Distinct().ToList(),
                cancellationToken);

        var recentRecords = recentProdRecords.Select(r =>
        {
            var alphaOrSerial = r.SerialNumber?.Serial ?? r.Id.ToString("N")[..8];
            var serialOrIdentifier = alphaOrSerial;
            if (isFitup
                && shellsByAssembly.TryGetValue(r.SerialNumberId, out var shells)
                && shells.Count > 0)
            {
                serialOrIdentifier = $"{alphaOrSerial} ({string.Join(", ", shells)})";
            }
            else if (shouldWrapShellWithAssemblyAlpha
                && assemblyByShell.TryGetValue(r.SerialNumberId, out var alphaCode))
            {
                serialOrIdentifier = $"{alphaCode} ({alphaOrSerial})";
            }
            var tankSize = r.SerialNumber?.Product?.TankSize;
            annotationColors.TryGetValue(r.Id, out var color);
            return new WCHistoryEntryDto
            {
                Id = r.Id,
                ProductionRecordId = r.Id,
                Timestamp = DateTime.SpecifyKind(r.Timestamp, DateTimeKind.Utc),
                SerialOrIdentifier = serialOrIdentifier,
                TankSize = tankSize,
                HasAnnotation = color != null,
                AnnotationColor = color
            };
        }).ToList();

        return new WCHistoryDto { DayCount = dayCount, HourlyCounts = dayHourlyCounts, RecentRecords = recentRecords };
    }

    private async Task<WCHistoryDto> GetInspectionHistoryItems(
        Guid wcId, DateTime startOfDay, DateTime endOfDay, int limit,
        TimeZoneInfo tz, CancellationToken cancellationToken)
    {
        var inspDayCount = await _db.InspectionRecords
            .Where(i => i.WorkCenterId == wcId && i.Timestamp >= startOfDay && i.Timestamp < endOfDay)
            .CountAsync(cancellationToken);
        var inspTimestamps = await _db.InspectionRecords
            .Where(i => i.WorkCenterId == wcId && i.Timestamp >= startOfDay && i.Timestamp < endOfDay)
            .Select(i => i.Timestamp)
            .ToListAsync(cancellationToken);
        var inspHourlyCounts = BuildHourlyCountDtos(inspTimestamps, tz);

        var inspRecords = await _db.InspectionRecords
            .Include(i => i.SerialNumber)
            .Include(i => i.SerialNumber!.Product)
            .Where(i => i.WorkCenterId == wcId)
            .OrderByDescending(i => i.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var prodRecordIds = inspRecords.Select(i => i.ProductionRecordId).Distinct().ToList();
        var inspAnnotationColors = await GetAnnotationColorsByRecordAsync(prodRecordIds, cancellationToken);

        var assemblyByShell = await ResolveAssemblyByShellAsync(
            inspRecords.Select(i => i.SerialNumberId).Distinct().ToList(),
            cancellationToken);

        var inspEntries = inspRecords.Select(i =>
        {
            var shellSerial = i.SerialNumber?.Serial ?? i.Id.ToString("N")[..8];
            var serialOrIdentifier = assemblyByShell.TryGetValue(i.SerialNumberId, out var alphaCode)
                ? $"{alphaCode} ({shellSerial})"
                : shellSerial;
            var tankSize = i.SerialNumber?.Product?.TankSize;
            inspAnnotationColors.TryGetValue(i.ProductionRecordId, out var color);
            return new WCHistoryEntryDto
            {
                Id = i.Id,
                ProductionRecordId = i.ProductionRecordId,
                Timestamp = DateTime.SpecifyKind(i.Timestamp, DateTimeKind.Utc),
                SerialOrIdentifier = serialOrIdentifier,
                TankSize = tankSize,
                HasAnnotation = color != null,
                AnnotationColor = color
            };
        }).ToList();

        return new WCHistoryDto { DayCount = inspDayCount, HourlyCounts = inspHourlyCounts, RecentRecords = inspEntries };
    }

    private async Task<Dictionary<Guid, string>> ResolveAssemblyByShellAsync(
        List<Guid> shellSnIds, CancellationToken cancellationToken)
    {
        var assemblyLogs = await _db.TraceabilityLogs
            .Include(t => t.ToSerialNumber)
            .Where(t => t.FromSerialNumberId.HasValue
                && shellSnIds.Contains(t.FromSerialNumberId.Value)
                && t.ToSerialNumberId.HasValue
                && (t.Relationship == "ShellToAssembly" || t.Relationship == "shell"))
            .ToListAsync(cancellationToken);

        var result = new Dictionary<Guid, string>();
        foreach (var log in assemblyLogs)
        {
            if (log.FromSerialNumberId.HasValue && log.ToSerialNumber?.Serial != null)
                result.TryAdd(log.FromSerialNumberId.Value, log.ToSerialNumber.Serial);
        }
        return result;
    }

    public async Task<IReadOnlyList<MaterialQueueItemDto>> GetMaterialQueueAsync(Guid wcId, string? type, Guid? productionLineId = null, CancellationToken cancellationToken = default)
    {
        var query = _db.MaterialQueueItems
            .Include(m => m.SerialNumber)
            .ThenInclude(s => s!.Product)
            .Where(m => m.WorkCenterId == wcId);

        if (productionLineId.HasValue)
            query = query.Where(m => m.ProductionLineId == productionLineId.Value);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(m => m.QueueType == type);

        var items = await query
            .OrderBy(m => m.Position)
            .ToListAsync(cancellationToken);

        var cardColorMap = await ResolveCardColorsAsync(
            items.Select(m => m.CardId).Where(c => c != null).Distinct().ToList()!,
            cancellationToken);

        return items.Select(m => MapQueueItem(m, cardColorMap)).ToList();
    }

    public async Task<QueueAdvanceResponseDto?> AdvanceQueueAsync(Guid wcId, Guid? productionLineId = null, CancellationToken cancellationToken = default)
    {
        var activeQuery = _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .Where(m => m.WorkCenterId == wcId && m.Status == "active");
        if (productionLineId.HasValue)
            activeQuery = activeQuery.Where(m => m.ProductionLineId == productionLineId.Value);

        var active = await activeQuery
            .OrderBy(m => m.Position)
            .FirstOrDefaultAsync(cancellationToken);

        var queuedQuery = _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .Where(m => m.WorkCenterId == wcId && m.Status == "queued");
        if (productionLineId.HasValue)
            queuedQuery = queuedQuery.Where(m => m.ProductionLineId == productionLineId.Value);

        var nextQueued = await queuedQuery
            .OrderBy(m => m.Position)
            .FirstOrDefaultAsync(cancellationToken);

        if (active != null)
        {
            if (active.QuantityCompleted < active.Quantity)
                return MapAdvanceResponse(active);

            active.Status = "completed";
            _db.QueueTransactions.Add(new QueueTransaction
            {
                Id = Guid.NewGuid(),
                WorkCenterId = wcId,
                ProductionLineId = active.ProductionLineId,
                Action = "completed",
                ItemSummary = $"{active.SerialNumber?.Product?.ProductNumber ?? ""} - Qty {active.QuantityCompleted}/{active.Quantity}",
                OperatorName = string.Empty,
                Timestamp = DateTime.UtcNow
            });

            if (nextQueued != null)
            {
                nextQueued.Status = "active";
                await _db.SaveChangesAsync(cancellationToken);
                return MapAdvanceResponse(nextQueued);
            }

            await _db.SaveChangesAsync(cancellationToken);
            return null;
        }

        if (nextQueued != null)
        {
            nextQueued.Status = "active";
            await _db.SaveChangesAsync(cancellationToken);
            return MapAdvanceResponse(nextQueued);
        }

        return null;
    }

    private static QueueAdvanceResponseDto MapAdvanceResponse(MaterialQueueItem m) => new()
    {
        ShellSize = m.SerialNumber?.Product?.TankSize.ToString() ?? string.Empty,
        HeatNumber = m.SerialNumber?.HeatNumber ?? string.Empty,
        CoilNumber = m.SerialNumber?.CoilNumber ?? string.Empty,
        Quantity = m.Quantity,
        QuantityCompleted = m.QuantityCompleted,
        ProductDescription = m.SerialNumber?.Product?.ProductNumber ?? string.Empty
    };

    public Task ReportFaultAsync(Guid wcId, string description, CancellationToken cancellationToken = default)
    {
        _logger.LogWarning("Fault reported: WorkCenter={WorkCenterId}, Description={Description}", wcId, description);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<DefectCodeDto>> GetDefectCodesAsync(Guid wcId, CancellationToken cancellationToken = default)
    {
        var list = await _db.DefectWorkCenters
            .Include(d => d.DefectCode)
            .Where(d => d.WorkCenterId == wcId)
            .Select(d => d.DefectCode)
            .Where(d => d.IsActive)
            .Distinct()
            .OrderBy(d => d.Code)
            .ToListAsync(cancellationToken);

        return list.Select(d => new DefectCodeDto
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name,
            Severity = d.Severity
        }).ToList();
    }

    public async Task<IReadOnlyList<DefectLocationDto>> GetDefectLocationsAsync(Guid wcId, CancellationToken cancellationToken = default)
    {
        var characteristicIds = await _db.CharacteristicWorkCenters
            .Where(c => c.WorkCenterId == wcId)
            .Select(c => c.CharacteristicId)
            .ToListAsync(cancellationToken);

        var locations = await _db.DefectLocations
            .Where(d => d.IsActive && (d.CharacteristicId == null || characteristicIds.Contains(d.CharacteristicId.Value)))
            .OrderBy(d => d.Code)
            .ToListAsync(cancellationToken);

        return locations.Select(d => new DefectLocationDto
        {
            Id = d.Id,
            Code = d.Code,
            Name = d.Name
        }).ToList();
    }

    public async Task<IReadOnlyList<CharacteristicDto>> GetCharacteristicsAsync(Guid wcId, int? tankSize = null, CancellationToken cancellationToken = default)
    {
        var query = _db.CharacteristicWorkCenters
            .Include(c => c.Characteristic)
            .Where(c => c.WorkCenterId == wcId && c.Characteristic.IsActive);

        if (tankSize.HasValue)
            query = query.Where(c => c.Characteristic.MinTankSize == null || c.Characteristic.MinTankSize <= tankSize.Value);

        var list = await query
            .Select(c => c.Characteristic)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return list.Select(c => new CharacteristicDto { Id = c.Id, Code = c.Code, Name = c.Name, MinTankSize = c.MinTankSize }).ToList();
    }

    private async Task<Guid> GetPlantIdForWorkCenter(Guid wcId, CancellationToken cancellationToken)
    {
        var plantId = await _db.WorkCenterProductionLines
            .Where(wpl => wpl.WorkCenterId == wcId)
            .Select(wpl => wpl.ProductionLine.PlantId)
            .FirstOrDefaultAsync(cancellationToken);
        return plantId;
    }

    private async Task<SerialNumber> FindOrCreateSerialAsync(string serialString, Guid plantId, Guid? productId,
        Guid? millVendorId, Guid? processorVendorId, Guid? headsVendorId,
        string? heatNumber, string? coilNumber, string? lotNumber, CancellationToken cancellationToken)
    {
        var existing = await _db.SerialNumbers
            .FirstOrDefaultAsync(s => s.Serial == serialString && s.PlantId == plantId, cancellationToken);
        if (existing != null)
        {
            bool changed = false;
            if (productId.HasValue && existing.ProductId != productId)
            {
                existing.ProductId = productId;
                changed = true;
            }
            if (millVendorId.HasValue && existing.MillVendorId != millVendorId)
            {
                existing.MillVendorId = millVendorId;
                changed = true;
            }
            if (processorVendorId.HasValue && existing.ProcessorVendorId != processorVendorId)
            {
                existing.ProcessorVendorId = processorVendorId;
                changed = true;
            }
            if (headsVendorId.HasValue && existing.HeadsVendorId != headsVendorId)
            {
                existing.HeadsVendorId = headsVendorId;
                changed = true;
            }
            if (heatNumber != null && existing.HeatNumber != heatNumber)
            {
                existing.HeatNumber = heatNumber;
                changed = true;
            }
            if (coilNumber != null && existing.CoilNumber != coilNumber)
            {
                existing.CoilNumber = coilNumber;
                changed = true;
            }
            if (lotNumber != null && existing.LotNumber != lotNumber)
            {
                existing.LotNumber = lotNumber;
                changed = true;
            }
            if (changed)
                await _db.SaveChangesAsync(cancellationToken);
            return existing;
        }

        var serial = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = serialString,
            PlantId = plantId,
            ProductId = productId,
            MillVendorId = millVendorId,
            ProcessorVendorId = processorVendorId,
            HeadsVendorId = headsVendorId,
            HeatNumber = heatNumber,
            CoilNumber = coilNumber,
            LotNumber = lotNumber,
            CreatedAt = DateTime.UtcNow
        };
        _db.SerialNumbers.Add(serial);
        await _db.SaveChangesAsync(cancellationToken);
        return serial;
    }

    public async Task<MaterialQueueItemDto> AddMaterialQueueItemAsync(Guid wcId, CreateMaterialQueueItemDto dto, CancellationToken cancellationToken = default)
    {
        var plantId = await GetPlantIdForWorkCenter(wcId, cancellationToken);
        var serialString = $"Heat {dto.HeatNumber} Coil {dto.CoilNumber}";
        var serial = await FindOrCreateSerialAsync(serialString, plantId, dto.ProductId,
            dto.VendorMillId, dto.VendorProcessorId, null,
            dto.HeatNumber, dto.CoilNumber, dto.LotNumber, cancellationToken);

        var existing = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .Where(m => m.WorkCenterId == wcId
                        && m.QueueType == "rolls"
                        && (m.Status == "queued" || m.Status == "active")
                        && m.SerialNumberId == serial.Id)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing != null)
            return MapQueueItem(existing);

        var currentQueueCount = await _db.MaterialQueueItems
            .CountAsync(
                m => m.WorkCenterId == wcId &&
                     (m.Status == "queued" || m.Status == "active"),
                cancellationToken);
        if (currentQueueCount >= MaxQueueItemsPerWorkCenter)
            throw new InvalidOperationException($"Queue is full. Maximum {MaxQueueItemsPerWorkCenter} items are allowed per work center queue.");

        var maxPos = await _db.MaterialQueueItems
            .Where(m => m.WorkCenterId == wcId)
            .Select(m => (int?)m.Position)
            .MaxAsync(cancellationToken) ?? 0;

        var product = await _db.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken);

        var item = new MaterialQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            Position = maxPos + 1,
            Status = "queued",
            Quantity = dto.Quantity,
            QueueType = "rolls",
            CreatedAt = DateTime.UtcNow,
            SerialNumberId = serial.Id,
            ProductionLineId = dto.ProductionLineId
        };

        _db.MaterialQueueItems.Add(item);

        var productDesc = product?.ProductNumber ?? "Unknown";
        _db.QueueTransactions.Add(new QueueTransaction
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            ProductionLineId = dto.ProductionLineId,
            Action = "added",
            ItemSummary = $"{productDesc} - Heat {dto.HeatNumber} Coil {dto.CoilNumber} - Qty {dto.Quantity}",
            OperatorName = string.Empty,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        item.SerialNumber = serial;
        serial.Product = product;
        return MapQueueItem(item);
    }

    public async Task<MaterialQueueItemDto?> UpdateMaterialQueueItemAsync(Guid wcId, Guid itemId, UpdateMaterialQueueItemDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .FirstOrDefaultAsync(m => m.Id == itemId && m.WorkCenterId == wcId && m.Status == "queued", cancellationToken);
        if (item == null) return null;

        var sn = item.SerialNumber;
        if (sn != null)
        {
            if (dto.HeatNumber != null) sn.HeatNumber = dto.HeatNumber;
            if (dto.CoilNumber != null) sn.CoilNumber = dto.CoilNumber;
            if (dto.LotNumber != null) sn.LotNumber = dto.LotNumber;
            if (dto.ProductId.HasValue) sn.ProductId = dto.ProductId.Value;
            if (dto.VendorMillId.HasValue) sn.MillVendorId = dto.VendorMillId;
            if (dto.VendorProcessorId.HasValue) sn.ProcessorVendorId = dto.VendorProcessorId;
        }
        if (dto.Quantity.HasValue) item.Quantity = dto.Quantity.Value;

        var productDesc = sn?.Product?.ProductNumber ?? string.Empty;
        var heat = sn?.HeatNumber ?? string.Empty;
        var coil = sn?.CoilNumber ?? string.Empty;

        _db.QueueTransactions.Add(new QueueTransaction
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            ProductionLineId = item.ProductionLineId,
            Action = "updated",
            ItemSummary = $"{productDesc} - Heat {heat} Coil {coil} - Qty {item.Quantity}",
            OperatorName = string.Empty,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return MapQueueItem(item);
    }

    public async Task<bool> DeleteMaterialQueueItemAsync(Guid wcId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .FirstOrDefaultAsync(m => m.Id == itemId && m.WorkCenterId == wcId && m.Status == "queued", cancellationToken);
        if (item == null) return false;

        var productDesc = item.SerialNumber?.Product?.ProductNumber ?? string.Empty;
        var heat = item.SerialNumber?.HeatNumber ?? string.Empty;
        var coil = item.SerialNumber?.CoilNumber ?? string.Empty;

        _db.QueueTransactions.Add(new QueueTransaction
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            ProductionLineId = item.ProductionLineId,
            Action = "removed",
            ItemSummary = $"{productDesc} - Heat {heat} Coil {coil}",
            OperatorName = string.Empty,
            Timestamp = DateTime.UtcNow
        });

        _db.MaterialQueueItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<MaterialQueueItemDto> AddFitupQueueItemAsync(Guid wcId, CreateFitupQueueItemDto dto, CancellationToken cancellationToken = default)
    {
        var plantId = await GetPlantIdForWorkCenter(wcId, cancellationToken);
        string serialString;
        if (!string.IsNullOrEmpty(dto.LotNumber))
            serialString = $"Lot {dto.LotNumber}";
        else
            serialString = $"Heat {dto.HeatNumber} Coil {dto.CoilSlabNumber}";

        var serial = await FindOrCreateSerialAsync(serialString, plantId, dto.ProductId,
            null, null, dto.VendorHeadId,
            dto.HeatNumber, dto.CoilSlabNumber, dto.LotNumber, cancellationToken);

        var existing = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .Where(m => m.WorkCenterId == wcId
                        && m.QueueType == "fitup"
                        && (m.Status == "queued" || m.Status == "active")
                        && m.SerialNumberId == serial.Id
                        && m.CardId == dto.CardCode)
            .OrderByDescending(m => m.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing != null)
            return MapQueueItem(existing);

        var currentQueueCount = await _db.MaterialQueueItems
            .CountAsync(
                m => m.WorkCenterId == wcId &&
                     (m.Status == "queued" || m.Status == "active"),
                cancellationToken);
        if (currentQueueCount >= MaxQueueItemsPerWorkCenter)
            throw new InvalidOperationException($"Queue is full. Maximum {MaxQueueItemsPerWorkCenter} items are allowed per work center queue.");

        var existingCard = await _db.MaterialQueueItems
            .AnyAsync(
                m => m.WorkCenterId == wcId
                     && m.QueueType == "fitup"
                     && m.CardId == dto.CardCode
                     && m.Status == "queued"
                     && (m.ProductionLineId == dto.ProductionLineId
                         || (!m.ProductionLineId.HasValue && !dto.ProductionLineId.HasValue)),
                cancellationToken);
        if (existingCard)
            throw new InvalidOperationException("This card is already assigned to an active queue entry");

        var card = await _db.BarcodeCards
            .FirstOrDefaultAsync(b => b.CardValue == dto.CardCode, cancellationToken);

        var maxPos = await _db.MaterialQueueItems
            .Where(m => m.WorkCenterId == wcId)
            .Select(m => (int?)m.Position)
            .MaxAsync(cancellationToken) ?? 0;

        var product = await _db.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken);

        var item = new MaterialQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            Position = maxPos + 1,
            Status = "queued",
            Quantity = 1,
            CardId = dto.CardCode,
            CardColor = card?.Color,
            QueueType = "fitup",
            CreatedAt = DateTime.UtcNow,
            SerialNumberId = serial.Id,
            ProductionLineId = dto.ProductionLineId
        };

        _db.MaterialQueueItems.Add(item);

        var productDesc = product?.ProductNumber ?? "Unknown";
        var summaryId = !string.IsNullOrEmpty(dto.LotNumber) ? $"Lot {dto.LotNumber}" : $"Heat {dto.HeatNumber}";
        _db.QueueTransactions.Add(new QueueTransaction
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            ProductionLineId = dto.ProductionLineId,
            Action = "added",
            ItemSummary = $"{productDesc} - {summaryId} - Card {dto.CardCode}",
            OperatorName = string.Empty,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);

        item.SerialNumber = serial;
        serial.Product = product;
        return MapQueueItem(item);
    }

    public async Task<MaterialQueueItemDto?> UpdateFitupQueueItemAsync(Guid wcId, Guid itemId, UpdateFitupQueueItemDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .FirstOrDefaultAsync(m => m.Id == itemId && m.WorkCenterId == wcId && m.Status == "queued", cancellationToken);
        if (item == null) return null;

        if (dto.CardCode != null && dto.CardCode != item.CardId)
        {
            var existingCard = await _db.MaterialQueueItems
                .AnyAsync(
                    m => m.WorkCenterId == wcId
                         && m.QueueType == "fitup"
                         && m.CardId == dto.CardCode
                         && m.Status == "queued"
                         && m.Id != itemId
                         && (m.ProductionLineId == item.ProductionLineId
                             || (!m.ProductionLineId.HasValue && !item.ProductionLineId.HasValue)),
                    cancellationToken);
            if (existingCard)
                throw new InvalidOperationException("This card is already assigned to an active queue entry");

            var card = await _db.BarcodeCards.FirstOrDefaultAsync(b => b.CardValue == dto.CardCode, cancellationToken);
            item.CardId = dto.CardCode;
            item.CardColor = card?.Color;
        }

        var sn = item.SerialNumber;
        if (sn != null)
        {
            if (dto.ProductId.HasValue) sn.ProductId = dto.ProductId.Value;
            if (dto.VendorHeadId.HasValue) sn.HeadsVendorId = dto.VendorHeadId;
            if (dto.LotNumber != null) sn.LotNumber = dto.LotNumber;
            if (dto.HeatNumber != null) sn.HeatNumber = dto.HeatNumber;
            if (dto.CoilSlabNumber != null) sn.CoilNumber = dto.CoilSlabNumber;
        }

        var productDesc = sn?.Product?.ProductNumber ?? string.Empty;
        _db.QueueTransactions.Add(new QueueTransaction
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            ProductionLineId = item.ProductionLineId,
            Action = "updated",
            ItemSummary = $"{productDesc} - Card {item.CardId}",
            OperatorName = string.Empty,
            Timestamp = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(cancellationToken);
        return MapQueueItem(item);
    }

    public async Task<bool> DeleteFitupQueueItemAsync(Guid wcId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .FirstOrDefaultAsync(m => m.Id == itemId && m.WorkCenterId == wcId && m.Status == "queued", cancellationToken);
        if (item == null) return false;

        var productDesc = item.SerialNumber?.Product?.ProductNumber ?? string.Empty;
        _db.QueueTransactions.Add(new QueueTransaction
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            ProductionLineId = item.ProductionLineId,
            Action = "removed",
            ItemSummary = $"{productDesc} - Card {item.CardId}",
            OperatorName = string.Empty,
            Timestamp = DateTime.UtcNow
        });

        _db.MaterialQueueItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<BarcodeCardDto>> GetBarcodeCardsAsync(Guid? plantId, CancellationToken cancellationToken = default)
    {
        var cards = await _db.BarcodeCards.ToListAsync(cancellationToken);
        var assignedCardIds = await _db.MaterialQueueItems
            .Where(m => m.CardId != null && m.Status == "queued")
            .Select(m => m.CardId)
            .ToHashSetAsync(cancellationToken);

        return cards.Select(c => new BarcodeCardDto
        {
            Id = c.Id,
            CardValue = c.CardValue,
            Color = c.Color,
            ColorName = c.Description,
            IsAssigned = assignedCardIds.Contains(c.CardValue)
        }).ToList();
    }

    private static MaterialQueueItemDto MapQueueItem(MaterialQueueItem m,
        Dictionary<string, string?>? cardColorMap = null)
    {
        string? color = m.CardColor;
        if (cardColorMap != null && m.CardId != null)
            cardColorMap.TryGetValue(m.CardId, out color);

        return new MaterialQueueItemDto
        {
            Id = m.Id,
            Position = m.Position,
            Status = m.Status,
            ProductDescription = m.SerialNumber?.Product?.ProductNumber ?? string.Empty,
            ShellSize = m.SerialNumber?.Product?.TankSize.ToString(),
            HeatNumber = m.SerialNumber?.HeatNumber ?? string.Empty,
            CoilNumber = m.SerialNumber?.CoilNumber ?? string.Empty,
            LotNumber = m.SerialNumber?.LotNumber,
            Quantity = m.Quantity,
            QuantityCompleted = m.QuantityCompleted,
            ProductId = m.SerialNumber?.ProductId,
            VendorMillId = m.SerialNumber?.MillVendorId,
            VendorProcessorId = m.SerialNumber?.ProcessorVendorId,
            VendorHeadId = m.SerialNumber?.HeadsVendorId,
            CardId = m.CardId,
            CardColor = color,
            CreatedAt = m.CreatedAt
        };
    }

    private static string StripCardPrefix(string cardId) =>
        cardId.StartsWith("KC;", StringComparison.OrdinalIgnoreCase) ? cardId[3..] : cardId;

    private async Task<Dictionary<string, string?>> ResolveCardColorsAsync(
        List<string> cardIds, CancellationToken cancellationToken)
    {
        if (cardIds.Count == 0)
            return new Dictionary<string, string?>();

        var stripped = cardIds.Select(StripCardPrefix).Distinct().ToList();
        var dbColors = await _db.BarcodeCards
            .Where(b => stripped.Contains(b.CardValue))
            .ToDictionaryAsync(b => b.CardValue, b => (string?)b.Color, cancellationToken);

        var result = new Dictionary<string, string?>();
        foreach (var id in cardIds)
        {
            var key = StripCardPrefix(id);
            if (dbColors.TryGetValue(key, out var color))
                result[id] = color;
        }
        return result;
    }

    /// <summary>
    /// Returns a map of ProductionRecordId → DisplayColor for the highest-priority
    /// annotation on each record.  Priority: RequiresResolution types first, then
    /// most-recently created.
    /// </summary>
    private async Task<Dictionary<Guid, string>> GetAnnotationColorsByRecordAsync(
        List<Guid> recordIds, CancellationToken cancellationToken)
    {
        if (recordIds.Count == 0)
            return new Dictionary<Guid, string>();

        var annotations = await _db.Annotations
            .Include(a => a.AnnotationType)
            .Where(a => a.ProductionRecordId != null && recordIds.Contains(a.ProductionRecordId.Value))
            .ToListAsync(cancellationToken);

        return annotations
            .GroupBy(a => a.ProductionRecordId!.Value)
            .ToDictionary(
                g => g.Key,
                g => g.OrderByDescending(a => a.AnnotationType.RequiresResolution)
                      .ThenByDescending(a => a.CreatedAt)
                      .First()
                      .AnnotationType.DisplayColor ?? "#212529");
    }

    private async Task<TimeZoneInfo> GetPlantTimeZoneAsync(Guid plantId, CancellationToken cancellationToken)
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

    public async Task<KanbanCardLookupDto?> GetCardLookupAsync(
        Guid workCenterId,
        Guid productionLineId,
        string cardId,
        CancellationToken cancellationToken = default)
    {
        var prefixed = $"KC;{cardId}";
        var queueItem = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .Where(m =>
                m.Status == "queued"
                && m.QueueType == "fitup"
                && m.WorkCenterId == workCenterId
                && m.ProductionLineId == productionLineId)
            .FirstOrDefaultAsync(m => m.CardId == cardId || m.CardId == prefixed, cancellationToken);
        if (queueItem != null)
        {
            var sn = queueItem.SerialNumber;
            var resolvedColor = queueItem.CardColor;
            if (resolvedColor == null && queueItem.CardId != null)
            {
                var lookupVal = StripCardPrefix(queueItem.CardId);
                resolvedColor = (await _db.BarcodeCards
                    .Where(b => b.CardValue == lookupVal)
                    .Select(b => b.Color)
                    .FirstOrDefaultAsync(cancellationToken));
            }
            return new KanbanCardLookupDto
            {
                HeatNumber = sn?.HeatNumber ?? string.Empty,
                CoilNumber = sn?.CoilNumber ?? string.Empty,
                LotNumber = sn?.LotNumber,
                ProductDescription = sn?.Product?.ProductNumber ?? string.Empty,
                CardColor = resolvedColor,
                TankSize = sn?.Product?.TankSize
            };
        }

        return null;
    }

    public async Task<IReadOnlyList<QueueTransactionDto>> GetQueueTransactionsAsync(Guid wcId, Guid productionLineId, int limit, Guid? plantId = null, string? action = null, CancellationToken cancellationToken = default)
    {
        var tz = plantId.HasValue
            ? await GetPlantTimeZoneAsync(plantId.Value, cancellationToken)
            : TimeZoneInfo.Utc;

        var query = _db.QueueTransactions
            .Where(qt => qt.WorkCenterId == wcId)
            .Where(qt => qt.ProductionLineId == productionLineId);

        if (!string.IsNullOrEmpty(action))
            query = query.Where(qt => qt.Action == action);

        var transactions = await query
            .OrderByDescending(qt => qt.Timestamp)
            .Take(limit)
            .Select(qt => new QueueTransactionDto
            {
                Id = qt.Id,
                Action = qt.Action,
                ItemSummary = qt.ItemSummary,
                OperatorName = qt.OperatorName,
                Timestamp = qt.Timestamp
            })
            .ToListAsync(cancellationToken);

        foreach (var tx in transactions)
            tx.Timestamp = TimeZoneInfo.ConvertTimeFromUtc(tx.Timestamp, tz);

        return transactions;
    }
}
