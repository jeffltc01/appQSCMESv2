using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class NameplateServiceTests
{
    private static readonly Guid TestProductId = Guid.NewGuid();

    [Fact]
    public async Task Create_ValidRecord_Succeeds()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedProduct(db);

        var sut = new NameplateService(db);
        var result = await sut.CreateAsync(new CreateNameplateRecordDto
        {
            SerialNumber = "W00100001",
            ProductId = TestProductId,
            WorkCenterId = TestHelpers.wcRollsId,
            OperatorId = TestHelpers.TestUserId
        });

        Assert.NotNull(result);
        Assert.Equal("W00100001", result.SerialNumber);
    }

    [Fact]
    public async Task Create_DuplicateSerial_Throws()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedProduct(db);
        db.NameplateRecords.Add(new NameplateRecord
        {
            Id = Guid.NewGuid(),
            SerialNumber = "W00100002",
            ProductId = TestProductId,
            WorkCenterId = TestHelpers.wcRollsId,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new NameplateService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateAsync(new CreateNameplateRecordDto
            {
                SerialNumber = "W00100002",
                ProductId = TestProductId,
                WorkCenterId = TestHelpers.wcRollsId,
                OperatorId = TestHelpers.TestUserId
            }));
    }

    [Fact]
    public async Task GetBySerial_Found_ReturnsRecord()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        SeedProduct(db);
        db.NameplateRecords.Add(new NameplateRecord
        {
            Id = Guid.NewGuid(),
            SerialNumber = "W00100003",
            ProductId = TestProductId,
            WorkCenterId = TestHelpers.wcRollsId,
            OperatorId = TestHelpers.TestUserId,
            Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new NameplateService(db);
        var result = await sut.GetBySerialAsync("W00100003");

        Assert.NotNull(result);
        Assert.Equal("W00100003", result.SerialNumber);
    }

    [Fact]
    public async Task GetBySerial_NotFound_ReturnsNull()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new NameplateService(db);
        var result = await sut.GetBySerialAsync("NONEXISTENT");
        Assert.Null(result);
    }

    private static void SeedProduct(MESv2.Api.Data.MesDbContext db)
    {
        if (!db.Products.Any(p => p.Id == TestProductId))
        {
            var ptId = Guid.NewGuid();
            if (!db.ProductTypes.Any())
                db.ProductTypes.Add(new ProductType { Id = ptId, Name = "Sellable" });
            else
                ptId = db.ProductTypes.First().Id;

            db.Products.Add(new Product
            {
                Id = TestProductId,
                ProductNumber = "120-AG-STD",
                TankSize = 120,
                TankType = "AG",
                ProductTypeId = ptId
            });
            db.SaveChanges();
        }
    }
}
