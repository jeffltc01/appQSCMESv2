using Microsoft.Extensions.Logging.Abstractions;
using MESv2.Api.DTOs;
using MESv2.Api.Models;
using MESv2.Api.Services;
using Moq;

namespace MESv2.Api.Tests;

public class IssueRequestServiceTests
{
    private static IssueRequestService CreateService(
        Data.MesDbContext db, Mock<IGitHubService>? mockGitHub = null)
    {
        if (mockGitHub == null)
        {
            mockGitHub = new Mock<IGitHubService>();
            mockGitHub.Setup(g => g.IsConfigured).Returns(true);
            mockGitHub.Setup(g => g.CreateIssueAsync(
                    It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((42, "https://github.com/test/repo/issues/42"));
        }

        return new IssueRequestService(db, mockGitHub.Object, NullLogger<IssueRequestService>.Instance);
    }

    [Fact]
    public async Task Submit_OperatorTier_CreatesPendingRequest()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var result = await sut.SubmitAsync(new CreateIssueRequestDto
        {
            Type = (int)IssueRequestType.Bug,
            Title = "Button does not work",
            Area = "Login / Authentication",
            BodyJson = """{"description":"Broken","steps":"Click it","expected":"Works","actual":"Nothing","browser":"Chrome","severity":"High"}""",
            SubmittedByUserId = TestHelpers.TestUserId,
            SubmitterRoleTier = 6.0m
        });

        Assert.NotNull(result);
        Assert.Equal((int)IssueRequestStatus.Pending, result.Status);
        Assert.Null(result.GitHubIssueUrl);
        Assert.Equal("Button does not work", result.Title);
    }

    [Fact]
    public async Task Submit_QualityManagerTier_AutoApproves()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var mockGitHub = new Mock<IGitHubService>();
        mockGitHub.Setup(g => g.IsConfigured).Returns(true);
        mockGitHub.Setup(g => g.CreateIssueAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((99, "https://github.com/test/repo/issues/99"));

        var sut = CreateService(db, mockGitHub);

        var result = await sut.SubmitAsync(new CreateIssueRequestDto
        {
            Type = (int)IssueRequestType.FeatureRequest,
            Title = "Add dashboard",
            Area = "Menu / Navigation",
            BodyJson = """{"problem":"No overview","solution":"Add dashboard","priority":"Important"}""",
            SubmittedByUserId = TestHelpers.TestUserId,
            SubmitterRoleTier = 3.0m
        });

        Assert.Equal((int)IssueRequestStatus.Approved, result.Status);
        Assert.Equal(99, result.GitHubIssueNumber);
        Assert.Equal("https://github.com/test/repo/issues/99", result.GitHubIssueUrl);
        mockGitHub.Verify(g => g.CreateIssueAsync(
            It.Is<string>(t => t.Contains("Add dashboard")),
            It.IsAny<string>(),
            It.Is<IEnumerable<string>>(l => l.Contains("enhancement")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Submit_AdminTier_AutoApproves()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var result = await sut.SubmitAsync(new CreateIssueRequestDto
        {
            Type = (int)IssueRequestType.GeneralQuestion,
            Title = "How to configure printers?",
            Area = "Admin - Products",
            BodyJson = """{"question":"How?","context":"Need help"}""",
            SubmittedByUserId = TestHelpers.TestUserId,
            SubmitterRoleTier = 1.0m
        });

        Assert.Equal((int)IssueRequestStatus.Approved, result.Status);
        Assert.NotNull(result.GitHubIssueUrl);
    }

    [Fact]
    public async Task Approve_PendingRequest_CreatesGitHubIssue()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var mockGitHub = new Mock<IGitHubService>();
        mockGitHub.Setup(g => g.IsConfigured).Returns(true);
        mockGitHub.Setup(g => g.CreateIssueAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((55, "https://github.com/test/repo/issues/55"));

        var sut = CreateService(db, mockGitHub);

        var submitted = await sut.SubmitAsync(new CreateIssueRequestDto
        {
            Type = (int)IssueRequestType.Bug,
            Title = "Scan issue",
            Area = "Scan Overlay",
            BodyJson = """{"description":"Broken scan","steps":"Scan barcode","expected":"Works","actual":"Fails","browser":"Edge","severity":"Medium"}""",
            SubmittedByUserId = TestHelpers.TestUserId,
            SubmitterRoleTier = 6.0m
        });

        Assert.Equal((int)IssueRequestStatus.Pending, submitted.Status);

        var approved = await sut.ApproveAsync(submitted.Id, new ApproveIssueRequestDto
        {
            ReviewerUserId = TestHelpers.TestUserId
        });

        Assert.Equal((int)IssueRequestStatus.Approved, approved.Status);
        Assert.Equal(55, approved.GitHubIssueNumber);
        Assert.Equal("https://github.com/test/repo/issues/55", approved.GitHubIssueUrl);
    }

    [Fact]
    public async Task Approve_WithEdits_UsesEditedFields()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var mockGitHub = new Mock<IGitHubService>();
        mockGitHub.Setup(g => g.IsConfigured).Returns(true);
        mockGitHub.Setup(g => g.CreateIssueAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((60, "https://github.com/test/repo/issues/60"));

        var sut = CreateService(db, mockGitHub);

        var submitted = await sut.SubmitAsync(new CreateIssueRequestDto
        {
            Type = (int)IssueRequestType.Bug,
            Title = "Typo title",
            Area = "Other",
            BodyJson = """{"description":"Bad","steps":"Click","expected":"Good","actual":"Bad","browser":"Chrome","severity":"Low"}""",
            SubmittedByUserId = TestHelpers.TestUserId,
            SubmitterRoleTier = 6.0m
        });

        var approved = await sut.ApproveAsync(submitted.Id, new ApproveIssueRequestDto
        {
            ReviewerUserId = TestHelpers.TestUserId,
            Title = "Corrected title",
            Area = "Rolls / Material",
        });

        Assert.Equal("Corrected title", approved.Title);
        Assert.Equal("Rolls / Material", approved.Area);
        mockGitHub.Verify(g => g.CreateIssueAsync(
            It.Is<string>(t => t.Contains("Corrected title")),
            It.IsAny<string>(),
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Reject_PendingRequest_SetsRejectedStatus()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var submitted = await sut.SubmitAsync(new CreateIssueRequestDto
        {
            Type = (int)IssueRequestType.FeatureRequest,
            Title = "Bad idea",
            Area = "Other",
            BodyJson = """{"problem":"None","solution":"Bad","priority":"Nice to have"}""",
            SubmittedByUserId = TestHelpers.TestUserId,
            SubmitterRoleTier = 6.0m
        });

        var rejected = await sut.RejectAsync(submitted.Id, new RejectIssueRequestDto
        {
            ReviewerUserId = TestHelpers.TestUserId,
            Notes = "Not aligned with roadmap"
        });

        Assert.Equal((int)IssueRequestStatus.Rejected, rejected.Status);
        Assert.Equal("Not aligned with roadmap", rejected.ReviewerNotes);
        Assert.Null(rejected.GitHubIssueUrl);
    }

    [Fact]
    public async Task GetPending_ReturnsOnlyPendingRequests()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        await sut.SubmitAsync(new CreateIssueRequestDto
        {
            Type = (int)IssueRequestType.Bug, Title = "Pending one", Area = "Other",
            BodyJson = """{"description":"A","steps":"B","expected":"C","actual":"D","browser":"Chrome","severity":"Low"}""",
            SubmittedByUserId = TestHelpers.TestUserId, SubmitterRoleTier = 6.0m
        });

        await sut.SubmitAsync(new CreateIssueRequestDto
        {
            Type = (int)IssueRequestType.Bug, Title = "Auto-approved", Area = "Other",
            BodyJson = """{"description":"A","steps":"B","expected":"C","actual":"D","browser":"Chrome","severity":"Low"}""",
            SubmittedByUserId = TestHelpers.TestUserId, SubmitterRoleTier = 2.0m
        });

        var pending = await sut.GetPendingAsync();

        Assert.Single(pending);
        Assert.Equal("Pending one", pending[0].Title);
    }

    [Fact]
    public async Task GetMyRequests_ReturnsOnlyUserRequests()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        await sut.SubmitAsync(new CreateIssueRequestDto
        {
            Type = (int)IssueRequestType.Bug, Title = "My issue", Area = "Other",
            BodyJson = """{"description":"A","steps":"B","expected":"C","actual":"D","browser":"Chrome","severity":"Low"}""",
            SubmittedByUserId = TestHelpers.TestUserId, SubmitterRoleTier = 6.0m
        });

        var myRequests = await sut.GetMyRequestsAsync(TestHelpers.TestUserId);
        Assert.Single(myRequests);
        Assert.Equal("My issue", myRequests[0].Title);

        var otherRequests = await sut.GetMyRequestsAsync(Guid.NewGuid());
        Assert.Empty(otherRequests);
    }

    [Fact]
    public async Task Approve_AlreadyApproved_ThrowsInvalidOperation()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var submitted = await sut.SubmitAsync(new CreateIssueRequestDto
        {
            Type = (int)IssueRequestType.Bug, Title = "Double approve", Area = "Other",
            BodyJson = """{"description":"A","steps":"B","expected":"C","actual":"D","browser":"Chrome","severity":"Low"}""",
            SubmittedByUserId = TestHelpers.TestUserId, SubmitterRoleTier = 6.0m
        });

        await sut.ApproveAsync(submitted.Id, new ApproveIssueRequestDto { ReviewerUserId = TestHelpers.TestUserId });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.ApproveAsync(submitted.Id, new ApproveIssueRequestDto { ReviewerUserId = TestHelpers.TestUserId }));
    }

    [Fact]
    public async Task Reject_AlreadyRejected_ThrowsInvalidOperation()
    {
        await using var db = TestHelpers.CreateInMemoryContext();
        var sut = CreateService(db);

        var submitted = await sut.SubmitAsync(new CreateIssueRequestDto
        {
            Type = (int)IssueRequestType.Bug, Title = "Double reject", Area = "Other",
            BodyJson = """{"description":"A","steps":"B","expected":"C","actual":"D","browser":"Chrome","severity":"Low"}""",
            SubmittedByUserId = TestHelpers.TestUserId, SubmitterRoleTier = 6.0m
        });

        await sut.RejectAsync(submitted.Id, new RejectIssueRequestDto { ReviewerUserId = TestHelpers.TestUserId });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.RejectAsync(submitted.Id, new RejectIssueRequestDto { ReviewerUserId = TestHelpers.TestUserId }));
    }
}
