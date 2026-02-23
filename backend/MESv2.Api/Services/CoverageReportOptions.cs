namespace MESv2.Api.Services;

public class CoverageReportOptions
{
    public string StorageConnectionString { get; set; } = string.Empty;
    public string ContainerName { get; set; } = "coverage-reports";
}
