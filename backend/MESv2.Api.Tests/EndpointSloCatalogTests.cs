using MESv2.Api.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;

namespace MESv2.Api.Tests;

public class EndpointSloCatalogTests
{
    [Fact]
    public void Resolve_LoginConfigRoute_ReturnsLoginMetadata()
    {
        var catalog = new EndpointSloCatalog();
        var context = CreateContext("GET", "/api/users/login-config");

        var metadata = catalog.Resolve(context);

        Assert.NotNull(metadata);
        Assert.Equal("Login", metadata!.EndpointCategory);
        Assert.Equal("LoginConfig", metadata.Feature);
    }

    [Fact]
    public void Resolve_UnknownRoute_ReturnsNull()
    {
        var catalog = new EndpointSloCatalog();
        var context = CreateContext("GET", "/api/unknown");

        var metadata = catalog.Resolve(context);

        Assert.Null(metadata);
    }

    private static DefaultHttpContext CreateContext(string method, string path)
    {
        var context = new DefaultHttpContext();
        context.Request.Method = method;
        context.Request.Path = path;

        var endpoint = new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse(path.TrimStart('/')),
            0,
            EndpointMetadataCollection.Empty,
            "test");
        context.SetEndpoint(endpoint);
        return context;
    }
}
