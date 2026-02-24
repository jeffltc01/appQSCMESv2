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
        Assert.Equal(ChecklistQuestionResponseTypes.PassFail, resolved.Items[0].ResponseType);
    }

    [Fact]
    public async Task SubmitResponses_RequiresFailNote_WhenSafetyTemplateRequiresIt()
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

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubmitResponsesAsync(entry.Id, new SubmitChecklistResponsesRequestDto
            {
                Responses =
                [
                    new ChecklistResponseDto
                    {
                        ChecklistTemplateItemId = itemId,
                        ResponseValue = "Fail"
                    }
                ]
            }, siteId, 6m));
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
                    ResponseValue = "Pass"
                }
            ]
        }, siteId, 6m);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CompleteEntryAsync(entry.Id, siteId, 6m));
    }

    [Fact]
    public async Task UpsertTemplate_RejectsSelectWithoutAtLeastTwoOptions()
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
            Items =
            [
                new ChecklistTemplateItemDto
                {
                    SortOrder = 1,
                    Prompt = "Pick an option",
                    IsRequired = true,
                    ResponseType = ChecklistQuestionResponseTypes.Select,
                    ResponseOptions = ["Only One"]
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
            Items =
            [
                new ChecklistTemplateItemDto
                {
                    Id = itemId,
                    SortOrder = 1,
                    Prompt = "Existing prompt",
                    IsRequired = true,
                    ResponseType = ChecklistQuestionResponseTypes.PassFail
                },
                new ChecklistTemplateItemDto
                {
                    SortOrder = 2,
                    Prompt = "New prompt",
                    IsRequired = true,
                    ResponseType = ChecklistQuestionResponseTypes.PassFail
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
            Items =
            [
                new ChecklistTemplateItemDto
                {
                    SortOrder = 2,
                    Prompt = "New prompt",
                    IsRequired = true,
                    ResponseType = ChecklistQuestionResponseTypes.PassFail
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
            DeletedItemIds = [itemId],
            Items =
            [
                new ChecklistTemplateItemDto
                {
                    SortOrder = 1,
                    Prompt = "Replacement prompt",
                    IsRequired = true,
                    ResponseType = ChecklistQuestionResponseTypes.PassFail
                }
            ]
        };

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.UpsertTemplateAsync(request, user.Id, 1m, siteId));
        Assert.Contains("cannot be removed", ex.Message, StringComparison.OrdinalIgnoreCase);
    }
}
