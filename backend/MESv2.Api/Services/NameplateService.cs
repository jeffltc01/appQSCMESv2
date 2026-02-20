using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class NameplateService : INameplateService
{
    private readonly MesDbContext _db;

    public NameplateService(MesDbContext db)
    {
        _db = db;
    }

    public async Task<NameplateRecordResponseDto> CreateAsync(CreateNameplateRecordDto dto, CancellationToken cancellationToken = default)
    {
        var duplicate = await _db.NameplateRecords
            .AnyAsync(n => n.SerialNumber == dto.SerialNumber, cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("This serial number already exists");

        var record = new NameplateRecord
        {
            Id = Guid.NewGuid(),
            SerialNumber = dto.SerialNumber,
            ProductId = dto.ProductId,
            WorkCenterId = dto.WorkCenterId,
            OperatorId = dto.OperatorId,
            Timestamp = DateTime.UtcNow
        };

        _db.NameplateRecords.Add(record);
        await _db.SaveChangesAsync(cancellationToken);

        return new NameplateRecordResponseDto
        {
            Id = record.Id,
            SerialNumber = record.SerialNumber,
            ProductId = record.ProductId,
            Timestamp = record.Timestamp
        };
    }

    public async Task<NameplateRecordResponseDto?> GetBySerialAsync(string serialNumber, CancellationToken cancellationToken = default)
    {
        var record = await _db.NameplateRecords
            .FirstOrDefaultAsync(n => n.SerialNumber == serialNumber, cancellationToken);
        if (record == null) return null;

        return new NameplateRecordResponseDto
        {
            Id = record.Id,
            SerialNumber = record.SerialNumber,
            ProductId = record.ProductId,
            Timestamp = record.Timestamp
        };
    }

    public Task ReprintAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Console.WriteLine($"[NiceLabel] Reprint request for NameplateRecord {id}");
        return Task.CompletedTask;
    }
}
