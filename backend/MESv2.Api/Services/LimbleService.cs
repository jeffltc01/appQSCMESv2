using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using MESv2.Api.DTOs;
using Microsoft.Extensions.Options;

namespace MESv2.Api.Services;

public class LimbleService : ILimbleService
{
    private readonly HttpClient _http;
    private readonly LimbleOptions _options;
    private readonly ILogger<LimbleService> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    public LimbleService(HttpClient http, IOptions<LimbleOptions> options, ILogger<LimbleService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<List<LimbleStatusDto>> GetStatusesAsync(CancellationToken cancellationToken = default)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/statuses/";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyAuth(request);

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var raw = JsonSerializer.Deserialize<List<LimbleApiStatus>>(body, JsonOptions) ?? [];

        return raw.Select(s => new LimbleStatusDto { Id = s.Id, Name = s.Name ?? $"Status {s.Id}" }).ToList();
    }

    public async Task<List<LimbleTaskDto>> GetMyRequestsAsync(string employeeNumber, CancellationToken cancellationToken = default)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/tasks/?meta1={Uri.EscapeDataString(employeeNumber)}";
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
        ApplyAuth(request);

        var response = await _http.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();

        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        var raw = JsonSerializer.Deserialize<List<LimbleApiTask>>(body, JsonOptions) ?? [];

        return raw.Select(t => new LimbleTaskDto
        {
            Id = t.Id,
            Name = t.Name ?? "",
            Description = t.Description,
            Priority = t.Priority,
            StatusId = t.StatusId,
            DueDate = t.Due,
            CreatedDate = t.Created,
            Meta1 = t.Meta1
        }).ToList();
    }

    public async Task<LimbleTaskDto> CreateWorkRequestAsync(CreateLimbleWorkRequestDto dto, CancellationToken cancellationToken = default)
    {
        var url = $"{_options.BaseUrl.TrimEnd('/')}/tasks/";
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        ApplyAuth(request);

        var payload = new LimbleCreateTaskPayload
        {
            Name = dto.Subject,
            LocationID = dto.LocationId,
            Due = dto.RequestedDueDate,
            Type = 6,
            Priority = dto.Priority,
            Description = dto.Description,
            RequestName = dto.DisplayName,
            RequestDescription = dto.Description,
            Meta1 = dto.EmployeeNo
        };

        request.Content = JsonContent.Create(payload);

        _logger.LogInformation("Creating Limble work request for employee {EmpNo}: {Subject}", dto.EmployeeNo, dto.Subject);

        var response = await _http.SendAsync(request, cancellationToken);
        var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Limble create task failed: {StatusCode} {Body}", (int)response.StatusCode, responseBody);
            throw new InvalidOperationException($"Limble API returned {(int)response.StatusCode}: {responseBody}");
        }

        var created = JsonSerializer.Deserialize<LimbleApiTask>(responseBody, JsonOptions);

        return new LimbleTaskDto
        {
            Id = created?.Id ?? 0,
            Name = created?.Name ?? dto.Subject,
            Description = created?.Description ?? dto.Description,
            Priority = created?.Priority ?? dto.Priority,
            StatusId = created?.StatusId,
            DueDate = created?.Due ?? dto.RequestedDueDate,
            CreatedDate = created?.Created,
            Meta1 = created?.Meta1 ?? dto.EmployeeNo
        };
    }

    private void ApplyAuth(HttpRequestMessage request)
    {
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _options.ApiKey);
    }

    private class LimbleApiStatus
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }
    }

    private class LimbleApiTask
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("priority")]
        public int? Priority { get; set; }

        [JsonPropertyName("statusId")]
        public int? StatusId { get; set; }

        [JsonPropertyName("due")]
        public long? Due { get; set; }

        [JsonPropertyName("created")]
        public long? Created { get; set; }

        [JsonPropertyName("meta1")]
        public string? Meta1 { get; set; }
    }

    private class LimbleCreateTaskPayload
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("locationID")]
        public string LocationID { get; set; } = string.Empty;

        [JsonPropertyName("due")]
        public long? Due { get; set; }

        [JsonPropertyName("type")]
        public int Type { get; set; }

        [JsonPropertyName("priority")]
        public int Priority { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        [JsonPropertyName("requestName")]
        public string RequestName { get; set; } = string.Empty;

        [JsonPropertyName("requestDescription")]
        public string RequestDescription { get; set; } = string.Empty;

        [JsonPropertyName("meta1")]
        public string Meta1 { get; set; } = string.Empty;
    }
}
