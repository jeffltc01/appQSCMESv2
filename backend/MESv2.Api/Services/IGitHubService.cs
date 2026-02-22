namespace MESv2.Api.Services;

public interface IGitHubService
{
    bool IsConfigured { get; }

    Task<(int IssueNumber, string IssueUrl)> CreateIssueAsync(
        string title, string markdownBody, IEnumerable<string> labels, CancellationToken ct = default);
}
