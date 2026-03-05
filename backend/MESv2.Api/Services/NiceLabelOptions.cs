namespace MESv2.Api.Services;

public class NiceLabelOptions
{
    public string BaseUrl { get; set; } = string.Empty;
    public string SubscriptionKey { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public int DocumentFolderId { get; set; } = 31;
    public bool AllowLivePrintInNonProd { get; set; } = false;
}
