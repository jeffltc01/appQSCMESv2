using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using MESv2.Api.Data;
using MESv2.Api.DTOs;
using MESv2.Api.Models;

namespace MESv2.Api.Services;

public class NaturalLanguageQueryService : INaturalLanguageQueryService
{
    private const string EntityName = "NaturalLanguageQuery";
    private readonly NaturalLanguageQueryOptions _options;
    private readonly INlqModelClient _modelClient;
    private readonly INlqToolExecutor _toolExecutor;
    private readonly IMemoryCache _cache;
    private readonly MesDbContext _db;
    private readonly ILogger<NaturalLanguageQueryService> _logger;

    public NaturalLanguageQueryService(
        IOptions<NaturalLanguageQueryOptions> options,
        INlqModelClient modelClient,
        INlqToolExecutor toolExecutor,
        IMemoryCache cache,
        MesDbContext db,
        ILogger<NaturalLanguageQueryService> logger)
    {
        _options = options.Value;
        _modelClient = modelClient;
        _toolExecutor = toolExecutor;
        _cache = cache;
        _db = db;
        _logger = logger;
    }

    public async Task<NaturalLanguageQueryResponseDto> AskAsync(
        Guid callerUserId,
        decimal callerRoleTier,
        Guid callerSiteId,
        NaturalLanguageQueryRequestDto request,
        CancellationToken cancellationToken = default)
    {
        var watch = Stopwatch.StartNew();

        if (!_options.Enabled)
            throw new InvalidOperationException("Natural language query feature is disabled.");

        if (callerRoleTier > 5.0m)
            throw new UnauthorizedAccessException("Only Team Lead and above can query analytics.");

        var cleanQuestion = (request.Question ?? string.Empty).Trim();
        if (cleanQuestion.Length == 0)
            throw new ArgumentException("Question is required.");
        if (cleanQuestion.Length > _options.MaxQuestionLength)
            cleanQuestion = cleanQuestion[.._options.MaxQuestionLength];

        var resolvedPlantId = request.Context?.PlantId ?? callerSiteId;
        if (resolvedPlantId != callerSiteId && callerRoleTier > 2.0m)
            throw new UnauthorizedAccessException("Cross-plant analytics are restricted to director and admin roles.");

        var redactedQuestion = RedactPrompt(cleanQuestion);
        var cacheKey = BuildCacheKey(resolvedPlantId, request.Context?.WorkCenterId, request.Context?.Date, redactedQuestion);

        var usedCache = _cache.TryGetValue(cacheKey, out NlqModelInterpretation? interpretation);
        if (!usedCache || interpretation == null)
        {
            interpretation = await _modelClient.InterpretAsync(redactedQuestion, request.Context?.View, cancellationToken);
            _cache.Set(cacheKey, interpretation, TimeSpan.FromSeconds(Math.Max(1, _options.IntentCacheSeconds)));
        }

        var intent = ApplyIntentFeatureFlags(interpretation.Intent);
        var response = await _toolExecutor.ExecuteAsync(intent, request, resolvedPlantId, cancellationToken);
        response.Confidence = Math.Clamp((response.Confidence + interpretation.Confidence) / 2m, 0m, 1m);
        response.FollowUps = (interpretation.FollowUps ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Take(_options.MaxFollowUps)
            .ToList();
        response.DataPoints = response.DataPoints.Take(_options.MaxDataPoints).ToList();
        response.AnswerText = Truncate(response.AnswerText, _options.MaxAnswerChars);
        response.Trace = new NaturalLanguageQueryTraceDto
        {
            Intent = intent.ToString(),
            UsedModel = true,
            UsedCache = usedCache,
            DurationMs = watch.ElapsedMilliseconds,
        };

        await WriteAuditLogAsync(
            callerUserId,
            redactedQuestion,
            intent,
            resolvedPlantId,
            watch.ElapsedMilliseconds,
            usedCache,
            cancellationToken);

        _logger.LogInformation(
            "NLQ handled. user={UserId} site={SiteId} intent={Intent} durationMs={DurationMs} usedCache={UsedCache}",
            callerUserId, resolvedPlantId, intent, watch.ElapsedMilliseconds, usedCache);

        return response;
    }

    private NlqIntent ApplyIntentFeatureFlags(NlqIntent intent)
    {
        if (_options.DisabledIntents.Any(x => string.Equals(x, intent.ToString(), StringComparison.OrdinalIgnoreCase)))
            return NlqIntent.Unknown;

        if (!_options.EnablePlantWideQueries &&
            (intent == NlqIntent.WorkCentersBehindTargetToday || intent == NlqIntent.TopDowntimeDriversToday))
            return NlqIntent.Unknown;

        return intent;
    }

    private async Task WriteAuditLogAsync(
        Guid callerUserId,
        string redactedQuestion,
        NlqIntent intent,
        Guid plantId,
        long durationMs,
        bool usedCache,
        CancellationToken cancellationToken)
    {
        var questionHash = ComputeSha256(redactedQuestion);
        var audit = new AuditLog
        {
            Action = "NLQ_ASK",
            EntityName = EntityName,
            EntityId = Guid.NewGuid(),
            ChangedByUserId = callerUserId,
            ChangedAtUtc = DateTime.UtcNow,
            Changes = $"intent={intent};site={plantId};durationMs={durationMs};usedCache={usedCache};questionHash={questionHash}",
        };
        _db.AuditLogs.Add(audit);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static string RedactPrompt(string question)
    {
        // Keep model inputs constrained to intent text, not full operational payloads.
        return question.Replace("\r", " ").Replace("\n", " ").Trim();
    }

    private static string BuildCacheKey(Guid plantId, Guid? wcId, string? date, string question)
    {
        return $"nlq:intent:{plantId:N}:{wcId?.ToString("N") ?? "none"}:{date ?? "today"}:{ComputeSha256(question)}";
    }

    private static string ComputeSha256(string value)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
        return Convert.ToHexString(bytes);
    }

    private static string Truncate(string value, int maxChars)
    {
        if (string.IsNullOrWhiteSpace(value))
            return value;
        if (value.Length <= maxChars)
            return value;
        return value[..maxChars];
    }
}
