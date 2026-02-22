using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class IssueRequestService : IIssueRequestService
{
    private readonly MesDbContext _db;
    private readonly IGitHubService _github;
    private readonly ILogger<IssueRequestService> _logger;

    private const decimal AutoApproveTierThreshold = 3.0m;

    public IssueRequestService(MesDbContext db, IGitHubService github, ILogger<IssueRequestService> logger)
    {
        _db = db;
        _github = github;
        _logger = logger;
    }

    public async Task<IssueRequestDto> SubmitAsync(CreateIssueRequestDto dto, CancellationToken ct = default)
    {
        var issueType = (IssueRequestType)dto.Type;

        var entity = new IssueRequest
        {
            Id = Guid.NewGuid(),
            Type = issueType,
            Status = IssueRequestStatus.Pending,
            Title = dto.Title.Trim(),
            Area = dto.Area,
            BodyJson = dto.BodyJson,
            SubmittedByUserId = dto.SubmittedByUserId,
            SubmittedAt = DateTime.UtcNow
        };

        if (dto.SubmitterRoleTier <= AutoApproveTierThreshold)
        {
            if (_github.IsConfigured)
            {
                try
                {
                    var (number, url) = await CreateGitHubIssueFromEntity(entity, ct);
                    entity.Status = IssueRequestStatus.Approved;
                    entity.ReviewedByUserId = dto.SubmittedByUserId;
                    entity.ReviewedAt = DateTime.UtcNow;
                    entity.GitHubIssueNumber = number;
                    entity.GitHubIssueUrl = url;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "GitHub issue creation failed during auto-approve; saving as Pending");
                }
            }
            else
            {
                _logger.LogInformation("GitHub token not configured; QM+ issue saved as Pending");
            }
        }

        _db.IssueRequests.Add(entity);
        await _db.SaveChangesAsync(ct);

        return await LoadDto(entity.Id, ct);
    }

    public async Task<IReadOnlyList<IssueRequestDto>> GetPendingAsync(CancellationToken ct = default)
    {
        return await _db.IssueRequests
            .Include(ir => ir.SubmittedByUser)
            .Include(ir => ir.ReviewedByUser)
            .Where(ir => ir.Status == IssueRequestStatus.Pending)
            .OrderBy(ir => ir.SubmittedAt)
            .Select(ir => MapToDto(ir))
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<IssueRequestDto>> GetMyRequestsAsync(Guid userId, CancellationToken ct = default)
    {
        return await _db.IssueRequests
            .Include(ir => ir.SubmittedByUser)
            .Include(ir => ir.ReviewedByUser)
            .Where(ir => ir.SubmittedByUserId == userId)
            .OrderByDescending(ir => ir.SubmittedAt)
            .Select(ir => MapToDto(ir))
            .ToListAsync(ct);
    }

    public async Task<IssueRequestDto> ApproveAsync(Guid id, ApproveIssueRequestDto dto, CancellationToken ct = default)
    {
        var entity = await _db.IssueRequests.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"IssueRequest {id} not found");

        if (entity.Status != IssueRequestStatus.Pending)
            throw new InvalidOperationException($"IssueRequest {id} is not pending");

        if (!_github.IsConfigured)
            throw new InvalidOperationException(
                "Cannot approve: GitHub token is not configured. Set the GitHub:Token in appsettings or Azure App Settings.");

        if (dto.Title != null) entity.Title = dto.Title.Trim();
        if (dto.Area != null) entity.Area = dto.Area;
        if (dto.BodyJson != null) entity.BodyJson = dto.BodyJson;

        var (number, url) = await CreateGitHubIssueFromEntity(entity, ct);

        entity.Status = IssueRequestStatus.Approved;
        entity.ReviewedByUserId = dto.ReviewerUserId;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.GitHubIssueNumber = number;
        entity.GitHubIssueUrl = url;

        await _db.SaveChangesAsync(ct);
        return await LoadDto(id, ct);
    }

    public async Task<IssueRequestDto> RejectAsync(Guid id, RejectIssueRequestDto dto, CancellationToken ct = default)
    {
        var entity = await _db.IssueRequests.FindAsync([id], ct)
            ?? throw new KeyNotFoundException($"IssueRequest {id} not found");

        if (entity.Status != IssueRequestStatus.Pending)
            throw new InvalidOperationException($"IssueRequest {id} is not pending");

        entity.Status = IssueRequestStatus.Rejected;
        entity.ReviewedByUserId = dto.ReviewerUserId;
        entity.ReviewedAt = DateTime.UtcNow;
        entity.ReviewerNotes = dto.Notes;

        await _db.SaveChangesAsync(ct);
        return await LoadDto(id, ct);
    }

    private async Task<(int Number, string Url)> CreateGitHubIssueFromEntity(IssueRequest entity, CancellationToken ct)
    {
        var body = FormatMarkdownBody(entity);
        var labels = GetLabels(entity.Type);
        var title = FormatTitle(entity);

        return await _github.CreateIssueAsync(title, body, labels, ct);
    }

    private static string FormatTitle(IssueRequest entity)
    {
        return entity.Type switch
        {
            IssueRequestType.Bug => $"[Bug]: {entity.Title}",
            IssueRequestType.FeatureRequest => $"[Feature]: {entity.Title}",
            IssueRequestType.GeneralQuestion => $"[Question]: {entity.Title}",
            _ => entity.Title
        };
    }

    private static List<string> GetLabels(IssueRequestType type)
    {
        return type switch
        {
            IssueRequestType.Bug => ["bug"],
            IssueRequestType.FeatureRequest => ["enhancement"],
            IssueRequestType.GeneralQuestion => ["question"],
            _ => []
        };
    }

    private static string FormatMarkdownBody(IssueRequest entity)
    {
        var fields = JsonSerializer.Deserialize<Dictionary<string, string>>(entity.BodyJson)
            ?? new Dictionary<string, string>();

        return entity.Type switch
        {
            IssueRequestType.Bug => FormatBugBody(entity.Area, fields),
            IssueRequestType.FeatureRequest => FormatFeatureBody(entity.Area, fields),
            IssueRequestType.GeneralQuestion => FormatQuestionBody(entity.Area, fields),
            _ => $"**Area:** {entity.Area}\n\n{entity.BodyJson}"
        };
    }

    private static string FormatBugBody(string area, Dictionary<string, string> f)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"### Area of the application\n\n{area}\n");
        if (f.TryGetValue("description", out var desc)) sb.AppendLine($"### Describe the bug\n\n{desc}\n");
        if (f.TryGetValue("steps", out var steps)) sb.AppendLine($"### Steps to reproduce\n\n{steps}\n");
        if (f.TryGetValue("expected", out var exp)) sb.AppendLine($"### Expected behavior\n\n{exp}\n");
        if (f.TryGetValue("actual", out var act)) sb.AppendLine($"### Actual behavior\n\n{act}\n");
        if (f.TryGetValue("screenshots", out var ss) && !string.IsNullOrWhiteSpace(ss)) sb.AppendLine($"### Screenshots or error messages\n\n{ss}\n");
        if (f.TryGetValue("browser", out var br)) sb.AppendLine($"### Browser\n\n{br}\n");
        if (f.TryGetValue("severity", out var sev)) sb.AppendLine($"### Severity\n\n{sev}\n");
        return sb.ToString().TrimEnd();
    }

    private static string FormatFeatureBody(string area, Dictionary<string, string> f)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"### Area of the application\n\n{area}\n");
        if (f.TryGetValue("problem", out var prob)) sb.AppendLine($"### What problem does this solve?\n\n{prob}\n");
        if (f.TryGetValue("solution", out var sol)) sb.AppendLine($"### Describe the feature you'd like\n\n{sol}\n");
        if (f.TryGetValue("alternatives", out var alt) && !string.IsNullOrWhiteSpace(alt)) sb.AppendLine($"### Alternatives or workarounds\n\n{alt}\n");
        if (f.TryGetValue("priority", out var pri)) sb.AppendLine($"### How important is this to you?\n\n{pri}\n");
        if (f.TryGetValue("context", out var ctx) && !string.IsNullOrWhiteSpace(ctx)) sb.AppendLine($"### Additional context\n\n{ctx}\n");
        return sb.ToString().TrimEnd();
    }

    private static string FormatQuestionBody(string area, Dictionary<string, string> f)
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine($"### Area of the application\n\n{area}\n");
        if (f.TryGetValue("question", out var q)) sb.AppendLine($"### Question\n\n{q}\n");
        if (f.TryGetValue("context", out var ctx) && !string.IsNullOrWhiteSpace(ctx)) sb.AppendLine($"### Additional context\n\n{ctx}\n");
        return sb.ToString().TrimEnd();
    }

    private async Task<IssueRequestDto> LoadDto(Guid id, CancellationToken ct)
    {
        var entity = await _db.IssueRequests
            .Include(ir => ir.SubmittedByUser)
            .Include(ir => ir.ReviewedByUser)
            .FirstAsync(ir => ir.Id == id, ct);
        return MapToDto(entity);
    }

    private static IssueRequestDto MapToDto(IssueRequest ir) => new()
    {
        Id = ir.Id,
        Type = (int)ir.Type,
        Status = (int)ir.Status,
        Title = ir.Title,
        Area = ir.Area,
        BodyJson = ir.BodyJson,
        SubmittedByUserId = ir.SubmittedByUserId,
        SubmittedByName = ir.SubmittedByUser?.DisplayName ?? "",
        SubmittedAt = ir.SubmittedAt,
        ReviewedByUserId = ir.ReviewedByUserId,
        ReviewedByName = ir.ReviewedByUser?.DisplayName,
        ReviewedAt = ir.ReviewedAt,
        ReviewerNotes = ir.ReviewerNotes,
        GitHubIssueNumber = ir.GitHubIssueNumber,
        GitHubIssueUrl = ir.GitHubIssueUrl
    };
}
