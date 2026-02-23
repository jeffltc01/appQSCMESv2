using MESv2.Api.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace MESv2.Api.Tests;

public class CoverageReportServiceTests
{
    private static CoverageReportService CreateService(string connectionString = "")
    {
        var options = Options.Create(new CoverageReportOptions
        {
            StorageConnectionString = connectionString,
            ContainerName = "coverage-reports",
        });
        var logger = new Mock<ILogger<CoverageReportService>>();
        return new CoverageReportService(options, logger.Object);
    }

    [Theory]
    [InlineData("")]
    [InlineData("SET_VIA_AZURE_APP_SETTINGS")]
    [InlineData("  ")]
    public void IsConfigured_ReturnsFalse_WhenConnectionStringIsPlaceholder(string connStr)
    {
        var service = CreateService(connStr);
        Assert.False(service.IsConfigured);
    }

    [Fact]
    public async Task GetSummaryJsonAsync_ReturnsNull_WhenNotConfigured()
    {
        var service = CreateService();
        var result = await service.GetSummaryJsonAsync();
        Assert.Null(result);
    }

    [Fact]
    public async Task GetReportFileAsync_ReturnsNull_WhenNotConfigured()
    {
        var service = CreateService();
        var result = await service.GetReportFileAsync("backend", "index.html");
        Assert.Null(result);
    }

    [Fact]
    public async Task GetReportFileAsync_ReturnsNull_ForInvalidLayer()
    {
        var service = CreateService();
        var result = await service.GetReportFileAsync("invalid-layer", "index.html");
        Assert.Null(result);
    }
}
