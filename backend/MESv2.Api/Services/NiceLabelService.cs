using System.Net.Http.Json;
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

    public async Task<(bool Success, string? ErrorMessage)> PrintNameplateAsync(
        string printerName,
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
                FilePath = _options.FilePath,
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
}
