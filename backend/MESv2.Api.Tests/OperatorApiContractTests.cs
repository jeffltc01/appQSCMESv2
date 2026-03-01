using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using MESv2.Api.Controllers;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class OperatorApiContractTests
{
    private static readonly JsonSerializerOptions CamelCaseJson = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    [Fact]
    public async Task GetLoginConfig_ResponseContract_ContainsRequiredFields()
    {
        var authService = new Mock<IAuthService>();
        authService
            .Setup(x => x.GetLoginConfigAsync("EMP001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((new LoginConfigDto
            {
                RequiresPin = false,
                DefaultSiteId = TestHelpers.PlantPlt1Id,
                AllowSiteSelection = true,
                IsWelder = false,
                UserName = "Jeff Thompson"
            }, false));

        var userService = new Mock<IUserService>();
        var controller = new UsersController(authService.Object, userService.Object);

        var response = await controller.GetLoginConfig("EMP001", CancellationToken.None);
        var ok = Assert.IsType<OkObjectResult>(response.Result);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value, CamelCaseJson));
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("requiresPin", out _));
        Assert.True(root.TryGetProperty("defaultSiteId", out _));
        Assert.True(root.TryGetProperty("allowSiteSelection", out _));
        Assert.True(root.TryGetProperty("isWelder", out _));
        Assert.True(root.TryGetProperty("userName", out _));
    }

    [Fact]
    public async Task GetLoginConfig_InactiveConflictContract_ContainsMessageField()
    {
        var authService = new Mock<IAuthService>();
        authService
            .Setup(x => x.GetLoginConfigAsync("EMP001", It.IsAny<CancellationToken>()))
            .ReturnsAsync((null as LoginConfigDto, true));

        var userService = new Mock<IUserService>();
        var controller = new UsersController(authService.Object, userService.Object);

        var response = await controller.GetLoginConfig("EMP001", CancellationToken.None);
        var conflict = Assert.IsType<ConflictObjectResult>(response.Result);

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(conflict.Value, CamelCaseJson));
        Assert.True(json.RootElement.TryGetProperty("message", out _));
    }

    [Fact]
    public async Task GetWorkCenterHistory_ResponseContract_ContainsRequiredFields()
    {
        var workCenterService = new Mock<IWorkCenterService>();
        workCenterService
            .Setup(x => x.GetHistoryAsync(
                TestHelpers.wcRollsId,
                TestHelpers.PlantPlt1Id,
                TestHelpers.ProductionLine1Plt1Id,
                null,
                5,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new WCHistoryDto
            {
                DayCount = 1,
                HourlyCounts = new List<HourlyCountDto>
                {
                    new() { Hour = 7, Count = 1 }
                },
                RecentRecords = new List<WCHistoryEntryDto>
                {
                    new()
                    {
                        Id = Guid.NewGuid(),
                        ProductionRecordId = Guid.NewGuid(),
                        Timestamp = DateTime.UtcNow,
                        SerialOrIdentifier = "20301",
                        TankSize = 120,
                        HasAnnotation = false,
                        AnnotationColor = null
                    }
                }
            });

        var controller = new WorkCentersController(
            workCenterService.Object,
            Mock.Of<IXrayQueueService>(),
            Mock.Of<IDowntimeService>(),
            Mock.Of<IAdminWorkCenterService>(),
            Mock.Of<ILogger<WorkCentersController>>());

        var response = await controller.GetHistory(
            TestHelpers.wcRollsId,
            TestHelpers.PlantPlt1Id,
            TestHelpers.ProductionLine1Plt1Id,
            date: null,
            limit: 5,
            assetId: null,
            cancellationToken: CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value, CamelCaseJson));
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("dayCount", out _));
        Assert.True(root.TryGetProperty("hourlyCounts", out var hourlyCounts));
        Assert.True(root.TryGetProperty("recentRecords", out var recentRecords));

        Assert.Equal(JsonValueKind.Array, hourlyCounts.ValueKind);
        Assert.Equal(JsonValueKind.Array, recentRecords.ValueKind);
        Assert.Equal(1, recentRecords.GetArrayLength());

        var firstRecord = recentRecords[0];
        Assert.True(firstRecord.TryGetProperty("id", out _));
        Assert.True(firstRecord.TryGetProperty("productionRecordId", out _));
        Assert.True(firstRecord.TryGetProperty("timestamp", out _));
        Assert.True(firstRecord.TryGetProperty("serialOrIdentifier", out _));
        Assert.True(firstRecord.TryGetProperty("tankSize", out _));
        Assert.True(firstRecord.TryGetProperty("hasAnnotation", out _));
        Assert.True(firstRecord.TryGetProperty("annotationColor", out _));
    }

    [Fact]
    public async Task GetWorkCenterHistory_ReturnsBadRequest_WhenProductionLineIdMissing()
    {
        var workCenterService = new Mock<IWorkCenterService>();
        var controller = new WorkCentersController(
            workCenterService.Object,
            Mock.Of<IXrayQueueService>(),
            Mock.Of<IDowntimeService>(),
            Mock.Of<IAdminWorkCenterService>(),
            Mock.Of<ILogger<WorkCentersController>>());

        var response = await controller.GetHistory(
            TestHelpers.wcRollsId,
            TestHelpers.PlantPlt1Id,
            Guid.Empty,
            date: null,
            limit: 5,
            assetId: null,
            cancellationToken: CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(badRequest.Value, CamelCaseJson));
        Assert.True(json.RootElement.TryGetProperty("message", out _));
        workCenterService.Verify(x => x.GetHistoryAsync(
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<Guid>(),
            It.IsAny<string?>(),
            It.IsAny<int>(),
            It.IsAny<Guid?>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public void CreateRoundSeamRecord_RequestContract_ContainsRequiredFields()
    {
        var request = new CreateRoundSeamRecordDto
        {
            SerialNumber = "20301",
            WorkCenterId = TestHelpers.wcRoundSeamId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId
        };

        using var json = JsonDocument.Parse(JsonSerializer.Serialize(request, CamelCaseJson));
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("serialNumber", out _));
        Assert.True(root.TryGetProperty("workCenterId", out _));
        Assert.True(root.TryGetProperty("assetId", out _));
        Assert.True(root.TryGetProperty("productionLineId", out _));
        Assert.True(root.TryGetProperty("operatorId", out _));
    }

    [Fact]
    public async Task CreateRoundSeamRecord_ResponseContract_ContainsRequiredFields()
    {
        var roundSeamService = new Mock<IRoundSeamService>();
        roundSeamService
            .Setup(x => x.CreateRoundSeamRecordAsync(It.IsAny<CreateRoundSeamRecordDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new CreateProductionRecordResponseDto
            {
                Id = Guid.NewGuid(),
                SerialNumber = "20301",
                Timestamp = DateTime.UtcNow,
                Warning = null
            });

        var controller = new RoundSeamController(roundSeamService.Object);

        var response = await controller.CreateRecord(new CreateRoundSeamRecordDto
        {
            SerialNumber = "20301",
            WorkCenterId = TestHelpers.wcRoundSeamId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId
        }, CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(response.Result);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(ok.Value, CamelCaseJson));
        var root = json.RootElement;

        Assert.True(root.TryGetProperty("id", out _));
        Assert.True(root.TryGetProperty("serialNumber", out _));
        Assert.True(root.TryGetProperty("timestamp", out _));
        Assert.True(root.TryGetProperty("warning", out _));
    }

    [Fact]
    public async Task CreateRoundSeamRecord_ErrorContract_ContainsMessageField()
    {
        var roundSeamService = new Mock<IRoundSeamService>();
        roundSeamService
            .Setup(x => x.CreateRoundSeamRecordAsync(It.IsAny<CreateRoundSeamRecordDto>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Welder setup required."));

        var controller = new RoundSeamController(roundSeamService.Object);

        var response = await controller.CreateRecord(new CreateRoundSeamRecordDto
        {
            SerialNumber = "20301",
            WorkCenterId = TestHelpers.wcRoundSeamId,
            AssetId = TestHelpers.TestAssetId,
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            OperatorId = TestHelpers.TestUserId
        }, CancellationToken.None);

        var badRequest = Assert.IsType<BadRequestObjectResult>(response.Result);
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(badRequest.Value, CamelCaseJson));
        Assert.True(json.RootElement.TryGetProperty("message", out _));
    }
}
