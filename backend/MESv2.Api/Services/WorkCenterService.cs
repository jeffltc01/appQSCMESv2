using System.Collections.Concurrent;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class WorkCenterService : IWorkCenterService
{
    private static readonly ConcurrentDictionary<Guid, List<WelderDto>> WeldersByWorkCenter = new();

    private readonly MesDbContext _db;

    public WorkCenterService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<WorkCenterDto>> GetWorkCentersAsync(string siteCode, CancellationToken cancellationToken = default)
    {
        var list = await _db.WorkCenters
            .Include(w => w.Plant)
            .Include(w => w.WorkCenterType)
            .Where(w => w.Plant.Code == siteCode)
            .OrderBy(w => w.Name)
            .ToListAsync(cancellationToken);

        return list.Select(w => new WorkCenterDto
        {
            Id = w.Id,
            Name = w.Name,
            PlantId = w.PlantId,
            WorkCenterTypeId = w.WorkCenterTypeId,
            WorkCenterTypeName = w.WorkCenterType.Name,
            NumberOfWelders = w.NumberOfWelders,
            ProductionLineId = w.ProductionLineId,
            DataEntryType = w.DataEntryType,
            MaterialQueueForWCId = w.MaterialQueueForWCId
        }).ToList();
    }

    public Task<IReadOnlyList<WelderDto>> GetWeldersAsync(Guid wcId, CancellationToken cancellationToken = default)
    {
        var list = WeldersByWorkCenter.GetValueOrDefault(wcId) ?? new List<WelderDto>();
        return Task.FromResult<IReadOnlyList<WelderDto>>(list);
    }

    public async Task<WelderDto?> AddWelderAsync(Guid wcId, string empNo, CancellationToken cancellationToken = default)
    {
        var user = await _db.Users
            .FirstOrDefaultAsync(u => u.EmployeeNumber == empNo && u.IsActive && u.IsCertifiedWelder, cancellationToken);
        if (user == null)
            return null;

        var welder = new WelderDto
        {
            UserId = user.Id,
            DisplayName = user.DisplayName,
            EmployeeNumber = user.EmployeeNumber
        };

        var list = WeldersByWorkCenter.GetOrAdd(wcId, _ => new List<WelderDto>());
        lock (list)
        {
            if (list.Any(w => w.UserId == user.Id))
                return welder;
            list.Add(welder);
        }

        return welder;
    }

    public Task<bool> RemoveWelderAsync(Guid wcId, Guid userId, CancellationToken cancellationToken = default)
    {
        if (!WeldersByWorkCenter.TryGetValue(wcId, out var list))
            return Task.FromResult(false);

        lock (list)
        {
            var removed = list.RemoveAll(w => w.UserId == userId) > 0;
            return Task.FromResult(removed);
        }
    }

    public async Task<WCHistoryDto> GetHistoryAsync(Guid wcId, string date, int limit, CancellationToken cancellationToken = default)
    {
        if (!DateTime.TryParse(date, out var dateParsed))
            dateParsed = DateTime.UtcNow.Date;

        var tz = await GetPlantTimeZoneForWorkCenterAsync(wcId, cancellationToken);
        var localDate = dateParsed.Date;
        var startOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate, tz);
        var endOfDay = TimeZoneInfo.ConvertTimeToUtc(localDate.AddDays(1), tz);

        var records = await _db.ProductionRecords
            .Include(r => r.SerialNumber)
            .Include(r => r.SerialNumber!.Product)
            .Where(r => r.WorkCenterId == wcId && r.Timestamp >= startOfDay && r.Timestamp < endOfDay)
            .OrderByDescending(r => r.Timestamp)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var dayCount = await _db.ProductionRecords
            .Where(r => r.WorkCenterId == wcId && r.Timestamp >= startOfDay && r.Timestamp < endOfDay)
            .CountAsync(cancellationToken);

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
            .Where(m => m.WorkCenterId == wcId);

        if (!string.IsNullOrEmpty(type))
            query = query.Where(m => m.Status == type);

        var items = await query
            .OrderBy(m => m.Position)
            .ToListAsync(cancellationToken);

        return items.Select(MapQueueItem).ToList();
    }

    public async Task<QueueAdvanceResponseDto?> AdvanceQueueAsync(Guid wcId, CancellationToken cancellationToken = default)
    {
        var active = await _db.MaterialQueueItems
            .Where(m => m.WorkCenterId == wcId && m.Status == "active")
            .OrderBy(m => m.Position)
            .FirstOrDefaultAsync(cancellationToken);

        if (active != null)
            return new QueueAdvanceResponseDto
            {
                ShellSize = active.ShellSize ?? string.Empty,
                HeatNumber = active.HeatNumber,
                CoilNumber = active.CoilNumber,
                Quantity = active.Quantity,
                ProductDescription = active.ProductDescription
            };

        var nextQueued = await _db.MaterialQueueItems
            .Where(m => m.WorkCenterId == wcId && m.Status == "queued")
            .OrderBy(m => m.Position)
            .FirstOrDefaultAsync(cancellationToken);

        if (nextQueued == null)
            return null;

        nextQueued.Status = "active";
        await _db.SaveChangesAsync(cancellationToken);

        return new QueueAdvanceResponseDto
        {
            ShellSize = nextQueued.ShellSize ?? string.Empty,
            HeatNumber = nextQueued.HeatNumber,
            CoilNumber = nextQueued.CoilNumber,
            Quantity = nextQueued.Quantity,
            ProductDescription = nextQueued.ProductDescription
        };
    }

    public Task ReportFaultAsync(Guid wcId, string description, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[Fault] WorkCenter={wcId}, Description={description}");
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

    public async Task<MaterialQueueItemDto> AddMaterialQueueItemAsync(Guid wcId, CreateMaterialQueueItemDto dto, CancellationToken cancellationToken = default)
    {
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
            ProductDescription = product?.ProductNumber ?? "Unknown",
            ShellSize = product?.TankSize.ToString(),
            HeatNumber = dto.HeatNumber,
            CoilNumber = dto.CoilNumber,
            Quantity = dto.Quantity,
            ProductId = dto.ProductId,
            VendorMillId = dto.VendorMillId,
            VendorProcessorId = dto.VendorProcessorId,
            LotNumber = dto.LotNumber,
            QueueType = "rolls",
            CreatedAt = DateTime.UtcNow
        };

        _db.MaterialQueueItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return MapQueueItem(item);
    }

    public async Task<MaterialQueueItemDto?> UpdateMaterialQueueItemAsync(Guid wcId, Guid itemId, UpdateMaterialQueueItemDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _db.MaterialQueueItems
            .FirstOrDefaultAsync(m => m.Id == itemId && m.WorkCenterId == wcId && m.Status == "queued", cancellationToken);
        if (item == null) return null;

        if (dto.HeatNumber != null) item.HeatNumber = dto.HeatNumber;
        if (dto.CoilNumber != null) item.CoilNumber = dto.CoilNumber;
        if (dto.Quantity.HasValue) item.Quantity = dto.Quantity.Value;
        if (dto.LotNumber != null) item.LotNumber = dto.LotNumber;
        if (dto.ProductId.HasValue)
        {
            item.ProductId = dto.ProductId.Value;
            var product = await _db.Products.FindAsync(new object[] { dto.ProductId.Value }, cancellationToken);
            if (product != null) item.ProductDescription = product.ProductNumber;
        }
        if (dto.VendorMillId.HasValue) item.VendorMillId = dto.VendorMillId;
        if (dto.VendorProcessorId.HasValue) item.VendorProcessorId = dto.VendorProcessorId;

        await _db.SaveChangesAsync(cancellationToken);
        return MapQueueItem(item);
    }

    public async Task<bool> DeleteMaterialQueueItemAsync(Guid wcId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.MaterialQueueItems
            .FirstOrDefaultAsync(m => m.Id == itemId && m.WorkCenterId == wcId && m.Status == "queued", cancellationToken);
        if (item == null) return false;
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

        var item = new MaterialQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            Position = maxPos + 1,
            Status = "queued",
            ProductDescription = product?.ProductNumber ?? "Unknown",
            HeatNumber = dto.HeatNumber ?? string.Empty,
            CoilNumber = string.Empty,
            Quantity = 1,
            CardId = dto.CardCode,
            CardColor = card?.Color,
            ProductId = dto.ProductId,
            VendorHeadId = dto.VendorHeadId,
            LotNumber = dto.LotNumber,
            CoilSlabNumber = dto.CoilSlabNumber,
            QueueType = "fitup",
            CreatedAt = DateTime.UtcNow
        };

        _db.MaterialQueueItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return MapQueueItem(item);
    }

    public async Task<MaterialQueueItemDto?> UpdateFitupQueueItemAsync(Guid wcId, Guid itemId, UpdateFitupQueueItemDto dto, CancellationToken cancellationToken = default)
    {
        var item = await _db.MaterialQueueItems
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

        if (dto.ProductId.HasValue)
        {
            item.ProductId = dto.ProductId.Value;
            var product = await _db.Products.FindAsync(new object[] { dto.ProductId.Value }, cancellationToken);
            if (product != null) item.ProductDescription = product.ProductNumber;
        }
        if (dto.VendorHeadId.HasValue) item.VendorHeadId = dto.VendorHeadId;
        if (dto.LotNumber != null) item.LotNumber = dto.LotNumber;
        if (dto.HeatNumber != null) item.HeatNumber = dto.HeatNumber;
        if (dto.CoilSlabNumber != null) item.CoilSlabNumber = dto.CoilSlabNumber;

        await _db.SaveChangesAsync(cancellationToken);
        return MapQueueItem(item);
    }

    public async Task<bool> DeleteFitupQueueItemAsync(Guid wcId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.MaterialQueueItems
            .FirstOrDefaultAsync(m => m.Id == itemId && m.WorkCenterId == wcId && m.Status == "queued", cancellationToken);
        if (item == null) return false;
        _db.MaterialQueueItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<IReadOnlyList<BarcodeCardDto>> GetBarcodeCardsAsync(string? siteCode, CancellationToken cancellationToken = default)
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
        ProductDescription = m.ProductDescription,
        ShellSize = m.ShellSize,
        HeatNumber = m.HeatNumber,
        CoilNumber = m.CoilNumber,
        Quantity = m.Quantity,
        CardId = m.CardId,
        CardColor = m.CardColor,
        CreatedAt = m.CreatedAt
    };

    private async Task<TimeZoneInfo> GetPlantTimeZoneForWorkCenterAsync(Guid wcId, CancellationToken cancellationToken)
    {
        var tzId = await _db.WorkCenters
            .Where(w => w.Id == wcId)
            .Select(w => w.Plant.TimeZoneId)
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
            .FirstOrDefaultAsync(m => m.CardId == cardId, cancellationToken);
        if (queueItem != null)
            return new KanbanCardLookupDto
            {
                HeatNumber = queueItem.HeatNumber,
                CoilNumber = queueItem.CoilNumber,
                ProductDescription = queueItem.ProductDescription,
                CardColor = queueItem.CardColor
            };

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
}
