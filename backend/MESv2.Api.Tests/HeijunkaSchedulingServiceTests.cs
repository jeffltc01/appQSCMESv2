using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class HeijunkaSchedulingServiceTests
{
    [Fact]
    public async Task Ingest_WhenSkuUnmapped_CreatesOpenException()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new HeijunkaSchedulingService(db);
        var actor = TestHelpers.TestUserId;

        var result = await svc.IngestErpDemandAsync(new IngestErpDemandRequestDto
        {
            Rows =
            [
                new ErpDemandRawIngestDto
                {
                    ErpSalesOrderId = "SO-100",
                    ErpSalesOrderLineId = "10",
                    ErpSkuCode = "SKU-UNMAPPED",
                    SiteCode = "000",
                    ErpLoadNumberRaw = "LOAD-100-0",
                    DispatchDateLocal = new DateTime(2026, 3, 9),
                    RequiredQty = 4,
                    OrderStatus = "Open",
                    SourceExtractedAtUtc = DateTime.UtcNow
                }
            ]
        }, actor);

        Assert.Equal(1, result.RawRowsInserted);
        Assert.Equal(1, result.UnmappedExceptionsCreated);
        Assert.Equal(1, db.UnmappedDemandExceptions.Count(x => x.ErpSkuCode == "SKU-UNMAPPED" && x.ExceptionStatus == "Open"));
    }

    [Fact]
    public async Task Ingest_WhenMappingPlanningGroupIsBlank_TreatsAsUnmappedAndCreatesException()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new HeijunkaSchedulingService(db);
        var actor = TestHelpers.TestUserId;

        await svc.UpsertSkuMappingAsync(new UpsertErpSkuMappingRequestDto
        {
            ErpSkuCode = "SKU-BLANK-PG",
            MesPlanningGroupId = "   ",
            SiteCode = "000",
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        }, actor);

        var result = await svc.IngestErpDemandAsync(new IngestErpDemandRequestDto
        {
            Rows =
            [
                new ErpDemandRawIngestDto
                {
                    ErpSalesOrderId = "SO-BLANK-PG",
                    ErpSalesOrderLineId = "10",
                    ErpSkuCode = "SKU-BLANK-PG",
                    SiteCode = "000",
                    ErpLoadNumberRaw = "LOAD-BLANK-PG-0",
                    DispatchDateLocal = new DateTime(2026, 3, 9),
                    RequiredQty = 5,
                    OrderStatus = "Open",
                    SourceExtractedAtUtc = DateTime.UtcNow
                }
            ]
        }, actor);

        var snapshot = Assert.Single(db.ErpDemandSnapshots.Where(x => x.ErpSkuCode == "SKU-BLANK-PG"));
        Assert.Null(snapshot.MesPlanningGroupId);
        Assert.Equal(1, result.UnmappedExceptionsCreated);
        Assert.Equal(1, db.UnmappedDemandExceptions.Count(x => x.ErpSkuCode == "SKU-BLANK-PG" && x.ExceptionStatus == "Open"));
    }

    [Fact]
    public async Task Publish_BlocksUntilUnmappedExceptionsResolved()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new HeijunkaSchedulingService(db);
        var actor = TestHelpers.TestUserId;
        var weekStart = new DateTime(2026, 3, 9);

        await svc.IngestErpDemandAsync(new IngestErpDemandRequestDto
        {
            Rows =
            [
                new ErpDemandRawIngestDto
                {
                    ErpSalesOrderId = "SO-200",
                    ErpSalesOrderLineId = "10",
                    ErpSkuCode = "SKU-UNMAPPED-2",
                    SiteCode = "000",
                    ErpLoadNumberRaw = "LOAD-200-0",
                    DispatchDateLocal = weekStart,
                    RequiredQty = 2,
                    OrderStatus = "Open",
                    SourceExtractedAtUtc = DateTime.UtcNow
                }
            ]
        }, actor);

        var schedule = await svc.GenerateDraftAsync(new GenerateScheduleDraftRequestDto
        {
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WeekStartDateLocal = weekStart
        }, actor);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.PublishScheduleAsync(schedule.Id, actor));

        var exception = db.UnmappedDemandExceptions.First();
        await svc.ResolveOrDeferExceptionAsync(new ResolveUnmappedDemandExceptionRequestDto
        {
            ExceptionId = exception.Id,
            Action = "Resolve",
            ResolutionNotes = "Mapped for phase 1 publish gate."
        }, actor);

        var published = await svc.PublishScheduleAsync(schedule.Id, actor);
        Assert.NotNull(published);
        Assert.Equal("Published", published!.Status);
    }

    [Fact]
    public async Task Publish_OnlyOneRevisionRemainsPublishedPerWeekAndLine()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new HeijunkaSchedulingService(db);
        var actor = TestHelpers.TestUserId;
        var weekStart = new DateTime(2026, 3, 9);

        await svc.UpsertSkuMappingAsync(new UpsertErpSkuMappingRequestDto
        {
            ErpSkuCode = "SKU-ONE-PUBLISH",
            MesPlanningGroupId = "PG-ONE-PUBLISH",
            SiteCode = "000",
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        }, actor);

        await svc.IngestErpDemandAsync(new IngestErpDemandRequestDto
        {
            Rows =
            [
                new ErpDemandRawIngestDto
                {
                    ErpSalesOrderId = "SO-ONE-PUBLISH",
                    ErpSalesOrderLineId = "1",
                    ErpSkuCode = "SKU-ONE-PUBLISH",
                    SiteCode = "000",
                    ErpLoadNumberRaw = "LOAD-ONE-PUBLISH-0",
                    DispatchDateLocal = weekStart,
                    RequiredQty = 3,
                    OrderStatus = "Open",
                    SourceExtractedAtUtc = DateTime.UtcNow
                }
            ]
        }, actor);

        var firstDraft = await svc.GenerateDraftAsync(new GenerateScheduleDraftRequestDto
        {
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WeekStartDateLocal = weekStart
        }, actor);

        var firstPublished = await svc.PublishScheduleAsync(firstDraft.Id, actor);
        Assert.NotNull(firstPublished);
        Assert.Equal("Published", firstPublished!.Status);

        var secondDraft = await svc.GenerateDraftAsync(new GenerateScheduleDraftRequestDto
        {
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WeekStartDateLocal = weekStart
        }, actor);

        var secondPublished = await svc.PublishScheduleAsync(secondDraft.Id, actor);
        Assert.NotNull(secondPublished);
        Assert.Equal("Published", secondPublished!.Status);

        var allWeekSchedules = db.Schedules
            .Where(x => x.SiteCode == "000" &&
                        x.ProductionLineId == TestHelpers.ProductionLine1Plt1Id &&
                        x.WeekStartDateLocal == weekStart)
            .ToList();

        Assert.Single(allWeekSchedules, x => x.Status == "Published");
        Assert.Contains(allWeekSchedules, x => x.Id == firstDraft.Id && x.Status == "Closed");
        Assert.Contains(allWeekSchedules, x => x.Id == secondDraft.Id && x.Status == "Published");
    }

    [Fact]
    public async Task FinalScan_IsIdempotentByKey()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new HeijunkaSchedulingService(db);
        var actor = TestHelpers.TestUserId;

        var first = await svc.RecordFinalScanExecutionAsync(new FinalScanExecutionRequestDto
        {
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            ExecutionDateLocal = new DateTime(2026, 3, 10),
            ActualQty = 1,
            ExecutionState = "Completed",
            IdempotencyKey = "fs-001"
        }, actor);

        var second = await svc.RecordFinalScanExecutionAsync(new FinalScanExecutionRequestDto
        {
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            ExecutionDateLocal = new DateTime(2026, 3, 10),
            ActualQty = 1,
            ExecutionState = "Completed",
            IdempotencyKey = "fs-001"
        }, actor);

        Assert.Equal(first.Id, second.Id);
        Assert.Equal(1, db.ScheduleExecutionEvents.Count(x => x.IdempotencyKey == "fs-001"));
    }

    [Fact]
    public async Task EndToEndFlow_MappingToKpi_CompletesCorePipeline()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new HeijunkaSchedulingService(db);
        var actor = TestHelpers.TestUserId;
        var weekStart = new DateTime(2026, 3, 9);

        await svc.UpsertSkuMappingAsync(new UpsertErpSkuMappingRequestDto
        {
            ErpSkuCode = "SKU-500",
            MesPlanningGroupId = "PG-500",
            SiteCode = "000",
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            RequiresReview = false
        }, actor);

        await svc.IngestErpDemandAsync(new IngestErpDemandRequestDto
        {
            Rows =
            [
                new ErpDemandRawIngestDto
                {
                    ErpSalesOrderId = "SO-500",
                    ErpSalesOrderLineId = "1",
                    ErpSkuCode = "SKU-500",
                    SiteCode = "000",
                    ErpLoadNumberRaw = "LOAD-500-0",
                    DispatchDateLocal = weekStart,
                    RequiredQty = 3,
                    OrderStatus = "Open",
                    SourceExtractedAtUtc = DateTime.UtcNow
                }
            ]
        }, actor);

        var draft = await svc.GenerateDraftAsync(new GenerateScheduleDraftRequestDto
        {
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WeekStartDateLocal = weekStart,
            FreezeHours = 24,
            PlanningResourceId = "paint-final-scan"
        }, actor);
        var published = await svc.PublishScheduleAsync(draft.Id, actor);
        Assert.NotNull(published);
        var line = published!.Lines.First();

        await svc.RecordFinalScanExecutionAsync(new FinalScanExecutionRequestDto
        {
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            MesPlanningGroupId = line.MesPlanningGroupId,
            ScheduleLineId = line.Id,
            ExecutionDateLocal = line.PlannedDateLocal,
            ActualQty = 3,
            ExecutionState = "Completed",
            IdempotencyKey = "flow-500"
        }, actor);

        var kpis = await svc.GetPhase1KpisAsync("000", TestHelpers.ProductionLine1Plt1Id, weekStart, weekStart.AddDays(6));

        Assert.True(kpis.IsEligible);
        Assert.NotNull(kpis.PlanAttainmentPercent.Value);
        Assert.NotNull(kpis.ScheduleAdherencePercent.Value);
    }

    [Fact]
    public async Task Resequence_UpdatesOrderAndWritesChangeLogs()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new HeijunkaSchedulingService(db);
        var actor = TestHelpers.TestUserId;
        var weekStart = new DateTime(2026, 3, 9);

        await svc.UpsertSkuMappingAsync(new UpsertErpSkuMappingRequestDto
        {
            ErpSkuCode = "SKU-A",
            MesPlanningGroupId = "PG-A",
            SiteCode = "000",
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        }, actor);
        await svc.UpsertSkuMappingAsync(new UpsertErpSkuMappingRequestDto
        {
            ErpSkuCode = "SKU-B",
            MesPlanningGroupId = "PG-B",
            SiteCode = "000",
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        }, actor);

        await svc.IngestErpDemandAsync(new IngestErpDemandRequestDto
        {
            Rows =
            [
                new ErpDemandRawIngestDto
                {
                    ErpSalesOrderId = "SO-A",
                    ErpSalesOrderLineId = "1",
                    ErpSkuCode = "SKU-A",
                    SiteCode = "000",
                    ErpLoadNumberRaw = "LOAD-A-0",
                    DispatchDateLocal = weekStart,
                    RequiredQty = 1,
                    OrderStatus = "Open",
                    SourceExtractedAtUtc = DateTime.UtcNow
                },
                new ErpDemandRawIngestDto
                {
                    ErpSalesOrderId = "SO-B",
                    ErpSalesOrderLineId = "1",
                    ErpSkuCode = "SKU-B",
                    SiteCode = "000",
                    ErpLoadNumberRaw = "LOAD-B-0",
                    DispatchDateLocal = weekStart,
                    RequiredQty = 1,
                    OrderStatus = "Open",
                    SourceExtractedAtUtc = DateTime.UtcNow.AddSeconds(1)
                }
            ]
        }, actor);

        var draft = await svc.GenerateDraftAsync(new GenerateScheduleDraftRequestDto
        {
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WeekStartDateLocal = weekStart
        }, actor);
        var secondLine = draft.Lines.OrderBy(x => x.SequenceIndex).Last();
        var resequenced = await svc.ReorderScheduleLineAsync(new ReorderScheduleLineRequestDto
        {
            ScheduleId = draft.Id,
            ScheduleLineId = secondLine.Id,
            NewSequenceIndex = 1,
            ChangeReasonCode = "DemandShock"
        }, actor);

        Assert.NotNull(resequenced);
        Assert.Equal(secondLine.Id, resequenced!.Lines.OrderBy(x => x.SequenceIndex).First().Id);
        Assert.True(db.ScheduleChangeLogs.Count(x => x.ScheduleId == draft.Id && x.FieldName == "SequenceIndex") >= 2);
    }

    [Fact]
    public async Task DispatchWeekOrderCoverage_UsesRequestedScheduleRevisionOnly()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new HeijunkaSchedulingService(db);
        var actor = TestHelpers.TestUserId;
        var weekStart = new DateTime(2026, 3, 9);

        await svc.UpsertSkuMappingAsync(new UpsertErpSkuMappingRequestDto
        {
            ErpSkuCode = "SKU-COV",
            MesPlanningGroupId = "PG-COV",
            SiteCode = "000",
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        }, actor);

        await svc.IngestErpDemandAsync(new IngestErpDemandRequestDto
        {
            Rows =
            [
                new ErpDemandRawIngestDto
                {
                    ErpSalesOrderId = "SO-COV-1",
                    ErpSalesOrderLineId = "1",
                    ErpSkuCode = "SKU-COV",
                    SiteCode = "000",
                    ErpLoadNumberRaw = "LOAD-COV-0",
                    DispatchDateLocal = weekStart,
                    RequiredQty = 4,
                    OrderStatus = "Open",
                    SourceExtractedAtUtc = DateTime.UtcNow
                },
                new ErpDemandRawIngestDto
                {
                    ErpSalesOrderId = "SO-COV-2",
                    ErpSalesOrderLineId = "1",
                    ErpSkuCode = "SKU-COV",
                    SiteCode = "000",
                    ErpLoadNumberRaw = "LOAD-COV-1",
                    DispatchDateLocal = weekStart,
                    RequiredQty = 2,
                    OrderStatus = "Open",
                    SourceExtractedAtUtc = DateTime.UtcNow.AddSeconds(1)
                }
            ]
        }, actor);

        var firstDraft = await svc.GenerateDraftAsync(new GenerateScheduleDraftRequestDto
        {
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WeekStartDateLocal = weekStart
        }, actor);

        await svc.GenerateDraftAsync(new GenerateScheduleDraftRequestDto
        {
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WeekStartDateLocal = weekStart
        }, actor);

        var coverage = await svc.GetDispatchWeekOrderCoverageAsync("000", TestHelpers.ProductionLine1Plt1Id, weekStart, firstDraft.Id);
        var first = coverage.First(x => x.ErpSalesOrderId == "SO-COV-1");
        Assert.Equal(6m, first.LoadGroupRequiredQty);
        Assert.Equal(6m, first.LoadGroupPlannedQty);
        Assert.True(first.LoadGroupCovered);
    }

    [Fact]
    public async Task SupermarketQuantityStatus_ReturnsLatestSnapshotAndNetAvailable()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new HeijunkaSchedulingService(db);
        var weekStart = new DateTime(2026, 3, 9);
        var productId = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");

        db.SupermarketPositionSnapshots.Add(new SupermarketPositionSnapshot
        {
            Id = Guid.NewGuid(),
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            ProductId = productId,
            OnHandQty = 10,
            InTransitQty = 2,
            DemandQty = 5,
            CapturedAtUtc = weekStart.AddDays(1)
        });
        db.SupermarketPositionSnapshots.Add(new SupermarketPositionSnapshot
        {
            Id = Guid.NewGuid(),
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            ProductId = productId,
            OnHandQty = 8,
            InTransitQty = 3,
            DemandQty = 4,
            CapturedAtUtc = weekStart.AddDays(2)
        });
        await db.SaveChangesAsync();

        var statuses = await svc.GetSupermarketQuantityStatusAsync("000", TestHelpers.ProductionLine1Plt1Id, weekStart);
        var row = Assert.Single(statuses);
        Assert.Equal(8m, row.OnHandQty);
        Assert.Equal(3m, row.InTransitQty);
        Assert.Equal(4m, row.DemandQty);
        Assert.Equal(7m, row.NetAvailableQty);
    }

    [Fact]
    public async Task MoveScheduleLine_UpdatesDateAndSequence()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new HeijunkaSchedulingService(db);
        var actor = TestHelpers.TestUserId;
        var weekStart = new DateTime(2026, 3, 9);

        await svc.UpsertSkuMappingAsync(new UpsertErpSkuMappingRequestDto
        {
            ErpSkuCode = "SKU-M1",
            MesPlanningGroupId = "PG-M1",
            SiteCode = "000",
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        }, actor);
        await svc.UpsertSkuMappingAsync(new UpsertErpSkuMappingRequestDto
        {
            ErpSkuCode = "SKU-M2",
            MesPlanningGroupId = "PG-M2",
            SiteCode = "000",
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true
        }, actor);

        await svc.IngestErpDemandAsync(new IngestErpDemandRequestDto
        {
            Rows =
            [
                new ErpDemandRawIngestDto
                {
                    ErpSalesOrderId = "SO-M1",
                    ErpSalesOrderLineId = "1",
                    ErpSkuCode = "SKU-M1",
                    SiteCode = "000",
                    ErpLoadNumberRaw = "LOAD-M1-0",
                    DispatchDateLocal = weekStart,
                    RequiredQty = 2,
                    OrderStatus = "Open",
                    SourceExtractedAtUtc = DateTime.UtcNow
                },
                new ErpDemandRawIngestDto
                {
                    ErpSalesOrderId = "SO-M2",
                    ErpSalesOrderLineId = "1",
                    ErpSkuCode = "SKU-M2",
                    SiteCode = "000",
                    ErpLoadNumberRaw = "LOAD-M2-0",
                    DispatchDateLocal = weekStart.AddDays(1),
                    RequiredQty = 3,
                    OrderStatus = "Open",
                    SourceExtractedAtUtc = DateTime.UtcNow.AddSeconds(1)
                }
            ]
        }, actor);

        var draft = await svc.GenerateDraftAsync(new GenerateScheduleDraftRequestDto
        {
            SiteCode = "000",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WeekStartDateLocal = weekStart
        }, actor);

        var moving = draft.Lines.First(x => x.LoadGroupId == "LOAD-M2");
        var moved = await svc.MoveScheduleLineAsync(new MoveScheduleLineRequestDto
        {
            ScheduleId = draft.Id,
            ScheduleLineId = moving.Id,
            NewPlannedDateLocal = weekStart,
            NewSequenceIndex = 1,
            ChangeReasonCode = "CalendarDragDrop"
        }, actor);

        Assert.NotNull(moved);
        var first = moved!.Lines.OrderBy(x => x.SequenceIndex).First();
        Assert.Equal(moving.Id, first.Id);
        Assert.Equal(weekStart, first.PlannedDateLocal.Date);
    }
}
