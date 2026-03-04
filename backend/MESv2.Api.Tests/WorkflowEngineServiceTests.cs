using Microsoft.EntityFrameworkCore;
using MESv2.Api.DTOs;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class WorkflowEngineServiceTests
{
    [Fact]
    public async Task StartWorkflow_PinsActiveDefinitionVersion()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new WorkflowEngineService(db);

        var instance = await svc.StartWorkflowAsync(new StartWorkflowRequestDto
        {
            EntityType = "HoldTag",
            EntityId = Guid.NewGuid(),
            WorkflowType = "HoldTag"
        });

        Assert.Equal("HoldTag", instance.WorkflowType);
        Assert.Equal(2, instance.WorkflowDefinitionVersion);
        Assert.Equal("TagCreated", instance.CurrentStepCode);
    }

    [Fact]
    public async Task RejectStep_WithTarget_MovesToTargetAndRejectedStatus()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new WorkflowEngineService(db);

        var instance = await svc.StartWorkflowAsync(new StartWorkflowRequestDto
        {
            EntityType = "Ncr",
            EntityId = Guid.NewGuid(),
            WorkflowType = "Ncr"
        });

        instance = await svc.AdvanceStepAsync(new AdvanceStepRequestDto
        {
            WorkflowInstanceId = instance.Id,
            ActionCode = "Submit",
            ActorUserId = TestHelpers.TestUserId
        });

        var rejected = await svc.RejectStepAsync(new ApproveRejectRequestDto
        {
            WorkflowInstanceId = instance.Id,
            StepCode = instance.CurrentStepCode,
            ActorUserId = TestHelpers.TestUserId,
            Comments = "Need rework"
        });

        Assert.Equal("Rejected", rejected.Status);
        Assert.Equal("DraftIntake", rejected.CurrentStepCode);
    }

    [Fact]
    public async Task ApproveAndComplete_SetsTerminalStatus()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new WorkflowEngineService(db);

        var definition = await svc.UpsertDefinitionAsync(new UpsertWorkflowDefinitionDto
        {
            WorkflowType = "UnitFlow",
            IsActive = true,
            StartStepCode = "A",
            Steps =
            [
                new WorkflowStepDefinitionDto
                {
                    StepCode = "A",
                    StepName = "A",
                    Sequence = 1,
                    ApprovalMode = "AnyOne",
                    ApprovalAssignments = [$"user:{TestHelpers.TestUserId}"],
                    AllowReject = false
                }
            ]
        }, TestHelpers.TestUserId);

        var instance = await svc.StartWorkflowAsync(new StartWorkflowRequestDto
        {
            EntityType = "Unit",
            EntityId = Guid.NewGuid(),
            WorkflowType = definition.WorkflowType
        });

        var approved = await svc.ApproveStepAsync(new ApproveRejectRequestDto
        {
            WorkflowInstanceId = instance.Id,
            StepCode = "A",
            ActorUserId = TestHelpers.TestUserId,
            Comments = "ok"
        });

        Assert.Equal("Completed", approved.Status);
        Assert.NotNull(approved.CompletedAtUtc);
        Assert.True(await db.WorkflowEvents.AnyAsync(x => x.WorkflowInstanceId == approved.Id && x.EventType == "Completed"));
    }

    [Fact]
    public async Task ApproveStep_AllMode_RequiresAllAssignmentsBeforeAdvance()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var approverA = Guid.NewGuid();
        var approverB = Guid.NewGuid();
        db.Users.AddRange(
            new MESv2.Api.Models.User
            {
                Id = approverA,
                EmployeeNumber = "UTA1",
                FirstName = "A",
                LastName = "Approver",
                DisplayName = "A Approver",
                RoleTier = 2m,
                RoleName = "Quality Director",
                DefaultSiteId = TestHelpers.PlantPlt1Id,
                IsActive = true
            },
            new MESv2.Api.Models.User
            {
                Id = approverB,
                EmployeeNumber = "UTB1",
                FirstName = "B",
                LastName = "Approver",
                DisplayName = "B Approver",
                RoleTier = 3m,
                RoleName = "Quality Manager",
                DefaultSiteId = TestHelpers.PlantPlt1Id,
                IsActive = true
            });
        await db.SaveChangesAsync();
        var svc = new WorkflowEngineService(db);

        var definition = await svc.UpsertDefinitionAsync(new UpsertWorkflowDefinitionDto
        {
            WorkflowType = "AllModeFlow",
            IsActive = true,
            StartStepCode = "ApproveAll",
            Steps =
            [
                new WorkflowStepDefinitionDto
                {
                    StepCode = "ApproveAll",
                    StepName = "Approve All",
                    Sequence = 1,
                    ApprovalMode = "All",
                    ApprovalAssignments = ["role:2", "role:3"],
                    AllowReject = true,
                    OnApproveNextStepCode = "Done"
                },
                new WorkflowStepDefinitionDto
                {
                    StepCode = "Done",
                    StepName = "Done",
                    Sequence = 2,
                    ApprovalMode = "None",
                    ApprovalAssignments = [],
                    AllowReject = false
                }
            ]
        }, approverA);

        var instance = await svc.StartWorkflowAsync(new StartWorkflowRequestDto
        {
            EntityType = "Unit",
            EntityId = Guid.NewGuid(),
            WorkflowType = definition.WorkflowType
        });
        Assert.Equal("ApproveAll", instance.CurrentStepCode);
        Assert.Equal("PendingApproval", instance.Status);

        var partial = await svc.ApproveStepAsync(new ApproveRejectRequestDto
        {
            WorkflowInstanceId = instance.Id,
            StepCode = "ApproveAll",
            ActorUserId = approverA,
            Comments = "first"
        });
        Assert.Equal("ApproveAll", partial.CurrentStepCode);
        Assert.Equal("PendingApproval", partial.Status);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.ApproveStepAsync(new ApproveRejectRequestDto
        {
            WorkflowInstanceId = instance.Id,
            StepCode = "ApproveAll",
            ActorUserId = approverA,
            Comments = "duplicate"
        }));

        var final = await svc.ApproveStepAsync(new ApproveRejectRequestDto
        {
            WorkflowInstanceId = instance.Id,
            StepCode = "ApproveAll",
            ActorUserId = approverB,
            Comments = "second"
        });
        Assert.Equal("Done", final.CurrentStepCode);
        Assert.Equal("InProgress", final.Status);
    }
}
