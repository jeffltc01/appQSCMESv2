namespace MESv2.Api.Services;

public interface INiceLabelService
{
    Task<(bool Success, string? ErrorMessage)> PrintNameplateAsync(
        string printerName,
        int quantity,
        string printedOnText,
        string tankType,
        int tankSize,
        string serialNo);
}
