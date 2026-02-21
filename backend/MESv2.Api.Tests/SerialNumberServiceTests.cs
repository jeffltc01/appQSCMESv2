using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class SerialNumberServiceTests
{
    [Fact]
    public async Task GetContext_UnknownSerial_ReturnsNull()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = new SerialNumberService(db);

        var result = await sut.GetContextAsync("UNKNOWN");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetContext_KnownSerial_ReturnsContext()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var ptId = Guid.NewGuid();
        var prodId = Guid.NewGuid();
        db.ProductTypes.Add(new ProductType { Id = ptId, Name = "Shell" });
        db.Products.Add(new Product { Id = prodId, ProductNumber = "SH-120", TankSize = 120, TankType = "AG", ProductTypeId = ptId });
        db.SerialNumbers.Add(new SerialNumber { Id = Guid.NewGuid(), Serial = "SH001", ProductId = prodId, CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var sut = new SerialNumberService(db);
        var result = await sut.GetContextAsync("SH001");

        Assert.NotNull(result);
        Assert.Equal("SH001", result.SerialNumber);
        Assert.Equal(120, result.TankSize);
    }

    [Fact]
    public async Task GetContext_WithAssembly_ReturnsAssemblyInfo()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var ptId = Guid.NewGuid();
        var prodId = Guid.NewGuid();
        var snId = Guid.NewGuid();
        db.ProductTypes.Add(new ProductType { Id = ptId, Name = "Shell" });
        db.Products.Add(new Product { Id = prodId, ProductNumber = "SH-500", TankSize = 500, TankType = "AG", ProductTypeId = ptId });
        db.SerialNumbers.Add(new SerialNumber { Id = snId, Serial = "SH100", ProductId = prodId, CreatedAt = DateTime.UtcNow });
        db.Assemblies.Add(new Assembly
        {
            Id = Guid.NewGuid(), AlphaCode = "AA", TankSize = 500,
            WorkCenterId = TestHelpers.wcRollsId, AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id, OperatorId = TestHelpers.TestUserId, Timestamp = DateTime.UtcNow
        });
        db.TraceabilityLogs.Add(new TraceabilityLog
        {
            Id = Guid.NewGuid(), FromSerialNumberId = snId, ToAlphaCode = "AA",
            Relationship = "shell", Timestamp = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var sut = new SerialNumberService(db);
        var result = await sut.GetContextAsync("SH100");

        Assert.NotNull(result);
        Assert.NotNull(result.ExistingAssembly);
        Assert.Equal("AA", result.ExistingAssembly!.AlphaCode);
    }
}
