using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.DataContracts;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.AspNetCore.Http;

namespace MESv2.Api.Observability;

public sealed class SloTelemetryInitializer : ITelemetryInitializer
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public SloTelemetryInitializer(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public void Initialize(ITelemetry telemetry)
    {
        if (telemetry is not RequestTelemetry requestTelemetry)
        {
            return;
        }

        var context = _httpContextAccessor.HttpContext;
        if (context == null)
        {
            return;
        }

        AddPropertyIfPresent(requestTelemetry, context, "SLO.EndpointCategory", "EndpointCategory");
        AddPropertyIfPresent(requestTelemetry, context, "SLO.Feature", "Feature");
        AddPropertyIfPresent(requestTelemetry, context, "SLO.Scope", "Scope");
        AddPropertyIfPresent(requestTelemetry, context, "SLO.IsOperatorCritical", "IsOperatorCritical");
        AddPropertyIfPresent(requestTelemetry, context, "SLO.PlantId", "PlantId");
        AddPropertyIfPresent(requestTelemetry, context, "SLO.WorkCenterId", "WorkCenterId");
    }

    private static void AddPropertyIfPresent(
        RequestTelemetry requestTelemetry,
        HttpContext context,
        string itemKey,
        string telemetryPropertyName)
    {
        if (!context.Items.TryGetValue(itemKey, out var value) || value == null)
        {
            return;
        }

        requestTelemetry.Properties[telemetryPropertyName] = value.ToString() ?? string.Empty;
    }
}
