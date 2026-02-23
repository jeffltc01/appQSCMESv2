using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class NaturalLanguageQueryServiceTests
{
    private static NaturalLanguageQueryService CreateService(
        Data.MesDbContext db,
        Mock<INlqModelClient> modelMock,
        Mock<INlqToolExecutor> toolMock,
        NaturalLanguageQueryOptions? options = null)
    {
        return new NaturalLanguageQueryService(
            Options.Create(options ?? new NaturalLanguageQueryOptions()),
            modelMock.Object,
            toolMock.Object,
            new MemoryCache(new MemoryCacheOptions()),
            db,
            new Mock<ILogger<NaturalLanguageQueryService>>().Object);
    }

    [Fact]
    public async Task AskAsync_RoleTooLow_ThrowsUnauthorized()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var modelMock = new Mock<INlqModelClient>();
        var toolMock = new Mock<INlqToolExecutor>();
        var sut = CreateService(db, modelMock, toolMock);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.AskAsync(
            TestHelpers.TestUserId,
            6.0m,
            TestHelpers.PlantPlt1Id,
            new NaturalLanguageQueryRequestDto { Question = "how many tanks today?" },
            CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_UsesCache_ForRepeatedQuestion()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var modelMock = new Mock<INlqModelClient>();
        modelMock.Setup(m => m.InterpretAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NlqModelInterpretation
            {
                Intent = NlqIntent.TanksProducedToday,
                Confidence = 0.9m,
            });

        var toolMock = new Mock<INlqToolExecutor>();
        toolMock.Setup(t => t.ExecuteAsync(
                NlqIntent.TanksProducedToday,
                It.IsAny<NaturalLanguageQueryRequestDto>(),
                TestHelpers.PlantPlt1Id,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NaturalLanguageQueryResponseDto
            {
                AnswerText = "mock answer",
                ScopeUsed = "plant-wide",
                Confidence = 0.8m,
            });

        var options = new NaturalLanguageQueryOptions { IntentCacheSeconds = 300 };
        var sut = CreateService(db, modelMock, toolMock, options);

        var request = new NaturalLanguageQueryRequestDto { Question = "how many tanks today?" };
        await sut.AskAsync(TestHelpers.TestUserId, 5.0m, TestHelpers.PlantPlt1Id, request, CancellationToken.None);
        await sut.AskAsync(TestHelpers.TestUserId, 5.0m, TestHelpers.PlantPlt1Id, request, CancellationToken.None);

        modelMock.Verify(m => m.InterpretAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AskAsync_CrossPlantRestricted_ForRoleAboveDirector()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var modelMock = new Mock<INlqModelClient>();
        var toolMock = new Mock<INlqToolExecutor>();
        var sut = CreateService(db, modelMock, toolMock);

        var request = new NaturalLanguageQueryRequestDto
        {
            Question = "how many tanks today?",
            Context = new NaturalLanguageQueryContextDto { PlantId = TestHelpers.PlantPlt2Id }
        };

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => sut.AskAsync(
            TestHelpers.TestUserId,
            5.0m,
            TestHelpers.PlantPlt1Id,
            request,
            CancellationToken.None));
    }

    [Fact]
    public async Task AskAsync_WritesAuditLog()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var modelMock = new Mock<INlqModelClient>();
        modelMock.Setup(m => m.InterpretAsync(It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NlqModelInterpretation
            {
                Intent = NlqIntent.TanksProducedToday,
                Confidence = 0.9m,
            });
        var toolMock = new Mock<INlqToolExecutor>();
        toolMock.Setup(t => t.ExecuteAsync(
                It.IsAny<NlqIntent>(),
                It.IsAny<NaturalLanguageQueryRequestDto>(),
                It.IsAny<Guid>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new NaturalLanguageQueryResponseDto
            {
                AnswerText = "ok",
                ScopeUsed = "plant-wide",
                Confidence = 0.8m,
            });
        var sut = CreateService(db, modelMock, toolMock);

        await sut.AskAsync(
            TestHelpers.TestUserId,
            5.0m,
            TestHelpers.PlantPlt1Id,
            new NaturalLanguageQueryRequestDto { Question = "how many tanks today?" },
            CancellationToken.None);

        Assert.Contains(db.AuditLogs, a => a.EntityName == "NaturalLanguageQuery" && a.Action == "NLQ_ASK");
    }
}
