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
    private const int DocumentFolderId = 31;

    private static NiceLabelService CreateService(HttpMessageHandler handler)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(new NiceLabelOptions
        {
            BaseUrl = BaseUrl,
            SubscriptionKey = SubscriptionKey,
            FilePath = FilePath,
            DocumentFolderId = DocumentFolderId
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
    public async Task GetPrinters_Success_ReturnsPrinterList()
    {
        var payload = """
                      [
                        { "PrinterName": "Zebra-A" },
                        { "PrinterName": "Zebra-B" }
                      ]
                      """;
        var handler = CreateMockHandler(HttpStatusCode.OK, payload);
        var sut = CreateService(handler.Object);

        var (success, printers, errorMessage) = await sut.GetPrintersAsync();

        Assert.True(success);
        Assert.Null(errorMessage);
        Assert.Equal(2, printers.Count);
        Assert.Contains("Zebra-A", printers);
        Assert.Contains("Zebra-B", printers);
    }

    [Fact]
    public async Task GetPrinters_Success_ParsesPrintQueueName()
    {
        var payload = """
                      {
                        "devices": [
                          { "printerName": null, "printQueueName": "WJO_Nameplates" }
                        ]
                      }
                      """;
        var handler = CreateMockHandler(HttpStatusCode.OK, payload);
        var sut = CreateService(handler.Object);

        var (success, printers, errorMessage) = await sut.GetPrintersAsync();

        Assert.True(success);
        Assert.Null(errorMessage);
        Assert.Contains("WJO_Nameplates", printers);
    }

    [Fact]
    public async Task GetPrinters_Success_SendsCorrectUrlAndHeaders()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("[]")
            });

        var sut = CreateService(handler.Object);
        await sut.GetPrintersAsync();

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal($"{BaseUrl}/Print/v2/Printers", capturedRequest.RequestUri!.ToString());
        Assert.Equal("no-cache", capturedRequest.Headers.GetValues("Cache-Control").First());
        Assert.Equal(SubscriptionKey, capturedRequest.Headers.GetValues("Ocp-Apim-Subscription-Key").First());
    }

    [Fact]
    public async Task GetPrinters_HttpError_ReturnsFalseWithMessage()
    {
        var handler = CreateMockHandler(HttpStatusCode.BadGateway, "Upstream unavailable");
        var sut = CreateService(handler.Object);

        var (success, printers, errorMessage) = await sut.GetPrintersAsync();

        Assert.False(success);
        Assert.Empty(printers);
        Assert.Contains("502", errorMessage);
    }

    [Fact]
    public async Task GetPrinters_Exception_ReturnsFalseWithMessage()
    {
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network unreachable"));

        var sut = CreateService(handler.Object);

        var (success, printers, errorMessage) = await sut.GetPrintersAsync();

        Assert.False(success);
        Assert.Empty(printers);
        Assert.Contains("Network unreachable", errorMessage);
    }

    [Fact]
    public async Task GetDocuments_Success_ReturnsFileItemsOnly()
    {
        var payload = """
                      {
                        "items": [
                          { "itemType": "Folder", "name": "SubFolder", "itemPath": "/Solutions/MES/SubFolder" },
                          { "itemType": "File", "name": "Label B", "itemPath": "/Solutions/MES/LabelB.nlbl" },
                          { "itemType": "File", "name": "Label A", "itemPath": "/Solutions/MES/LabelA.nlbl" },
                          { "itemType": "File", "name": "Dual Shell Label Solution", "itemPath": "/Solutions/MES/Dual Shell Label Solution.nsln" }
                        ]
                      }
                      """;
        var handler = CreateMockHandler(HttpStatusCode.OK, payload);
        var sut = CreateService(handler.Object);

        var (success, documents, errorMessage) = await sut.GetDocumentsAsync();

        Assert.True(success);
        Assert.Null(errorMessage);
        Assert.Equal(2, documents.Count);
        Assert.Equal("/Solutions/MES/LabelA.nlbl", documents[0].ItemPath);
        Assert.Equal("/Solutions/MES/LabelB.nlbl", documents[1].ItemPath);
        Assert.DoesNotContain(documents, d => d.ItemPath.EndsWith(".nsln", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetDocuments_Success_SendsCorrectUrlAndHeaders()
    {
        HttpRequestMessage? capturedRequest = null;
        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .Callback<HttpRequestMessage, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("""{"items": []}""")
            });

        var sut = CreateService(handler.Object);
        await sut.GetDocumentsAsync();

        Assert.NotNull(capturedRequest);
        Assert.Equal(HttpMethod.Get, capturedRequest!.Method);
        Assert.Equal($"{BaseUrl}/document/v2/list/{DocumentFolderId}", capturedRequest.RequestUri!.ToString());
        Assert.Equal("no-cache", capturedRequest.Headers.GetValues("Cache-Control").First());
        Assert.Equal(SubscriptionKey, capturedRequest.Headers.GetValues("Ocp-Apim-Subscription-Key").First());
    }

    [Fact]
    public async Task PrintNameplate_Success_ReturnsTrueAndNoError()
    {
        var handler = CreateMockHandler(HttpStatusCode.OK);
        var sut = CreateService(handler.Object);

        var (success, errorMessage) = await sut.PrintNameplateAsync(
            "TestPrinter", FilePath, 1, "02/21/2026 3:45 PM", "Propane", 500, "W00123456");

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
        await sut.PrintNameplateAsync("MyPrinter", FilePath, 1, "02/21/2026 3:45 PM", "Propane", 500, "W00123456");

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
        await sut.PrintNameplateAsync("Printer1", FilePath, 1, "02/21/2026 3:45 PM", "Propane", 500, "W00123456");

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
            "TestPrinter", FilePath, 1, "02/21/2026 3:45 PM", "Propane", 500, "W00123456");

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
            "TestPrinter", FilePath, 1, "02/21/2026 3:45 PM", "Propane", 500, "W00123456");

        Assert.False(success);
        Assert.Contains("Network unreachable", errorMessage);
    }
}
