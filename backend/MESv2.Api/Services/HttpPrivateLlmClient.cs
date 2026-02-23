using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;

namespace MESv2.Api.Services;

public class HttpPrivateLlmClient : INlqModelClient
{
    private readonly HttpClient _httpClient;
    private readonly NaturalLanguageQueryOptions _options;
    private readonly ILogger<HttpPrivateLlmClient> _logger;

    public HttpPrivateLlmClient(
        HttpClient httpClient,
        IOptions<NaturalLanguageQueryOptions> options,
        ILogger<HttpPrivateLlmClient> logger)
    {
        _httpClient = httpClient;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<NlqModelInterpretation> InterpretAsync(
        string redactedQuestion,
        string? view,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.HttpModel.BaseUrl))
            throw new InvalidOperationException("NaturalLanguageQuery:HttpModel:BaseUrl must be configured for HTTP provider.");

        var url = $"{_options.HttpModel.BaseUrl.TrimEnd('/')}{_options.HttpModel.InterpretPath}";
        var req = new
        {
            question = redactedQuestion,
            view,
            allowedIntents = Enum.GetNames<NlqIntent>().Where(n => n != nameof(NlqIntent.Unknown)).ToArray(),
        };

        using var message = new HttpRequestMessage(HttpMethod.Post, url)
        {
            Content = new StringContent(JsonSerializer.Serialize(req), Encoding.UTF8, "application/json"),
        };

        if (!string.IsNullOrWhiteSpace(_options.HttpModel.ApiKey))
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.HttpModel.ApiKey);

        using var response = await _httpClient.SendAsync(message, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Private LLM endpoint returned {StatusCode}. Falling back to mock parser.", response.StatusCode);
            return await new MockPrivateLlmClient().InterpretAsync(redactedQuestion, view, cancellationToken);
        }

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var parsed = JsonSerializer.Deserialize<HttpInterpretResponse>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

        if (parsed == null || string.IsNullOrWhiteSpace(parsed.Intent))
            return await new MockPrivateLlmClient().InterpretAsync(redactedQuestion, view, cancellationToken);

        if (!Enum.TryParse<NlqIntent>(parsed.Intent, ignoreCase: true, out var intent))
            intent = NlqIntent.Unknown;

        return new NlqModelInterpretation
        {
            Intent = intent,
            Confidence = Math.Clamp(parsed.Confidence ?? 0.5m, 0m, 1m),
            ScopeHint = string.IsNullOrWhiteSpace(parsed.ScopeHint) ? "context" : parsed.ScopeHint!,
            FollowUps = parsed.FollowUps?.Where(x => !string.IsNullOrWhiteSpace(x)).Take(5).ToList() ?? [],
        };
    }

    private sealed class HttpInterpretResponse
    {
        public string? Intent { get; set; }
        public decimal? Confidence { get; set; }
        public string? ScopeHint { get; set; }
        public List<string>? FollowUps { get; set; }
    }
}
