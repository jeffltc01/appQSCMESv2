namespace MESv2.Api.Models;

public enum IssueRequestType
{
    Bug = 0,
    FeatureRequest = 1,
    GeneralQuestion = 2
}

public enum IssueRequestStatus
{
    Pending = 0,
    Approved = 1,
    Rejected = 2
}

public class IssueRequest
{
    public Guid Id { get; set; }
    public IssueRequestType Type { get; set; }
    public IssueRequestStatus Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string BodyJson { get; set; } = "{}";

    public Guid SubmittedByUserId { get; set; }
    public User SubmittedByUser { get; set; } = null!;
    public DateTime SubmittedAt { get; set; }

    public Guid? ReviewedByUserId { get; set; }
    public User? ReviewedByUser { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewerNotes { get; set; }

    public int? GitHubIssueNumber { get; set; }
    public string? GitHubIssueUrl { get; set; }
}
