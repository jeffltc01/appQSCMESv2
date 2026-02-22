using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class WorkCenterService : IWorkCenterService
{
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

    public async Task<IReadOnlyList<WelderDto>> GetWeldersAsync(Guid wcId, CancellationToken cancellationToken = default)
    {
        var welders = await _db.WorkCenterWelders
            .Include(w => w.User)
            .Where(w => w.WorkCenterId == wcId)
            .OrderBy(w => w.AssignedAt)
            .ToListAsync(cancellationToken);

        return welders.Select(w => new WelderDto
        {
            UserId = w.UserId,
            DisplayName = w.User.DisplayName,
            EmployeeNumber = w.User.EmployeeNumber
        }).ToList();
    }

    public async Task<WelderDto?> LookupWelderAsync(string empNo, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.EmployeeNumber == empNo && u.IsActive, cancellationToken);
        if (user == null) return null;
        return new WelderDto { UserId = user.Id, DisplayName = user.DisplayName, EmployeeNumber = user.EmployeeNumber };
    }

    public async Task<WelderDto?> AddWelderAsync(Guid wcId, string empNo, CancellationToken cancellationToken = default)
    {
        var plantIds = await _db.WorkCenterProductionLines
            .Where(wpl => wpl.WorkCenterId == wcId)
            .Select(wpl => wpl.ProductionLine.PlantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.EmployeeNumber == empNo && u.IsActive
                && (plantIds.Count == 0 || plantIds.Contains(u.DefaultSiteId)), cancellationToken);
        if (user == null)
            return null;

        var existing = await _db.WorkCenterWelders
            .AnyAsync(w => w.WorkCenterId == wcId && w.UserId == user.Id, cancellationToken);
        if (existing)
            return new WelderDto
            {
                UserId = user.Id,
                DisplayName = user.DisplayName,
                EmployeeNumber = user.EmployeeNumber
            };

        _db.WorkCenterWelders.Add(new WorkCenterWelder
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            UserId = user.Id,
            AssignedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync(cancellationToken);

        return new WelderDto
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            EmployeeNumber = user.EmployeeNumber
        };
    }

    public async Task<bool> RemoveWelderAsync(Guid wcId, Guid userId, CancellationToken cancellationToken = default)
    {
        var entry = await _db.WorkCenterWelders
            .FirstOrDefaultAsync(w => w.WorkCenterId == wcId && w.UserId == userId, cancellationToken);
        if (entry == null)
            return false;

        _db.WorkCenterWelders.Remove(entry);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<WCHistoryDto> GetHistoryAsync(Guid wcId, Guid plantId, string? date, int limit, Guid? assetId = null, CancellationToken cancellationToken = default)
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
            .Where(r => r.ProductionLine.PlantId == plantId);

        if (assetId.HasValue)
            baseFilter = baseFilter.Where(r => r.AssetId == assetId.Value);

        var dayCount = await baseFilter
            .Where(r => r.Timestamp >= startOfDay && r.Timestamp < endOfDay)
            .CountAsync(cancellationToken);

        var recentProdRecords = await baseFilter
            .Include(r => r.SerialNumber)
            .Include(r => r.SerialNumber!.Product)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);

        if (recentProdRecords.Count > 0)
        {
            var recordIds = recentProdRecords.Select(r => r.Id).ToList();
            var annotationColors = await GetAnnotationColorsByRecordAsync(recordIds, cancellationToken);

            var wcDataEntryType = await _db.WorkCenters
                .Where(w => w.Id == wcId)
                .Select(w => w.DataEntryType)
                .FirstOrDefaultAsync(cancellationToken);
            var isFitup = string.Equals(wcDataEntryType, "Fitup", StringComparison.OrdinalIgnoreCase);

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
                        && t.Relationship == "shell")
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
                var tankSize = r.SerialNumber?.Product?.TankSize;
                annotationColors.TryGetValue(r.Id, out var color);
                return new WCHistoryEntryDto
                {
                    Id = r.Id,
                    Timestamp = ToLocal(r.Timestamp, tz),
                    SerialOrIdentifier = serialOrIdentifier,
                    TankSize = tankSize,
                    HasAnnotation = color != null,
                    AnnotationColor = color
                };
            }).ToList();

            return new WCHistoryDto { DayCount = dayCount, RecentRecords = recentRecords };
        }

        var inspDayCount = await _db.InspectionRecords
            .Where(i => i.WorkCenterId == wcId && i.Timestamp >= startOfDay && i.Timestamp < endOfDay)
            .CountAsync(cancellationToken);

        var inspRecords = await _db.InspectionRecords
            .Include(i => i.SerialNumber)
            .Include(i => i.SerialNumber!.Product)
            .Where(i => i.WorkCenterId == wcId)
            .OrderByDescending(i => i.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var prodRecordIds = inspRecords.Select(i => i.ProductionRecordId).Distinct().ToList();
        var inspAnnotationColors = await GetAnnotationColorsByRecordAsync(prodRecordIds, cancellationToken);

        var inspEntries = inspRecords.Select(i =>
        {
            var serialOrIdentifier = i.SerialNumber?.Serial ?? i.Id.ToString("N")[..8];
            var tankSize = i.SerialNumber?.Product?.TankSize;
            inspAnnotationColors.TryGetValue(i.ProductionRecordId, out var color);
            return new WCHistoryEntryDto
            {
                Id = i.Id,
                Timestamp = ToLocal(i.Timestamp, tz),
                SerialOrIdentifier = serialOrIdentifier,
                TankSize = tankSize,
                HasAnnotation = color != null,
                AnnotationColor = color
            };
        }).ToList();

        return new WCHistoryDto { DayCount = inspDayCount, RecentRecords = inspEntries };
    }

    public async Task<IReadOnlyList<MaterialQueueItemDto>> GetMaterialQueueAsync(Guid wcId, string? type, CancellationToken cancellationToken = default)
    {
        var query = _db.MaterialQueueItems
            .Include(m => m.SerialNumber)
            .ThenInclude(s => s!.Product)
            .Where(m => m.WorkCenterId == wcId);

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

    public async Task<QueueAdvanceResponseDto?> AdvanceQueueAsync(Guid wcId, CancellationToken cancellationToken = default)
    {
        var active = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .Where(m => m.WorkCenterId == wcId && m.Status == "active")
            .OrderBy(m => m.Position)
            .FirstOrDefaultAsync(cancellationToken);

        var nextQueued = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .Where(m => m.WorkCenterId == wcId && m.Status == "queued")
            .OrderBy(m => m.Position)
            .FirstOrDefaultAsync(cancellationToken);

        if (active != null && nextQueued != null)
        {
            active.Status = "completed";
            _db.QueueTransactions.Add(new QueueTransaction
            {
                Id = Guid.NewGuid(),
                WorkCenterId = wcId,
                Action = "completed",
                ItemSummary = $"{active.SerialNumber?.Product?.ProductNumber ?? ""} - Qty {active.QuantityCompleted}/{active.Quantity}",
                OperatorName = string.Empty,
                Timestamp = DateTime.UtcNow
            });

            nextQueued.Status = "active";
            await _db.SaveChangesAsync(cancellationToken);
            return MapAdvanceResponse(nextQueued);
        }

        if (active != null)
            return MapAdvanceResponse(active);

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
            .Where(d => d.IsActive && d.CharacteristicId != null && characteristicIds.Contains(d.CharacteristicId!.Value))
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
            .Where(c => c.WorkCenterId == wcId);

        if (tankSize.HasValue)
            query = query.Where(c => c.Characteristic.MinTankSize == null || c.Characteristic.MinTankSize <= tankSize.Value);

        var list = await query
            .Select(c => c.Characteristic)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return list.Select(c => new CharacteristicDto { Id = c.Id, Name = c.Name, MinTankSize = c.MinTankSize }).ToList();
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
        var maxPos = await _db.MaterialQueueItems
            .Where(m => m.WorkCenterId == wcId)
            .Select(m => (int?)m.Position)
            .MaxAsync(cancellationToken) ?? 0;

        var product = await _db.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken);

        var plantId = await GetPlantIdForWorkCenter(wcId, cancellationToken);
        var serialString = $"Heat {dto.HeatNumber} Coil {dto.CoilNumber}";
        var serial = await FindOrCreateSerialAsync(serialString, plantId, dto.ProductId,
            dto.VendorMillId, dto.VendorProcessorId, null,
            dto.HeatNumber, dto.CoilNumber, dto.LotNumber, cancellationToken);

        var item = new MaterialQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            Position = maxPos + 1,
            Status = "queued",
            Quantity = dto.Quantity,
            QueueType = "rolls",
            CreatedAt = DateTime.UtcNow,
            SerialNumberId = serial.Id
        };

        _db.MaterialQueueItems.Add(item);

        var productDesc = product?.ProductNumber ?? "Unknown";
        _db.QueueTransactions.Add(new QueueTransaction
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
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
        var existingCard = await _db.MaterialQueueItems
            .AnyAsync(m => m.CardId == dto.CardCode && m.Status == "queued", cancellationToken);
        if (existingCard)
            throw new InvalidOperationException("This card is already assigned to an active queue entry");

        var card = await _db.BarcodeCards
            .FirstOrDefaultAsync(b => b.CardValue == dto.CardCode, cancellationToken);

        var maxPos = await _db.MaterialQueueItems
            .Where(m => m.WorkCenterId == wcId)
            .Select(m => (int?)m.Position)
            .MaxAsync(cancellationToken) ?? 0;

        var product = await _db.Products.FindAsync(new object[] { dto.ProductId }, cancellationToken);

        var plantId = await GetPlantIdForWorkCenter(wcId, cancellationToken);
        string serialString;
        if (!string.IsNullOrEmpty(dto.LotNumber))
            serialString = $"Lot {dto.LotNumber}";
        else
            serialString = $"Heat {dto.HeatNumber} Coil {dto.CoilSlabNumber}";

        var serial = await FindOrCreateSerialAsync(serialString, plantId, dto.ProductId,
            null, null, dto.VendorHeadId,
            dto.HeatNumber, dto.CoilSlabNumber, dto.LotNumber, cancellationToken);

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
            SerialNumberId = serial.Id
        };

        _db.MaterialQueueItems.Add(item);

        var productDesc = product?.ProductNumber ?? "Unknown";
        var summaryId = !string.IsNullOrEmpty(dto.LotNumber) ? $"Lot {dto.LotNumber}" : $"Heat {dto.HeatNumber}";
        _db.QueueTransactions.Add(new QueueTransaction
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
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
                .AnyAsync(m => m.CardId == dto.CardCode && m.Status == "queued" && m.Id != itemId, cancellationToken);
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
            ProductId = m.SerialNumber?.ProductId,
            VendorMillId = m.SerialNumber?.MillVendorId,
            VendorProcessorId = m.SerialNumber?.ProcessorVendorId,
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

    private static DateTime ToLocal(DateTime utc, TimeZoneInfo tz)
    {
        var spec = utc.Kind == DateTimeKind.Utc ? utc : DateTime.SpecifyKind(utc, DateTimeKind.Utc);
        return TimeZoneInfo.ConvertTimeFromUtc(spec, tz);
    }

    public async Task<KanbanCardLookupDto?> GetCardLookupAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var prefixed = $"KC;{cardId}";
        var queueItem = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .Where(m => m.Status == "queued")
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

        var card = await _db.BarcodeCards
            .FirstOrDefaultAsync(b => b.CardValue == cardId, cancellationToken);
        if (card != null)
            return new KanbanCardLookupDto
            {
                HeatNumber = string.Empty,
                CoilNumber = string.Empty,
                ProductDescription = card.Description ?? card.CardValue,
                CardColor = card.Color
            };

        return null;
    }

    public async Task<IReadOnlyList<QueueTransactionDto>> GetQueueTransactionsAsync(Guid wcId, int limit, Guid? plantId = null, CancellationToken cancellationToken = default)
    {
        var tz = plantId.HasValue
            ? await GetPlantTimeZoneAsync(plantId.Value, cancellationToken)
            : TimeZoneInfo.Utc;

        var transactions = await _db.QueueTransactions
            .Where(qt => qt.WorkCenterId == wcId)
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
