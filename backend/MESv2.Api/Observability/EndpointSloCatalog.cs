using Microsoft.AspNetCore.Http;

namespace MESv2.Api.Observability;

public sealed class EndpointSloCatalog
{
    private readonly Dictionary<string, EndpointSloMetadata> _definitions;

    public EndpointSloCatalog()
    {
        _definitions = new Dictionary<string, EndpointSloMetadata>(StringComparer.OrdinalIgnoreCase)
        {
            ["GET:api/users/login-config"] = new("Login", "LoginConfig", "login", false),
            ["POST:api/auth/login"] = new("Login", "LoginSubmit", "login", false),
            ["POST:api/production-records"] = new("OperatorCriticalWrite", "CreateProductionRecord", "operator-workflow", true),
            ["POST:api/nameplate"] = new("OperatorCriticalWrite", "CreateNameplateRecord", "operator-workflow", true),
            ["POST:api/workcenters/{id:guid}/material-queue"] = new("OperatorCriticalWrite", "AddMaterialQueueItem", "operator-workflow", true),
            ["POST:api/workcenters/{id:guid}/fitup-queue"] = new("OperatorCriticalWrite", "AddFitupQueueItem", "operator-workflow", true),
            ["POST:api/workcenters/{id:guid}/xray-queue"] = new("OperatorCriticalWrite", "AddXrayQueueItem", "operator-workflow", true),
            ["POST:api/workcenters/{id:guid}/queue/advance"] = new("OperatorCriticalWrite", "AdvanceQueue", "operator-workflow", true),
            ["GET:api/workcenters/{id:guid}/history"] = new("WorkCenterRead", "GetWorkCenterHistory", "operator-workflow", true),
            ["GET:api/workcenters/{id:guid}/material-queue"] = new("WorkCenterRead", "GetMaterialQueue", "operator-workflow", true),
            ["GET:api/workcenters/{id:guid}/queue-transactions"] = new("WorkCenterRead", "GetQueueTransactions", "operator-workflow", true),
            ["GET:api/workcenters/{id:guid}/xray-queue"] = new("WorkCenterRead", "GetXrayQueue", "operator-workflow", true),
            ["GET:api/workcenters/{id:guid}/defect-codes"] = new("WorkCenterRead", "GetDefectCodes", "operator-workflow", true),
            ["GET:api/workcenters/{id:guid}/defect-locations"] = new("WorkCenterRead", "GetDefectLocations", "operator-workflow", true),
            ["GET:api/supervisor-dashboard/{wcId:guid}/metrics"] = new("SupervisorDashboardRead", "GetSupervisorMetrics", "supervisor-dashboard", false),
            ["GET:api/supervisor-dashboard/{wcId:guid}/trends"] = new("SupervisorDashboardRead", "GetSupervisorTrends", "supervisor-dashboard", false),
            ["GET:api/supervisor-dashboard/{wcId:guid}/records"] = new("SupervisorDashboardRead", "GetSupervisorRecords", "supervisor-dashboard", false),
            ["GET:api/supervisor-dashboard/{wcId:guid}/performance-table"] = new("SupervisorDashboardRead", "GetSupervisorPerformanceTable", "supervisor-dashboard", false)
        };
    }

    public EndpointSloMetadata? Resolve(HttpContext httpContext)
    {
        var endpoint = httpContext.GetEndpoint();
        var routeEndpoint = endpoint as Microsoft.AspNetCore.Routing.RouteEndpoint;
        var rawPattern = routeEndpoint?.RoutePattern.RawText?.TrimStart('/');
        if (string.IsNullOrEmpty(rawPattern))
        {
            rawPattern = httpContext.Request.Path.Value?.TrimStart('/');
        }

        if (string.IsNullOrEmpty(rawPattern))
        {
            return null;
        }

        var key = $"{httpContext.Request.Method}:{rawPattern}";
        return _definitions.TryGetValue(key, out var metadata) ? metadata : null;
    }
}

public sealed record EndpointSloMetadata(
    string EndpointCategory,
    string Feature,
    string Scope,
    bool IsOperatorCritical);
