using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class XrayQueueService : IXrayQueueService
{
    private readonly MesDbContext _db;

    public XrayQueueService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<XrayQueueItemDto>> GetQueueAsync(Guid wcId, CancellationToken cancellationToken = default)
    {
        var items = await _db.XrayQueueItems
            .Where(x => x.WorkCenterId == wcId)
            .OrderBy(x => x.CreatedAt)
            .ToListAsync(cancellationToken);

        return items.Select(x => new XrayQueueItemDto
        {
            Id = x.Id,
            SerialNumber = x.SerialNumber,
            CreatedAt = x.CreatedAt
        }).ToList();
    }

    public async Task<XrayQueueItemDto> AddAsync(Guid wcId, AddXrayQueueItemDto dto, CancellationToken cancellationToken = default)
    {
        var serialExists = await _db.SerialNumbers
            .AnyAsync(s => s.Serial == dto.SerialNumber, cancellationToken);
        if (!serialExists)
            throw new InvalidOperationException("Serial number not found");

        var duplicate = await _db.XrayQueueItems
            .AnyAsync(x => x.WorkCenterId == wcId && x.SerialNumber == dto.SerialNumber, cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("Serial is already in queue");

        var item = new XrayQueueItem
        {
            Id = Guid.NewGuid(),
            WorkCenterId = wcId,
            SerialNumber = dto.SerialNumber,
            OperatorId = dto.OperatorId,
            CreatedAt = DateTime.UtcNow
        };

        _db.XrayQueueItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        return new XrayQueueItemDto
        {
            Id = item.Id,
            SerialNumber = item.SerialNumber,
            CreatedAt = item.CreatedAt
        };
    }

    public async Task<bool> RemoveAsync(Guid wcId, Guid itemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.XrayQueueItems
            .FirstOrDefaultAsync(x => x.Id == itemId && x.WorkCenterId == wcId, cancellationToken);
        if (item == null) return false;

        _db.XrayQueueItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
        return true;
    }
}
