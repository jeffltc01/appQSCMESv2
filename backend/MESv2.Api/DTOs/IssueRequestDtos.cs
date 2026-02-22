namespace MESv2.Api.DTOs;

public class CreateIssueRequestDto
{
    public int Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string BodyJson { get; set; } = "{}";
    public Guid SubmittedByUserId { get; set; }
    public decimal SubmitterRoleTier { get; set; }
}

public class ApproveIssueRequestDto
{
    public Guid ReviewerUserId { get; set; }
    public string? Title { get; set; }
    public string? Area { get; set; }
    public string? BodyJson { get; set; }
}

public class RejectIssueRequestDto
{
    public Guid ReviewerUserId { get; set; }
    public string? Notes { get; set; }
}

public class IssueRequestDto
{
    public Guid Id { get; set; }
    public int Type { get; set; }
    public int Status { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string BodyJson { get; set; } = "{}";
    public Guid SubmittedByUserId { get; set; }
    public string SubmittedByName { get; set; } = string.Empty;
    public DateTime SubmittedAt { get; set; }
    public Guid? ReviewedByUserId { get; set; }
    public string? ReviewedByName { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ReviewerNotes { get; set; }
    public int? GitHubIssueNumber { get; set; }
    public string? GitHubIssueUrl { get; set; }
}
