namespace MESv2.Api.Services;

public interface ICoverageReportService
{
    bool IsConfigured { get; }
    Task<string?> GetSummaryJsonAsync(CancellationToken ct = default);
    Task<(Stream Content, string ContentType)?> GetReportFileAsync(string layer, string path, CancellationToken ct = default);
}
