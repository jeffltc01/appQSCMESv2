using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace MESv2.Api.Services;

public class NiceLabelService : INiceLabelService
{
    private readonly HttpClient _http;
    private readonly NiceLabelOptions _options;
    private readonly ILogger<NiceLabelService> _logger;

    public NiceLabelService(HttpClient http, IOptions<NiceLabelOptions> options, ILogger<NiceLabelService> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<(bool Success, List<string> Printers, string? ErrorMessage)> GetPrintersAsync()
    {
        try
        {
            var url = $"{_options.BaseUrl.TrimEnd('/')}/Print/v2/Printers";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _options.SubscriptionKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("NiceLabel printers fetch failed: {StatusCode} {Body}", (int)response.StatusCode, errorBody);
                return (false, [], $"HTTP {(int)response.StatusCode}: {errorBody}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var printers = ParsePrinterNames(content);
            return (true, printers, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NiceLabel printers fetch exception");
            return (false, [], ex.Message);
        }
    }

    public async Task<(bool Success, List<NiceLabelDocumentItem> Documents, string? ErrorMessage)> GetDocumentsAsync()
    {
        try
        {
            var url = $"{_options.BaseUrl.TrimEnd('/')}/document/v2/list/{_options.DocumentFolderId}";
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Add("Cache-Control", "no-cache");
            request.Headers.Add("Ocp-Apim-Subscription-Key", _options.SubscriptionKey);

            var response = await _http.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("NiceLabel documents fetch failed: {StatusCode} {Body}", (int)response.StatusCode, errorBody);
                return (false, [], $"HTTP {(int)response.StatusCode}: {errorBody}");
            }

            var content = await response.Content.ReadAsStringAsync();
            var documents = ParseDocuments(content);
            return (true, documents, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NiceLabel documents fetch exception");
            return (false, [], ex.Message);
        }
    }

    public async Task<(bool Success, string? ErrorMessage)> PrintNameplateAsync(
        string printerName,
        string filePath,
        int quantity,
        string printedOnText,
        string tankType,
        int tankSize,
        string serialNo)
    {
        try
        {
            var url = $"{_options.BaseUrl.TrimEnd('/')}/Print/v1/Print/{Uri.EscapeDataString(printerName)}";

            var body = new NiceLabelPrintRequest
            {
                FilePath = filePath,
                Quantity = quantity,
                Variables = [new NiceLabelVariables
                {
                    PrintedOnText = printedOnText,
                    TankType = tankType,
                    TankSize = tankSize.ToString(),
                    SerialNo = serialNo
                }]
            };

            using var request = new HttpRequestMessage(HttpMethod.Put, url);
            request.Headers.Add("Ocp-Apim-Subscription-Key", _options.SubscriptionKey);
            request.Content = JsonContent.Create(body);

            var response = await _http.SendAsync(request);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation(
                    "NiceLabel print succeeded for serial {SerialNo} on printer {Printer}",
                    serialNo, printerName);
                return (true, null);
            }

            var errorBody = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "NiceLabel print failed for serial {SerialNo}: {StatusCode} {Body}",
                serialNo, (int)response.StatusCode, errorBody);
            return (false, $"HTTP {(int)response.StatusCode}: {errorBody}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "NiceLabel print exception for serial {SerialNo}", serialNo);
            return (false, ex.Message);
        }
    }

    private class NiceLabelPrintRequest
    {
        [JsonPropertyName("FilePath")]
        public string FilePath { get; set; } = string.Empty;

        [JsonPropertyName("Quantity")]
        public int Quantity { get; set; }

        [JsonPropertyName("Variables")]
        public List<NiceLabelVariables> Variables { get; set; } = [];
    }

    private class NiceLabelVariables
    {
        [JsonPropertyName("PrintedOnText")]
        public string PrintedOnText { get; set; } = string.Empty;

        [JsonPropertyName("TankType")]
        public string TankType { get; set; } = string.Empty;

        [JsonPropertyName("TankSize")]
        public string TankSize { get; set; } = string.Empty;

        [JsonPropertyName("SerialNo")]
        public string SerialNo { get; set; } = string.Empty;
    }

    private static List<string> ParsePrinterNames(string jsonContent)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        using var doc = JsonDocument.Parse(jsonContent);
        CollectPrinterNames(doc.RootElement, names);
        return names.OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToList();
    }

    private static void CollectPrinterNames(JsonElement element, HashSet<string> output)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var item in element.EnumerateArray())
                    CollectPrinterNames(item, output);
                break;

            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.String &&
                        (string.Equals(property.Name, "printerName", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(property.Name, "printQueueName", StringComparison.OrdinalIgnoreCase)
                         || string.Equals(property.Name, "name", StringComparison.OrdinalIgnoreCase)))
                    {
                        var value = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(value))
                            output.Add(value.Trim());
                    }

                    CollectPrinterNames(property.Value, output);
                }
                break;
        }
    }

    private static List<NiceLabelDocumentItem> ParseDocuments(string jsonContent)
    {
        using var doc = JsonDocument.Parse(jsonContent);
        var result = new List<NiceLabelDocumentItem>();

        if (!doc.RootElement.TryGetProperty("items", out var items) || items.ValueKind != JsonValueKind.Array)
            return result;

        foreach (var item in items.EnumerateArray())
        {
            if (!item.TryGetProperty("itemType", out var itemTypeProp) ||
                !string.Equals(itemTypeProp.GetString(), "File", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var itemPath = item.TryGetProperty("itemPath", out var itemPathProp)
                ? itemPathProp.GetString()
                : null;
            var name = item.TryGetProperty("name", out var nameProp)
                ? nameProp.GetString()
                : null;

            if (string.IsNullOrWhiteSpace(itemPath))
                continue;

            if (!itemPath.Trim().EndsWith(".nlbl", StringComparison.OrdinalIgnoreCase))
                continue;

            result.Add(new NiceLabelDocumentItem
            {
                Name = string.IsNullOrWhiteSpace(name) ? itemPath.Trim() : name.Trim(),
                ItemPath = itemPath.Trim()
            });
        }

        return result
            .OrderBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(d => d.ItemPath, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }
}
