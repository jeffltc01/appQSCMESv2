using Microsoft.Extensions.Logging.Abstractions;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class NameplateServiceTests
{
    [Fact]
    public async Task Create_ValidRecord_Succeeds()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        var sut = new NameplateService(db, NullLogger<NameplateService>.Instance);
        var result = await sut.CreateAsync(new CreateNameplateRecordDto
        {
            SerialNumber = "W00100001",
            ProductId = product.Id,
            WorkCenterId = TestHelpers.wcNameplateId,
            OperatorId = TestHelpers.TestUserId
        });

        Assert.NotNull(result);
        Assert.Equal("W00100001", result.SerialNumber);

        var sn = db.SerialNumbers.FirstOrDefault(s => s.Serial == "W00100001");
        Assert.NotNull(sn);
        Assert.Equal(product.Id, sn.ProductId);
    }

    [Fact]
    public async Task Create_DuplicateSerial_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        db.SerialNumbers.Add(new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "W00100002",
            ProductId = product.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new NameplateService(db, NullLogger<NameplateService>.Instance);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateAsync(new CreateNameplateRecordDto
            {
                SerialNumber = "W00100002",
                ProductId = product.Id,
                WorkCenterId = TestHelpers.wcNameplateId,
                OperatorId = TestHelpers.TestUserId
            }));
    }

    [Fact]
    public async Task GetBySerial_Found_ReturnsRecord()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var product = db.Products.First(p => p.ProductType!.SystemTypeName == "sellable" && p.TankSize == 120);

        db.SerialNumbers.Add(new SerialNumber
        {
            Id = Guid.NewGuid(),
            Serial = "W00100003",
            ProductId = product.Id,
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new NameplateService(db, NullLogger<NameplateService>.Instance);
        var result = await sut.GetBySerialAsync("W00100003");

        Assert.NotNull(result);
        Assert.Equal("W00100003", result.SerialNumber);
    }

    [Fact]
    public async Task GetBySerial_NotFound_ReturnsNull()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new NameplateService(db, NullLogger<NameplateService>.Instance);
        var result = await sut.GetBySerialAsync("NONEXISTENT");
        Assert.Null(result);
    }
}
