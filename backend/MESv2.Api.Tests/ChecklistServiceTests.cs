using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;

namespace MESv2.Api.Tests;

public class ChecklistServiceTests
{
    [Fact]
    public async Task ResolveTemplate_PrefersPlantWorkCenterOverSiteDefault()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);

        var user = db.Users.First();
        var siteId = TestHelpers.PlantPlt1Id;
        var workCenterId = TestHelpers.wcRollsId;

        db.ChecklistTemplates.Add(new ChecklistTemplate
        {
            Id = Guid.NewGuid(),
            TemplateCode = "SITE-DEFAULT",
            Title = "Site Default",
            ChecklistType = "SafetyPreShift",
            ScopeLevel = ChecklistScopeLevels.SiteDefault,
            SiteId = siteId,
            VersionNo = 1,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            ResponseMode = ChecklistResponseModes.PassFail,
            OwnerUserId = user.Id,
            CreatedByUserId = user.Id,
            Items =
            {
                new ChecklistTemplateItem
                {
                    Id = Guid.NewGuid(),
                    SortOrder = 1,
                    Prompt = "Site question"
                }
            }
        });

        db.ChecklistTemplates.Add(new ChecklistTemplate
        {
            Id = Guid.NewGuid(),
            TemplateCode = "WC-SPECIFIC",
            Title = "WC Specific",
            ChecklistType = "SafetyPreShift",
            ScopeLevel = ChecklistScopeLevels.PlantWorkCenter,
            SiteId = siteId,
            WorkCenterId = workCenterId,
            VersionNo = 1,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            ResponseMode = ChecklistResponseModes.PassFail,
            OwnerUserId = user.Id,
            CreatedByUserId = user.Id,
            Items =
            {
                new ChecklistTemplateItem
                {
                    Id = Guid.NewGuid(),
                    SortOrder = 1,
                    Prompt = "WC question"
                }
            }
        });
        await db.SaveChangesAsync();

        var resolved = await service.ResolveTemplateAsync(new ResolveChecklistTemplateRequestDto
        {
            ChecklistType = "SafetyPreShift",
            SiteId = siteId,
            WorkCenterId = workCenterId,
            ProductionLineId = null
        }, siteId);

        Assert.NotNull(resolved);
        Assert.Equal("WC-SPECIFIC", resolved.TemplateCode);
        Assert.Equal(ChecklistScopeLevels.PlantWorkCenter, resolved.ScopeLevel);
        Assert.Equal(ChecklistQuestionResponseTypes.Checkbox, resolved.Items[0].ResponseType);
    }

    [Fact]
    public async Task SubmitResponses_AcceptsCheckboxResponse()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);

        var user = db.Users.First();
        var siteId = TestHelpers.PlantPlt1Id;
        var workCenterId = TestHelpers.wcRollsId;
        var templateId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        db.ChecklistTemplates.Add(new ChecklistTemplate
        {
            Id = templateId,
            TemplateCode = "SAFE-FAIL-NOTE",
            Title = "Safety",
            ChecklistType = "SafetyPreShift",
            ScopeLevel = ChecklistScopeLevels.PlantWorkCenter,
            SiteId = siteId,
            WorkCenterId = workCenterId,
            VersionNo = 1,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            ResponseMode = ChecklistResponseModes.PassFail,
            RequireFailNote = true,
            IsSafetyProfile = true,
            OwnerUserId = user.Id,
            CreatedByUserId = user.Id,
            Items =
            {
                new ChecklistTemplateItem
                {
                    Id = itemId,
                    SortOrder = 1,
                    Prompt = "Guard in place?"
                }
            }
        });
        await db.SaveChangesAsync();

        var entry = await service.CreateEntryAsync(new CreateChecklistEntryRequestDto
        {
            ChecklistType = "SafetyPreShift",
            SiteId = siteId,
            WorkCenterId = workCenterId,
            OperatorUserId = user.Id
        }, siteId, 6m);

        var updated = await service.SubmitResponsesAsync(entry.Id, new SubmitChecklistResponsesRequestDto
        {
            Responses =
            [
                new ChecklistResponseDto
                {
                    ChecklistTemplateItemId = itemId,
                    ResponseValue = "true"
                }
            ]
        }, siteId, 6m);

        Assert.NotNull(updated);
        Assert.Single(updated.Responses);
        Assert.Equal("true", updated.Responses[0].ResponseValue);
    }

    [Fact]
    public async Task CompleteEntry_RequiresAllRequiredItems()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);

        var user = db.Users.First();
        var siteId = TestHelpers.PlantPlt1Id;
        var workCenterId = TestHelpers.wcRollsId;
        var item1 = Guid.NewGuid();
        var item2 = Guid.NewGuid();

        db.ChecklistTemplates.Add(new ChecklistTemplate
        {
            Id = Guid.NewGuid(),
            TemplateCode = "OPS-REQ",
            Title = "Ops",
            ChecklistType = "OpsPreShift",
            ScopeLevel = ChecklistScopeLevels.PlantWorkCenter,
            SiteId = siteId,
            WorkCenterId = workCenterId,
            VersionNo = 1,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            ResponseMode = ChecklistResponseModes.PassFailNa,
            OwnerUserId = user.Id,
            CreatedByUserId = user.Id,
            Items =
            {
                new ChecklistTemplateItem { Id = item1, SortOrder = 1, Prompt = "Item 1", IsRequired = true },
                new ChecklistTemplateItem { Id = item2, SortOrder = 2, Prompt = "Item 2", IsRequired = true }
            }
        });
        await db.SaveChangesAsync();

        var entry = await service.CreateEntryAsync(new CreateChecklistEntryRequestDto
        {
            ChecklistType = "OpsPreShift",
            SiteId = siteId,
            WorkCenterId = workCenterId,
            OperatorUserId = user.Id
        }, siteId, 6m);

        await service.SubmitResponsesAsync(entry.Id, new SubmitChecklistResponsesRequestDto
        {
            Responses =
            [
                new ChecklistResponseDto
                {
                    ChecklistTemplateItemId = item1,
                    ResponseValue = "true"
                }
            ]
        }, siteId, 6m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteEntryAsync(entry.Id, siteId, 6m));
    }

    [Fact]
    public async Task UpsertTemplate_RejectsRemovedLegacyResponseType()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var user = db.Users.First();
        var siteId = TestHelpers.PlantPlt1Id;
        var workCenterId = TestHelpers.wcRollsId;

        var request = new UpsertChecklistTemplateRequestDto
        {
            TemplateCode = "OPS-SELECT",
            Title = "Ops Select",
            ChecklistType = "OpsPreShift",
            ScopeLevel = ChecklistScopeLevels.PlantWorkCenter,
            SiteId = siteId,
            WorkCenterId = workCenterId,
            VersionNo = 1,
            EffectiveFromUtc = DateTime.UtcNow,
            IsActive = true,
            ResponseMode = ChecklistResponseModes.PassFailNa,
            OwnerUserId = user.Id,
            Items =
            [
                new ChecklistTemplateItemDto
                {
                    SortOrder = 1,
                    Prompt = "Pick an option",
                    IsRequired = true,
                    ResponseType = "Select",
                }
            ]
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertTemplateAsync(request, user.Id, 1m, siteId));
    }

    [Fact]
    public async Task UpsertTemplate_AllowsAddingQuestion_OnExistingTemplate()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var user = db.Users.First();
        var siteId = TestHelpers.PlantPlt1Id;
        var workCenterId = TestHelpers.wcRollsId;

        var templateId = Guid.NewGuid();
        var itemId = Guid.NewGuid();

        db.ChecklistTemplates.Add(new ChecklistTemplate
        {
            Id = templateId,
            TemplateCode = "SAFE-ADD",
            Title = "Safety Add",
            ChecklistType = "SafetyPreShift",
            ScopeLevel = ChecklistScopeLevels.PlantWorkCenter,
            SiteId = siteId,
            WorkCenterId = workCenterId,
            VersionNo = 1,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            ResponseMode = ChecklistResponseModes.PassFail,
            OwnerUserId = user.Id,
            CreatedByUserId = user.Id,
            Items =
            {
                new ChecklistTemplateItem
                {
                    Id = itemId,
                    SortOrder = 1,
                    Prompt = "Existing prompt",
                    IsRequired = true
                }
            }
        });

        await db.SaveChangesAsync();
        var service = new ChecklistService(db);

        var request = new UpsertChecklistTemplateRequestDto
        {
            Id = templateId,
            TemplateCode = "SAFE-ADD",
            Title = "Safety Add",
            ChecklistType = "SafetyPreShift",
            ScopeLevel = ChecklistScopeLevels.PlantWorkCenter,
            SiteId = siteId,
            WorkCenterId = workCenterId,
            VersionNo = 1,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            ResponseMode = ChecklistResponseModes.PassFail,
            OwnerUserId = user.Id,
            Items =
            [
                new ChecklistTemplateItemDto
                {
                    Id = itemId,
                    SortOrder = 1,
                    Prompt = "Existing prompt",
                    IsRequired = true,
                    ResponseType = ChecklistQuestionResponseTypes.Checkbox
                },
                new ChecklistTemplateItemDto
                {
                    SortOrder = 2,
                    Prompt = "New prompt",
                    IsRequired = true,
                    ResponseType = ChecklistQuestionResponseTypes.Checkbox
                }
            ]
        };

        var updated = await service.UpsertTemplateAsync(request, user.Id, 1m, siteId);
        Assert.Equal(2, updated.Items.Count);
        Assert.Contains(updated.Items, i => i.Prompt == "New prompt");
    }

    [Fact]
    public async Task UpsertTemplate_DoesNotDeleteExistingQuestion_WhenDeleteNotExplicit()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var user = db.Users.First();
        var siteId = TestHelpers.PlantPlt1Id;
        var workCenterId = TestHelpers.wcRollsId;

        var templateId = Guid.NewGuid();
        var existingItemId = Guid.NewGuid();

        db.ChecklistTemplates.Add(new ChecklistTemplate
        {
            Id = templateId,
            TemplateCode = "SAFE-NO-DELETE",
            Title = "Safety No Delete",
            ChecklistType = "SafetyPreShift",
            ScopeLevel = ChecklistScopeLevels.PlantWorkCenter,
            SiteId = siteId,
            WorkCenterId = workCenterId,
            VersionNo = 1,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            ResponseMode = ChecklistResponseModes.PassFail,
            OwnerUserId = user.Id,
            CreatedByUserId = user.Id,
            Items =
            {
                new ChecklistTemplateItem
                {
                    Id = existingItemId,
                    SortOrder = 1,
                    Prompt = "Existing prompt",
                    IsRequired = true
                }
            }
        });
        await db.SaveChangesAsync();

        var service = new ChecklistService(db);
        var request = new UpsertChecklistTemplateRequestDto
        {
            Id = templateId,
            TemplateCode = "SAFE-NO-DELETE",
            Title = "Safety No Delete",
            ChecklistType = "SafetyPreShift",
            ScopeLevel = ChecklistScopeLevels.PlantWorkCenter,
            SiteId = siteId,
            WorkCenterId = workCenterId,
            VersionNo = 1,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            ResponseMode = ChecklistResponseModes.PassFail,
            OwnerUserId = user.Id,
            Items =
            [
                new ChecklistTemplateItemDto
                {
                    SortOrder = 2,
                    Prompt = "New prompt",
                    IsRequired = true,
                    ResponseType = ChecklistQuestionResponseTypes.Checkbox
                }
            ]
        };

        var updated = await service.UpsertTemplateAsync(request, user.Id, 1m, siteId);
        Assert.Equal(2, updated.Items.Count);
        Assert.Contains(updated.Items, i => i.Id == existingItemId);
        Assert.Contains(updated.Items, i => i.Prompt == "New prompt");
    }

    [Fact]
    public async Task UpsertTemplate_RejectsRemovingQuestion_WhenResponsesExist()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var user = db.Users.First();
        var siteId = TestHelpers.PlantPlt1Id;
        var workCenterId = TestHelpers.wcRollsId;

        var templateId = Guid.NewGuid();
        var itemId = Guid.NewGuid();
        var entryId = Guid.NewGuid();

        db.ChecklistTemplates.Add(new ChecklistTemplate
        {
            Id = templateId,
            TemplateCode = "SAFE-REMOVE",
            Title = "Safety Remove",
            ChecklistType = "SafetyPreShift",
            ScopeLevel = ChecklistScopeLevels.PlantWorkCenter,
            SiteId = siteId,
            WorkCenterId = workCenterId,
            VersionNo = 1,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            ResponseMode = ChecklistResponseModes.PassFail,
            OwnerUserId = user.Id,
            CreatedByUserId = user.Id,
            Items =
            {
                new ChecklistTemplateItem
                {
                    Id = itemId,
                    SortOrder = 1,
                    Prompt = "Existing prompt",
                    IsRequired = true
                }
            }
        });

        db.ChecklistEntries.Add(new ChecklistEntry
        {
            Id = entryId,
            ChecklistTemplateId = templateId,
            ChecklistType = "SafetyPreShift",
            SiteId = siteId,
            WorkCenterId = workCenterId,
            OperatorUserId = user.Id,
            Status = ChecklistEntryStatuses.Completed,
            StartedAtUtc = DateTime.UtcNow.AddHours(-1),
            CompletedAtUtc = DateTime.UtcNow,
            ResolvedFromScope = ChecklistScopeLevels.PlantWorkCenter,
            ResolvedTemplateCode = "SAFE-REMOVE",
            ResolvedTemplateVersionNo = 1
        });

        db.ChecklistEntryItemResponses.Add(new ChecklistEntryItemResponse
        {
            Id = Guid.NewGuid(),
            ChecklistEntryId = entryId,
            ChecklistTemplateItemId = itemId,
            ResponseValue = "Pass",
            RespondedAtUtc = DateTime.UtcNow
        });

        await db.SaveChangesAsync();

        var request = new UpsertChecklistTemplateRequestDto
        {
            Id = templateId,
            TemplateCode = "SAFE-REMOVE",
            Title = "Safety Remove",
            ChecklistType = "SafetyPreShift",
            ScopeLevel = ChecklistScopeLevels.PlantWorkCenter,
            SiteId = siteId,
            WorkCenterId = workCenterId,
            VersionNo = 1,
            EffectiveFromUtc = DateTime.UtcNow.AddDays(-1),
            IsActive = true,
            ResponseMode = ChecklistResponseModes.PassFail,
            OwnerUserId = user.Id,
            DeletedItemIds = [itemId],
            Items =
            [
                new ChecklistTemplateItemDto
                {
                    SortOrder = 1,
                    Prompt = "Replacement prompt",
                    IsRequired = true,
                    ResponseType = ChecklistQuestionResponseTypes.Checkbox
                }
            ]
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertTemplateAsync(request, user.Id, 1m, siteId));
        Assert.Contains("cannot be removed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task UpsertTemplate_AllowsAdminAssigningDifferentOwnerOnCreate()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var caller = db.Users.First();
        var assignee = new User
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP900",
            FirstName = "Assigned",
            LastName = "Owner",
            DisplayName = "Assigned Owner",
            RoleTier = 5m,
            RoleName = "Team Lead",
            DefaultSiteId = TestHelpers.PlantPlt1Id,
            IsActive = true
        };
        db.Users.Add(assignee);
        await db.SaveChangesAsync();

        var request = BuildTemplateRequest(ownerUserId: assignee.Id);
        var saved = await service.UpsertTemplateAsync(request, caller.Id, 1m, TestHelpers.PlantPlt1Id);
        Assert.Equal(assignee.Id, saved.OwnerUserId);
    }

    [Fact]
    public async Task UpsertTemplate_AllowsOwnerToEditRegardlessRoleTier()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var owner = db.Users.First();
        var request = BuildTemplateRequest(owner.Id);
        var created = await service.UpsertTemplateAsync(request, owner.Id, 1m, TestHelpers.PlantPlt1Id);

        request.Id = created.Id;
        request.Title = "Updated title";
        var updated = await service.UpsertTemplateAsync(request, owner.Id, 6m, TestHelpers.PlantPlt1Id);
        Assert.Equal("Updated title", updated.Title);
    }

    [Fact]
    public async Task UpsertTemplate_RejectsEditByNonOwner()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var owner = db.Users.First();
        var anotherUser = new User
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = "EMP901",
            FirstName = "Other",
            LastName = "User",
            DisplayName = "Other User",
            RoleTier = 6m,
            RoleName = "Operator",
            DefaultSiteId = TestHelpers.PlantPlt1Id,
            IsActive = true
        };
        db.Users.Add(anotherUser);
        await db.SaveChangesAsync();

        var request = BuildTemplateRequest(owner.Id);
        var created = await service.UpsertTemplateAsync(request, owner.Id, 1m, TestHelpers.PlantPlt1Id);
        request.Id = created.Id;
        request.Title = "Attempted by non-owner";

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertTemplateAsync(request, anotherUser.Id, 6m, TestHelpers.PlantPlt1Id));
    }

    [Fact]
    public async Task UpsertTemplate_RejectsPassFailLegacyResponseType()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var caller = db.Users.First();
        var request = BuildTemplateRequest(caller.Id);
        request.Items[0].ResponseType = "PassFail";

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertTemplateAsync(request, caller.Id, 1m, TestHelpers.PlantPlt1Id));
    }

    [Fact]
    public async Task UpsertTemplate_RejectsUnsupportedChecklistType()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var caller = db.Users.First();
        var request = BuildTemplateRequest(caller.Id);
        request.ChecklistType = "SafetyPreShit";

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertTemplateAsync(request, caller.Id, 1m, TestHelpers.PlantPlt1Id));
    }

    [Fact]
    public async Task UpsertTemplate_RejectsScoreWithoutScoreTypeId()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var caller = db.Users.First();
        var request = BuildTemplateRequest(caller.Id);
        request.Items[0].ResponseType = ChecklistQuestionResponseTypes.Score;
        request.Items[0].ScoreTypeId = null;

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertTemplateAsync(request, caller.Id, 1m, TestHelpers.PlantPlt1Id));
    }

    [Fact]
    public async Task UpsertTemplate_RejectsDimensionWithInvalidBounds()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var caller = db.Users.First();
        var request = BuildTemplateRequest(caller.Id);
        request.Items[0].ResponseType = ChecklistQuestionResponseTypes.Dimension;
        request.Items[0].DimensionLowerLimit = 8m;
        request.Items[0].DimensionTarget = 5m;
        request.Items[0].DimensionUpperLimit = 10m;
        request.Items[0].DimensionUnitOfMeasure = "inches";

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertTemplateAsync(request, caller.Id, 1m, TestHelpers.PlantPlt1Id));
    }

    [Theory]
    [InlineData(1.0)]
    [InlineData(2.0)]
    public async Task UpsertScoreType_AllowsAdminAndDirectorRoles(double roleTier)
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var caller = db.Users.First();
        var request = new UpsertScoreTypeRequestDto
        {
            Name = $"Score Type {roleTier}",
            IsActive = true,
            Values =
            [
                new ScoreTypeValueDto { Score = 1m, Description = "Poor", SortOrder = 1 }
            ]
        };

        var saved = await service.UpsertScoreTypeAsync(request, caller.Id, (decimal)roleTier);
        Assert.Equal(request.Name, saved.Name);
        Assert.Single(saved.Values);
    }

    [Fact]
    public async Task UpsertScoreType_RejectsRoleBelowDirector()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var caller = db.Users.First();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertScoreTypeAsync(new UpsertScoreTypeRequestDto
            {
                Name = "Blocked",
                IsActive = true,
                Values = [new ScoreTypeValueDto { Score = 1m, Description = "Value", SortOrder = 1 }]
            }, caller.Id, 3m));
    }

    [Fact]
    public async Task UpsertScoreType_RejectsDuplicateScoreDescriptionPair()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var caller = db.Users.First();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertScoreTypeAsync(new UpsertScoreTypeRequestDto
            {
                Name = "Duplicate Pair",
                IsActive = true,
                Values =
                [
                    new ScoreTypeValueDto { Score = 1m, Description = "Same", SortOrder = 1 },
                    new ScoreTypeValueDto { Score = 1m, Description = "same", SortOrder = 2 }
                ]
            }, caller.Id, 1m));
    }

    [Fact]
    public async Task UpsertScoreType_RejectsDuplicateActiveNameCaseInsensitive()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var service = new ChecklistService(db);
        var caller = db.Users.First();

        await service.UpsertScoreTypeAsync(new UpsertScoreTypeRequestDto
        {
            Name = "Quality Score",
            IsActive = true,
            Values = [new ScoreTypeValueDto { Score = 1m, Description = "A", SortOrder = 1 }]
        }, caller.Id, 1m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertScoreTypeAsync(new UpsertScoreTypeRequestDto
            {
                Name = "quality score",
                IsActive = true,
                Values = [new ScoreTypeValueDto { Score = 2m, Description = "B", SortOrder = 1 }]
            }, caller.Id, 1m));
    }

    private static UpsertChecklistTemplateRequestDto BuildTemplateRequest(Guid ownerUserId) =>
        new()
        {
            TemplateCode = $"TEMP-{Guid.NewGuid():N}".Substring(0, 12),
            Title = "Template",
            ChecklistType = "OpsPreShift",
            ScopeLevel = ChecklistScopeLevels.PlantWorkCenter,
            SiteId = TestHelpers.PlantPlt1Id,
            WorkCenterId = TestHelpers.wcRollsId,
            VersionNo = 1,
            EffectiveFromUtc = DateTime.UtcNow,
            IsActive = true,
            ResponseMode = ChecklistResponseModes.PassFailNa,
            OwnerUserId = ownerUserId,
            Items =
            [
                new ChecklistTemplateItemDto
                {
                    SortOrder = 1,
                    Prompt = "Item 1",
                    IsRequired = true,
                    ResponseType = ChecklistQuestionResponseTypes.Checkbox
                }
            ]
        };
}
