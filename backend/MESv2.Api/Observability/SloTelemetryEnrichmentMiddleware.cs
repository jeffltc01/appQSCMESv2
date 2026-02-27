using Microsoft.AspNetCore.Http;

namespace MESv2.Api.Observability;

public sealed class SloTelemetryEnrichmentMiddleware
{
    private readonly RequestDelegate _next;
    private readonly EndpointSloCatalog _catalog;

    public SloTelemetryEnrichmentMiddleware(RequestDelegate next, EndpointSloCatalog catalog)
    {
        _next = next;
        _catalog = catalog;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var metadata = _catalog.Resolve(context);
        if (metadata != null)
        {
            context.Items["SLO.EndpointCategory"] = metadata.EndpointCategory;
            context.Items["SLO.Feature"] = metadata.Feature;
            context.Items["SLO.Scope"] = metadata.Scope;
            context.Items["SLO.IsOperatorCritical"] = metadata.IsOperatorCritical ? "true" : "false";
        }

        if (TryGetPlantId(context, out var plantId))
        {
            context.Items["SLO.PlantId"] = plantId.ToString();
        }

        if (TryGetWorkCenterId(context, out var workCenterId))
        {
            context.Items["SLO.WorkCenterId"] = workCenterId.ToString();
        }

        await _next(context);
    }

    private static bool TryGetPlantId(HttpContext context, out Guid plantId)
    {
        if (TryGetGuidFromRoute(context, "plantId", out plantId))
        {
            return true;
        }

        if (TryGetGuidFromQuery(context, "plantId", out plantId))
        {
            return true;
        }

        if (TryGetGuidFromQuery(context, "siteId", out plantId))
        {
            return true;
        }

        plantId = Guid.Empty;
        return false;
    }

    private static bool TryGetWorkCenterId(HttpContext context, out Guid workCenterId)
    {
        if (TryGetGuidFromRoute(context, "id", out workCenterId))
        {
            return true;
        }

        if (TryGetGuidFromRoute(context, "wcId", out workCenterId))
        {
            return true;
        }

        workCenterId = Guid.Empty;
        return false;
    }

    private static bool TryGetGuidFromRoute(HttpContext context, string key, out Guid value)
    {
        value = Guid.Empty;
        return context.Request.RouteValues.TryGetValue(key, out var routeValue)
               && routeValue is not null
               && Guid.TryParse(routeValue.ToString(), out value);
    }

    private static bool TryGetGuidFromQuery(HttpContext context, string key, out Guid value)
    {
        value = Guid.Empty;
        return context.Request.Query.TryGetValue(key, out var queryValue)
               && queryValue.Count > 0
               && Guid.TryParse(queryValue[0], out value);
    }
}
