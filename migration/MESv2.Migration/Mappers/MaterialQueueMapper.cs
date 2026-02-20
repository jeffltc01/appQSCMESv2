using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class MaterialQueueMapper
{
    /// <summary>
    /// Maps v1 mesWorkCenterMaterialQueue (joined with SerialNumberMaster + Product) to v2 MaterialQueueItem.
    /// v1 is serial-number-centric; v2 is denormalized with product/vendor/heat/coil fields.
    /// </summary>
    public static MaterialQueueItem? Map(dynamic row)
    {
        return new MaterialQueueItem
        {
            Id = (Guid)row.Id,
            WorkCenterId = (Guid)row.WorkCenterId,
            Position = (int)(decimal)(row.QueuePosition ?? 0m),
            Status = MapStatus((string?)row.QueueStatus),
            ProductDescription = (string?)row.ProductDescription ?? "",
            ShellSize = ((int?)row.TankSize)?.ToString(),
            HeatNumber = "",
            CoilNumber = (string?)row.CoilNumber ?? "",
            Quantity = (int)(decimal)(row.Quantity ?? 0m),
            CardId = null,
            CardColor = null,
            CreatedAt = (DateTime?)row.CreateDateTime ?? DateTime.UtcNow,
            ProductId = (Guid?)row.ProductId,
            VendorMillId = (Guid?)row.MillVendorId,
            VendorProcessorId = (Guid?)row.ProcessorVendorId,
            VendorHeadId = (Guid?)row.HeadsVendorId,
            LotNumber = (string?)row.LotNumber,
            CoilSlabNumber = null,
            OperatorId = null,
            QueueType = null
        };
    }

    private static string MapStatus(string? v1Status)
    {
        if (string.IsNullOrEmpty(v1Status)) return "queued";
        return v1Status.ToLowerInvariant() switch
        {
            "active" => "active",
            "completed" or "complete" => "completed",
            _ => "queued"
        };
    }
}
