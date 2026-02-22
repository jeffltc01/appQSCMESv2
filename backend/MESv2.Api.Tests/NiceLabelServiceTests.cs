using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MESv2.Api.Services;
using Moq;
using Moq.Protected;

namespace MESv2.Api.Tests;

public class NiceLabelServiceTests
{
    private const string BaseUrl = "https://labelcloudapi.test.com";
    private const string SubscriptionKey = "test-key-123";
    private const string FilePath = "/Solutions/MES/DataPlateFoilLabel.nlbl";

    private static NiceLabelService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new NiceLabelOptions
        {
            BaseUrl = BaseUrl,
            SubscriptionKey = SubscriptionKey,
            FilePath = FilePath
        });
        return new NiceLabelService(httpClient, options, NullLogger<NiceLabelService>.Instance);
    }

    private static Mock<HttpMessageHandler> CreateMockHandler(HttpStatusCode statusCode, string body = "")
    {
        var mock = new Mock<HttpMessageHandler>();
        mock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(body)
            });
        return mock;
    }

    [Fact]
    public async Task PrintNameplate_Success_ReturnsTrueAndNoError()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK);
        var sut = CreateService(handler.Object);

        var (success, errorMessage) = await sut.PrintNameplateAsync(
            "TestPrinter", 1, "02/21/2026 3:45 PM", "Propane", 500, "W00123456");

        Assert.True(success);
        Assert.Null(errorMessage);
    }

    [Fact]
    public async Task PrintNameplate_Success_SendsCorrectUrl()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = CreateService(handler.Object);
        await sut.PrintNameplateAsync("MyPrinter", 1, "02/21/2026 3:45 PM", "Propane", 500, "W00123456");

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Put, capturedRequest!.Method);
        Assert.Equal($"{BaseUrl}/Print/v1/Print/MyPrinter", capturedRequest.RequestUri!.ToString());
        Assert.Equal(SubscriptionKey, capturedRequest.Headers.GetValues("Ocp-Apim-Subscription-Key").First());
    }

    [Fact]
    public async Task PrintNameplate_Success_SendsCorrectBody()
    {
        string? capturedBody = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>(async (req, _) =>
            {
                capturedBody = await req.Content!.ReadAsStringAsync();
            })
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK));

        var sut = CreateService(handler.Object);
        await sut.PrintNameplateAsync("Printer1", 1, "02/21/2026 3:45 PM", "Propane", 500, "W00123456");

        Assert.NotNull(capturedBody);
        Assert.Contains("\"FilePath\":\"/Solutions/MES/DataPlateFoilLabel.nlbl\"", capturedBody);
        Assert.Contains("\"Quantity\":1", capturedBody);
        Assert.Contains("\"TankType\":\"Propane\"", capturedBody);
        Assert.Contains("\"TankSize\":\"500\"", capturedBody);
        Assert.Contains("\"SerialNo\":\"W00123456\"", capturedBody);
        Assert.Contains("\"PrintedOnText\":\"02/21/2026 3:45 PM\"", capturedBody);
    }

    [Fact]
    public async Task PrintNameplate_HttpError_ReturnsFalseWithMessage()
    {
        var handler = CreateMockHandler(HttpStatusCode.InternalServerError, "Printer offline");
        var sut = CreateService(handler.Object);

        var (success, errorMessage) = await sut.PrintNameplateAsync(
            "TestPrinter", 1, "02/21/2026 3:45 PM", "Propane", 500, "W00123456");

        Assert.False(success);
        Assert.Contains("500", errorMessage);
        Assert.Contains("Printer offline", errorMessage);
    }

    [Fact]
    public async Task PrintNameplate_Exception_ReturnsFalseWithMessage()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network unreachable"));

        var sut = CreateService(handler.Object);

        var (success, errorMessage) = await sut.PrintNameplateAsync(
            "TestPrinter", 1, "02/21/2026 3:45 PM", "Propane", 500, "W00123456");

        Assert.False(success);
        Assert.Contains("Network unreachable", errorMessage);
    }
}
