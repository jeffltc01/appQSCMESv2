using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace MESv2.Api.Services;

public class GitHubService : IGitHubService
{
    private readonly HttpClient _http;
    private readonly GitHubOptions _options;
    private readonly ILogger<GitHubService> _logger;

    private static readonly HashSet<string> PlaceholderValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "SET_VIA_AZURE_APP_SETTINGS", "YOUR_TOKEN_HERE"
    };

    public bool IsConfigured => !string.IsNullOrWhiteSpace(_options.Token)
                                && !PlaceholderValues.Contains(_options.Token.Trim());

    public GitHubService(HttpClient http, IOptions<GitHubOptions> options, ILogger<GitHubService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        _http.BaseAddress = new Uri("https://api.github.com");
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("MESv2", "1.0"));
        if (IsConfigured)
        {
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _options.Token);
        }
    }

    public async Task<(int IssueNumber, string IssueUrl)> CreateIssueAsync(
        string title, string markdownBody, IEnumerable<string> labels, CancellationToken ct = default)
    {
        var url = $"/repos/{Uri.EscapeDataString(_options.Owner)}/{Uri.EscapeDataString(_options.Repo)}/issues";

        var payload = new CreateIssuePayload
        {
            Title = title,
            Body = markdownBody,
            Labels = labels.ToList()
        };

        var response = await _http.PostAsJsonAsync(url, payload, ct);

        if (!response.IsSuccessStatusCode)
        {
            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogError("GitHub API error creating issue: {StatusCode} {Body}",
                (int)response.StatusCode, errorBody);
            throw new InvalidOperationException($"GitHub API returned {(int)response.StatusCode}: {errorBody}");
        }

        var result = await response.Content.ReadFromJsonAsync<GitHubIssueResponse>(ct)
            ?? throw new InvalidOperationException("GitHub API returned null response");

        _logger.LogInformation("Created GitHub issue #{Number}: {Url}", result.Number, result.HtmlUrl);

        return (result.Number, result.HtmlUrl);
    }

    private class CreateIssuePayload
    {
        [JsonPropertyName("title")]
        public string Title { get; set; } = string.Empty;

        [JsonPropertyName("body")]
        public string Body { get; set; } = string.Empty;

        [JsonPropertyName("labels")]
        public List<string> Labels { get; set; } = [];
    }

    private class GitHubIssueResponse
    {
        [JsonPropertyName("number")]
        public int Number { get; set; }

        [JsonPropertyName("html_url")]
        public string HtmlUrl { get; set; } = string.Empty;
    }
}
