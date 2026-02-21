using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class NameplateService : INameplateService
{
    private readonly MesDbContext _db;
    private readonly ILogger<NameplateService> _logger;

    public NameplateService(MesDbContext db, ILogger<NameplateService> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<NameplateRecordResponseDto> CreateAsync(CreateNameplateRecordDto dto, CancellationToken cancellationToken = default)
    {
        var duplicate = await _db.SerialNumbers
            .AnyAsync(s => s.Serial == dto.SerialNumber && s.Product!.ProductType!.SystemTypeName == "sellable", cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("This serial number already exists");

        var operator_ = await _db.Users
            .FirstOrDefaultAsync(u => u.Id == dto.OperatorId, cancellationToken);
        var plantId = operator_?.DefaultSiteId ?? Guid.Empty;

        var sn = new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = dto.SerialNumber,
            ProductId = dto.ProductId,
            PlantId = plantId,
            CreatedAt = DateTime.UtcNow,
            CreatedByUserId = dto.OperatorId
        };
        _db.SerialNumbers.Add(sn);

        var record = new ProductionRecord
        {
            Id = Guid.NewGuid(),
            SerialNumberId = sn.Id,
            WorkCenterId = dto.WorkCenterId,
            OperatorId = dto.OperatorId,
            ProductionLineId = Guid.Empty,
            Timestamp = DateTime.UtcNow
        };
        _db.ProductionRecords.Add(record);

        await _db.SaveChangesAsync(cancellationToken);

        return new NameplateRecordResponseDto
        {
            Id = sn.Id,
            SerialNumber = sn.Serial,
            ProductId = dto.ProductId,
            Timestamp = record.Timestamp
        };
    }

    public async Task<NameplateRecordResponseDto?> GetBySerialAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        var sn = await _db.SerialNumbers
            .Include(s => s.Product)
            .FirstOrDefaultAsync(s => s.Serial == serialNumber && s.Product!.ProductType!.SystemTypeName == "sellable", cancellationToken);
        if (sn == null) return null;

        return new NameplateRecordResponseDto
        {
            Id = sn.Id,
            SerialNumber = sn.Serial,
            ProductId = sn.ProductId ?? Guid.Empty,
            Timestamp = sn.CreatedAt
        };
    }

    public Task ReprintAsync(Guid id, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("NiceLabel reprint requested for SerialNumber {SerialNumberId}", id);
        return Task.CompletedTask;
    }
}
