namespace MESv2.Api.Services;

public interface INiceLabelService
{
    Task<(bool Success, List<string> Printers, string? ErrorMessage)> GetPrintersAsync();
    Task<(bool Success, List<NiceLabelDocumentItem> Documents, string? ErrorMessage)> GetDocumentsAsync();

    Task<(bool Success, string? ErrorMessage)> PrintNameplateAsync(
        string printerName,
        string filePath,
        int quantity,
        string printedOnText,
        string tankType,
        int tankSize,
        string serialNo);
}
