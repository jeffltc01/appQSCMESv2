using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class HoldTagNcrServiceTests
{
    [Fact]
    public async Task HoldTag_RepairResolve_RequiresLinkedNcr()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var wf = new WorkflowEngineService(db);
        var holdTags = new HoldTagService(db, wf);
        var actorId = Guid.Parse("77777777-7777-7777-7777-777777777702");
        var serialId = Guid.NewGuid();
        db.SerialNumbers.Add(new MESv2.Api.Models.SerialNumber
        {
            Id = serialId,
            Serial = "UT-HOLD-001",
            PlantId = TestHelpers.PlantPlt1Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var created = await holdTags.CreateHoldTagAsync(new CreateHoldTagRequestDto
        {
            SiteCode = "PLT1",
            ProductionLineId = TestHelpers.ProductionLine1Plt1Id,
            WorkCenterId = TestHelpers.wcRollsId,
            SerialNumberMasterId = serialId,
            ProblemDescription = "Test hold tag",
            ActorUserId = actorId
        });

        await holdTags.SetDispositionAsync(new SetHoldTagDispositionRequestDto
        {
            HoldTagId = created.Id,
            Disposition = "Repair",
            RepairInstructionTemplateId = Guid.NewGuid(),
            ActorUserId = actorId
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => holdTags.ResolveAsync(new ResolveHoldTagRequestDto
        {
            HoldTagId = created.Id,
            ActorUserId = actorId
        }));
    }

    [Fact]
    public async Task Ncr_NumberingIncrements_AndApprovalNeedsAttachment()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var wf = new WorkflowEngineService(db);
        var ncrService = new NcrService(db, wf);

        var actorId = Guid.Parse("77777777-7777-7777-7777-777777777702");
        var ncrType = (await ncrService.GetNcrTypesAsync(includeInactive: false)).First();

        var ncr1 = await ncrService.CreateNcrAsync(new CreateNcrRequestDto
        {
            SourceType = "DirectQuality",
            SiteCode = "PLT1",
            DetectedByUserId = actorId,
            SubmitterUserId = actorId,
            CoordinatorUserId = actorId,
            NcrTypeId = ncrType.Id,
            DateUtc = DateTime.UtcNow,
            ProblemDescription = "Problem A",
            CreatedByUserId = actorId
        });
        var ncr2 = await ncrService.CreateNcrAsync(new CreateNcrRequestDto
        {
            SourceType = "DirectQuality",
            SiteCode = "PLT1",
            DetectedByUserId = actorId,
            SubmitterUserId = actorId,
            CoordinatorUserId = actorId,
            NcrTypeId = ncrType.Id,
            DateUtc = DateTime.UtcNow,
            ProblemDescription = "Problem B",
            CreatedByUserId = actorId
        });

        Assert.True(ncr2.NcrNumber > ncr1.NcrNumber);

        await ncrService.SubmitNcrStepAsync(new SubmitNcrStepRequestDto
        {
            NcrId = ncr1.Id,
            ActionCode = "SubmitNcrStep",
            ActorUserId = actorId
        });

        await Assert.ThrowsAsync<InvalidOperationException>(() => ncrService.ApproveNcrStepAsync(new NcrDecisionRequestDto
        {
            NcrId = ncr1.Id,
            StepCode = "ApprovalGate",
            ActorUserId = actorId,
            Comments = "approve"
        }));
    }
}
