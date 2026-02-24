using Moq;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class NaturalLanguageQueryToolExecutorTests
{
    private static NaturalLanguageQueryToolExecutor CreateSut(
        Data.MesDbContext db,
        Mock<ISupervisorDashboardService>? supervisorMock = null,
        Mock<IDigitalTwinService>? digitalTwinMock = null)
    {
        return new NaturalLanguageQueryToolExecutor(
            db,
            (supervisorMock ?? new Mock<ISupervisorDashboardService>()).Object,
            (digitalTwinMock ?? new Mock<IDigitalTwinService>()).Object);
    }

    [Fact]
    public async Task ExecuteAsync_CurrentScreenFilteredRecordCount_ReturnsProvidedCount()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateSut(db);
        var req = new NaturalLanguageQueryRequestDto
        {
            Question = "how many records match my filters?",
            Context = new NaturalLanguageQueryContextDto
            {
                ScreenKey = "audit-log",
                ActiveFilterTotalCount = 42,
                FilterSummary = "action=updated",
            }
        };

        var result = await sut.ExecuteAsync(
            NlqIntent.CurrentScreenFilteredRecordCount,
            req,
            TestHelpers.PlantPlt1Id,
            CancellationToken.None);

        Assert.Contains("42", result.AnswerText);
        Assert.Equal("context", result.ScopeUsed);
    }

    [Fact]
    public async Task ExecuteAsync_CurrentScreenFilteredRecordCount_WithoutContext_ReturnsGuidance()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateSut(db);

        var result = await sut.ExecuteAsync(
            NlqIntent.CurrentScreenFilteredRecordCount,
            new NaturalLanguageQueryRequestDto { Question = "how many match?" },
            TestHelpers.PlantPlt1Id,
            CancellationToken.None);

        Assert.Contains("need list/table filter context", result.AnswerText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExecuteAsync_BottleneckIntent_UsesDigitalTwinSnapshot()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var twinMock = new Mock<IDigitalTwinService>();
        twinMock.Setup(t => t.GetSnapshotAsync(
                TestHelpers.PlantPlt1Id,
                TestHelpers.ProductionLine1Plt1Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DigitalTwinSnapshotDto
            {
                Stations =
                [
                    new StationStatusDto { Name = "Rolls", WipCount = 2, Status = "Active", IsBottleneck = false },
                    new StationStatusDto { Name = "Fitup", WipCount = 8, Status = "Slow", IsBottleneck = true },
                ]
            });
        var sut = CreateSut(db, digitalTwinMock: twinMock);
        var req = new NaturalLanguageQueryRequestDto
        {
            Question = "what is the bottleneck",
            Context = new NaturalLanguageQueryContextDto
            {
                ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            }
        };

        var result = await sut.ExecuteAsync(
            NlqIntent.BottleneckWorkCenterNow,
            req,
            TestHelpers.PlantPlt1Id,
            CancellationToken.None);

        Assert.Contains("Fitup", result.AnswerText);
        Assert.Equal("context", result.ScopeUsed);
    }
}
