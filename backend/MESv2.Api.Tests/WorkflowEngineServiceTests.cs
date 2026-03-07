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

    [Fact]
    public async Task UpsertDefinition_WithSourceDefinition_CreatesNextVersionForSameWorkflowType()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new WorkflowEngineService(db);

        var baseDefinition = await svc.UpsertDefinitionAsync(new UpsertWorkflowDefinitionDto
        {
            WorkflowType = "CloneFlow",
            IsActive = true,
            StartStepCode = "Start",
            Steps =
            [
                new WorkflowStepDefinitionDto
                {
                    StepCode = "Start",
                    StepName = "Start",
                    Sequence = 1,
                    ApprovalMode = "None",
                    ApprovalAssignments = [],
                    AllowReject = false
                }
            ]
        }, TestHelpers.TestUserId);

        var cloned = await svc.UpsertDefinitionAsync(new UpsertWorkflowDefinitionDto
        {
            SourceDefinitionIdForNewVersion = baseDefinition.Id,
            WorkflowType = "IgnoredByServiceWhenSourcePresent",
            IsActive = false,
            StartStepCode = "Start",
            Steps =
            [
                new WorkflowStepDefinitionDto
                {
                    StepCode = "Start",
                    StepName = "Start Updated",
                    Sequence = 1,
                    ApprovalMode = "None",
                    ApprovalAssignments = [],
                    AllowReject = false
                }
            ]
        }, TestHelpers.TestUserId);

        Assert.Equal("CloneFlow", cloned.WorkflowType);
        Assert.Equal(2, cloned.Version);
        Assert.False(cloned.IsActive);
    }

    [Fact]
    public async Task UpsertDefinition_WhenNewVersionInactive_PreservesExistingActiveDefinition()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new WorkflowEngineService(db);

        var v1 = await svc.UpsertDefinitionAsync(new UpsertWorkflowDefinitionDto
        {
            WorkflowType = "ActivationFlow",
            IsActive = true,
            StartStepCode = "A",
            Steps =
            [
                new WorkflowStepDefinitionDto
                {
                    StepCode = "A",
                    StepName = "A",
                    Sequence = 1,
                    ApprovalMode = "None",
                    ApprovalAssignments = [],
                    AllowReject = false
                }
            ]
        }, TestHelpers.TestUserId);

        var v2 = await svc.UpsertDefinitionAsync(new UpsertWorkflowDefinitionDto
        {
            WorkflowType = "ActivationFlow",
            IsActive = false,
            StartStepCode = "A",
            Steps =
            [
                new WorkflowStepDefinitionDto
                {
                    StepCode = "A",
                    StepName = "A copy",
                    Sequence = 1,
                    ApprovalMode = "None",
                    ApprovalAssignments = [],
                    AllowReject = false
                }
            ]
        }, TestHelpers.TestUserId);

        var all = await svc.GetDefinitionsAsync("ActivationFlow");
        var activeVersions = all.Where(x => x.IsActive).Select(x => x.Version).ToList();

        Assert.Equal(1, v1.Version);
        Assert.Equal(2, v2.Version);
        Assert.Single(activeVersions);
        Assert.Equal(1, activeVersions[0]);
    }

    [Fact]
    public async Task GetDefinitions_OrdersStepsBySequence()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new WorkflowEngineService(db);

        await svc.UpsertDefinitionAsync(new UpsertWorkflowDefinitionDto
        {
            WorkflowType = "ReorderFlow",
            IsActive = true,
            StartStepCode = "S2",
            Steps =
            [
                new WorkflowStepDefinitionDto
                {
                    StepCode = "S2",
                    StepName = "Second",
                    Sequence = 2,
                    ApprovalMode = "None",
                    ApprovalAssignments = [],
                    AllowReject = false
                },
                new WorkflowStepDefinitionDto
                {
                    StepCode = "S1",
                    StepName = "First",
                    Sequence = 1,
                    ApprovalMode = "None",
                    ApprovalAssignments = [],
                    AllowReject = false
                }
            ]
        }, TestHelpers.TestUserId);

        var definitions = await svc.GetDefinitionsAsync("ReorderFlow");
        var stepCodes = definitions.Single().Steps.Select(x => x.StepCode).ToList();

        Assert.Equal(["S1", "S2"], stepCodes);
    }

    [Fact]
    public async Task UpsertNotificationRule_PersistsTemplateContent()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new WorkflowEngineService(db);

        var created = await svc.UpsertNotificationRuleAsync(new NotificationRuleDto
        {
            WorkflowType = "HoldTag",
            TriggerEvent = "Created",
            TargetStepCodes = [],
            RecipientMode = "Roles",
            RecipientConfigJson = "[\"3\"]",
            TemplateKey = "HoldTag.Created",
            TemplateTitle = "Hold Tag Created",
            TemplateBody = "Hold Tag {{holdTagNumber}} created at {{siteCode}}.",
            ClearPolicy = "OnEntityComplete",
            IsActive = true,
        });

        var saved = await svc.GetNotificationRulesAsync("HoldTag");
        Assert.Contains(saved, x =>
            x.Id == created.Id &&
            x.TemplateTitle == "Hold Tag Created" &&
            x.TemplateBody.Contains("{{holdTagNumber}}", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UpsertNotificationRule_StepEntered_RequiresAndPersistsTargetStepCodes()
    {
        using var db = TestHelpers.CreateInMemoryContext();
        var svc = new WorkflowEngineService(db);

        await svc.UpsertDefinitionAsync(new UpsertWorkflowDefinitionDto
        {
            WorkflowType = "ScopedFlow",
            IsActive = true,
            StartStepCode = "Start",
            Steps =
            [
                new WorkflowStepDefinitionDto
                {
                    StepCode = "Start",
                    StepName = "Start",
                    Sequence = 1,
                    ApprovalMode = "None",
                    ApprovalAssignments = [],
                    AllowReject = false,
                    OnApproveNextStepCode = "Review"
                },
                new WorkflowStepDefinitionDto
                {
                    StepCode = "Review",
                    StepName = "Review",
                    Sequence = 2,
                    ApprovalMode = "AnyOne",
                    ApprovalAssignments = [$"user:{TestHelpers.TestUserId}"],
                    AllowReject = true
                }
            ]
        }, TestHelpers.TestUserId);

        await Assert.ThrowsAsync<InvalidOperationException>(() => svc.UpsertNotificationRuleAsync(new NotificationRuleDto
        {
            WorkflowType = "ScopedFlow",
            TriggerEvent = "StepEntered",
            TargetStepCodes = [],
            RecipientMode = "Roles",
            RecipientConfigJson = "[\"3\"]",
            TemplateKey = "ScopedFlow.StepEntered",
            TemplateTitle = "Step Entered",
            TemplateBody = "Workflow moved to a scoped step.",
            ClearPolicy = "None",
            IsActive = true,
        }));

        var created = await svc.UpsertNotificationRuleAsync(new NotificationRuleDto
        {
            WorkflowType = "ScopedFlow",
            TriggerEvent = "StepEntered",
            TargetStepCodes = ["Review"],
            RecipientMode = "Roles",
            RecipientConfigJson = "[\"3\"]",
            TemplateKey = "ScopedFlow.StepEntered",
            TemplateTitle = "Step Entered",
            TemplateBody = "Workflow moved to review.",
            ClearPolicy = "None",
            IsActive = true,
        });

        Assert.Equal(["Review"], created.TargetStepCodes);
    }
}
