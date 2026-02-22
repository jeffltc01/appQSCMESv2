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
        var plantId = await GetPlantIdForWorkCenter(wcId, cancellationToken);

        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.EmployeeNumber == empNo && u.IsActive
                && (plantId == Guid.Empty || u.DefaultSiteId == plantId), cancellationToken);
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

    public async Task<WCHistoryDto> GetHistoryAsync(Guid wcId, Guid plantId, string date, int limit, Guid? assetId = null, CancellationToken cancellationToken = default)
    {
        if (!DateTime.TryParse(date, out var dateParsed))
            dateParsed = DateTime.UtcNow.Date;

        var tz = await GetPlantTimeZoneAsync(plantId, cancellationToken);
        var localDate = dateParsed.Date;
        var startOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate, tz);
        var endOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), tz);

        var query = _db.ProductionRecords
            .Include(r => r.SerialNumber)
            .Include(r => r.SerialNumber!.Product)
            .Where(r => r.WorkCenterId == wcId && r.Timestamp >= startOfDay && r.Timestamp < endOfDay)
            .Where(r => r.ProductionLine.PlantId == plantId);

        if (assetId.HasValue)
            query = query.Where(r => r.AssetId == assetId.Value);

        var records = await query
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var dayCount = await query.CountAsync(cancellationToken);

        var recordIds = records.Select(r => r.Id).ToList();
        var annotationsExist = await _db.Annotations
            .Where(a => recordIds.Contains(a.ProductionRecordId))
            .Select(a => a.ProductionRecordId)
            .Distinct()
            .ToHashSetAsync(cancellationToken);

        var recentRecords = records.Select(r =>
        {
            var serialOrIdentifier = r.SerialNumber?.Serial ?? r.Id.ToString("N")[..8];
            var tankSize = r.SerialNumber?.Product?.TankSize;
            return new WCHistoryEntryDto
            {
                Id = r.Id,
                Timestamp = r.Timestamp,
                SerialOrIdentifier = serialOrIdentifier,
                TankSize = tankSize,
                HasAnnotation = annotationsExist.Contains(r.Id)
            };
        }).ToList();

        return new WCHistoryDto { DayCount = dayCount, RecentRecords = recentRecords };
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

        return items.Select(MapQueueItem).ToList();
    }

    public async Task<QueueAdvanceResponseDto?> AdvanceQueueAsync(Guid wcId, CancellationToken cancellationToken = default)
    {
        var active = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .Where(m => m.WorkCenterId == wcId && m.Status == "active")
            .OrderBy(m => m.Position)
            .FirstOrDefaultAsync(cancellationToken);

        if (active != null)
            return MapAdvanceResponse(active);

        var nextQueued = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .Where(m => m.WorkCenterId == wcId && m.Status == "queued")
            .OrderBy(m => m.Position)
            .FirstOrDefaultAsync(cancellationToken);

        if (nextQueued == null)
            return null;

        nextQueued.Status = "active";
        await _db.SaveChangesAsync(cancellationToken);

        return MapAdvanceResponse(nextQueued);
    }

    private static QueueAdvanceResponseDto MapAdvanceResponse(MaterialQueueItem m) => new()
    {
        ShellSize = m.SerialNumber?.Product?.TankSize.ToString() ?? string.Empty,
        HeatNumber = m.SerialNumber?.HeatNumber ?? string.Empty,
        CoilNumber = m.SerialNumber?.CoilNumber ?? string.Empty,
        Quantity = m.Quantity,
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

    public async Task<IReadOnlyList<CharacteristicDto>> GetCharacteristicsAsync(Guid wcId, CancellationToken cancellationToken = default)
    {
        var list = await _db.CharacteristicWorkCenters
            .Include(c => c.Characteristic)
            .Where(c => c.WorkCenterId == wcId)
            .Select(c => c.Characteristic)
            .OrderBy(c => c.Name)
            .ToListAsync(cancellationToken);

        return list.Select(c => new CharacteristicDto { Id = c.Id, Name = c.Name }).ToList();
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
        if (existing != null) return existing;

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
            dto.HeatNumber, null, dto.LotNumber, cancellationToken);

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

    private static MaterialQueueItemDto MapQueueItem(MaterialQueueItem m) => new()
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
        CardColor = m.CardColor,
        CreatedAt = m.CreatedAt
    };

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

    public async Task<KanbanCardLookupDto?> GetCardLookupAsync(string cardId, CancellationToken cancellationToken = default)
    {
        var queueItem = await _db.MaterialQueueItems
            .Include(m => m.SerialNumber).ThenInclude(s => s!.Product)
            .FirstOrDefaultAsync(m => m.CardId == cardId, cancellationToken);
        if (queueItem != null)
        {
            var sn = queueItem.SerialNumber;
            return new KanbanCardLookupDto
            {
                HeatNumber = sn?.HeatNumber ?? string.Empty,
                CoilNumber = sn?.CoilNumber ?? string.Empty,
                LotNumber = sn?.LotNumber,
                ProductDescription = sn?.Product?.ProductNumber ?? string.Empty,
                CardColor = queueItem.CardColor
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
