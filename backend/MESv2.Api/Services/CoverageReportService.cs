using Azure;
using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;

namespace MESv2.Api.Services;

public class CoverageReportService : ICoverageReportService
{
    private readonly BlobContainerClient? _container;
    private readonly ILogger<CoverageReportService> _logger;

    private static readonly HashSet<string> PlaceholderValues = new(StringComparer.OrdinalIgnoreCase)
    {
        "", "SET_VIA_AZURE_APP_SETTINGS"
    };

    private static readonly Dictionary<string, string> ContentTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".html"] = "text/html",
        [".htm"] = "text/html",
        [".css"] = "text/css",
        [".js"] = "application/javascript",
        [".json"] = "application/json",
        [".svg"] = "image/svg+xml",
        [".png"] = "image/png",
        [".gif"] = "image/gif",
        [".ico"] = "image/x-icon",
    };

    public bool IsConfigured => _container is not null;

    public CoverageReportService(IOptions<CoverageReportOptions> options, ILogger<CoverageReportService> logger)
    {
        _logger = logger;

        var connStr = options.Value.StorageConnectionString;
        if (!string.IsNullOrWhiteSpace(connStr) && !PlaceholderValues.Contains(connStr.Trim()))
        {
            _container = new BlobContainerClient(connStr, options.Value.ContainerName);
        }
    }

    public async Task<string?> GetSummaryJsonAsync(CancellationToken ct = default)
    {
        if (_container is null) return null;

        try
        {
            var blob = _container.GetBlobClient("summary.json");
            var response = await blob.DownloadContentAsync(ct);
            return response.Value.Content.ToString();
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning("Coverage summary.json not found in blob storage");
            return null;
        }
    }

    public async Task<(Stream Content, string ContentType)?> GetReportFileAsync(
        string layer, string path, CancellationToken ct = default)
    {
        if (_container is null) return null;

        var validLayers = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "frontend", "backend" };
        if (!validLayers.Contains(layer)) return null;

        if (string.IsNullOrWhiteSpace(path)) path = "index.html";

        var blobPath = $"{layer}/{path}";

        try
        {
            var blob = _container.GetBlobClient(blobPath);
            var download = await blob.DownloadStreamingAsync(cancellationToken: ct);

            var ext = Path.GetExtension(path);
            var contentType = ContentTypes.GetValueOrDefault(ext, "application/octet-stream");

            return (download.Value.Content, contentType);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            return null;
        }
    }
}
