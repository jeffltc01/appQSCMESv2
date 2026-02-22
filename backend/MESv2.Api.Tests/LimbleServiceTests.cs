using System.Net;
using System.Text.Json;
using MESv2.Api.DTOs;
using MESv2.Api.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;

namespace MESv2.Api.Tests;

public class LimbleServiceTests
{
    private static readonly LimbleOptions TestOptions = new()
    {
        BaseUrl = "https://api.limblecmms.com:443/v2",
        ApiKey = "dGVzdDp0ZXN0"
    };

    private static LimbleService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        return new LimbleService(httpClient, Options.Create(TestOptions), NullLogger<LimbleService>.Instance);
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(HttpStatusCode statusCode, string responseBody)
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseBody, System.Text.Encoding.UTF8, "application/json")
            });
        return mock;
    }

    [Fact]
    public async Task GetStatuses_ReturnsListOfStatuses()
    {
        var json = JsonSerializer.Serialize(new[]
        {
            new { id = 1, name = "Open" },
            new { id = 2, name = "In Progress" },
            new { id = 3, name = "Closed" }
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var sut = CreateService(handler.Object);

        var result = await sut.GetStatusesAsync();

        Assert.Equal(3, result.Count);
        Assert.Equal("Open", result[0].Name);
        Assert.Equal(2, result[1].Id);
    }

    [Fact]
    public async Task GetStatuses_ThrowsOnNonSuccessStatus()
    {
        var handler = CreateMockHandler(HttpStatusCode.Unauthorized, "{\"error\":\"unauthorized\"}");
        var sut = CreateService(handler.Object);

        await Assert.ThrowsAsync<HttpRequestException>(() => sut.GetStatusesAsync());
    }

    [Fact]
    public async Task GetMyRequests_ReturnsTasksFilteredByMeta1()
    {
        var tasks = new[]
        {
            new { id = 100, name = "Fix valve", description = "Leaking valve", priority = 3, statusId = 1, due = (long?)1700000000L, created = (long?)1699000000L, meta1 = "EMP001" },
            new { id = 101, name = "Replace belt", description = "", priority = 1, statusId = 2, due = (long?)null, created = (long?)1699100000L, meta1 = "EMP001" }
        };
        var json = JsonSerializer.Serialize(tasks);

        var handler = CreateMockHandler(HttpStatusCode.OK, json);
        var sut = CreateService(handler.Object);

        var result = await sut.GetMyRequestsAsync("EMP001");

        Assert.Equal(2, result.Count);
        Assert.Equal("Fix valve", result[0].Name);
        Assert.Equal(3, result[0].Priority);
        Assert.Equal("EMP001", result[0].Meta1);
        Assert.Equal("Replace belt", result[1].Name);
    }

    [Fact]
    public async Task GetMyRequests_SendsCorrectUrl()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK, "[]");
        var sut = CreateService(handler.Object);

        await sut.GetMyRequestsAsync("EMP001");

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Get &&
                r.RequestUri!.ToString().Contains("/tasks/?meta1=EMP001")),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CreateWorkRequest_ReturnsCreatedTask()
    {
        var responseJson = JsonSerializer.Serialize(new
        {
            id = 200,
            name = "New pump needed",
            description = "Pump failing",
            priority = 2,
            statusId = 1,
            due = 1700000000L,
            created = 1699500000L,
            meta1 = "EMP002"
        });

        var handler = CreateMockHandler(HttpStatusCode.OK, responseJson);
        var sut = CreateService(handler.Object);

        var result = await sut.CreateWorkRequestAsync(new CreateLimbleWorkRequestDto
        {
            Subject = "New pump needed",
            Description = "Pump failing",
            Priority = 2,
            RequestedDueDate = 1700000000,
            LocationId = "12345",
            EmployeeNo = "EMP002",
            DisplayName = "John Smith"
        });

        Assert.Equal(200, result.Id);
        Assert.Equal("New pump needed", result.Name);
        Assert.Equal(2, result.Priority);
        Assert.Equal("EMP002", result.Meta1);
    }

    [Fact]
    public async Task CreateWorkRequest_SendsCorrectPayload()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK, JsonSerializer.Serialize(new { id = 1, name = "Test" }));
        var sut = CreateService(handler.Object);

        await sut.CreateWorkRequestAsync(new CreateLimbleWorkRequestDto
        {
            Subject = "Test Subject",
            Description = "Test Desc",
            Priority = 3,
            RequestedDueDate = 1700000000,
            LocationId = "99",
            EmployeeNo = "EMP003",
            DisplayName = "Jane Doe"
        });

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Method == HttpMethod.Post &&
                r.RequestUri!.ToString().Contains("/tasks/") &&
                r.Headers.Authorization != null &&
                r.Headers.Authorization.Scheme == "Basic"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task CreateWorkRequest_ThrowsOnApiError()
    {
        var handler = CreateMockHandler(HttpStatusCode.BadRequest, "{\"error\":\"bad request\"}");
        var sut = CreateService(handler.Object);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            sut.CreateWorkRequestAsync(new CreateLimbleWorkRequestDto
            {
                Subject = "Test",
                Description = "Desc",
                Priority = 1,
                LocationId = "1",
                EmployeeNo = "EMP001",
                DisplayName = "Test"
            }));
    }

    [Fact]
    public async Task GetStatuses_SetsAuthorizationHeader()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK, "[]");
        var sut = CreateService(handler.Object);

        await sut.GetStatusesAsync();

        handler.Protected().Verify("SendAsync", Times.Once(),
            ItExpr.Is<HttpRequestMessage>(r =>
                r.Headers.Authorization != null &&
                r.Headers.Authorization.Scheme == "Basic" &&
                r.Headers.Authorization.Parameter == TestOptions.ApiKey),
            ItExpr.IsAny<CancellationToken>());
    }
}
