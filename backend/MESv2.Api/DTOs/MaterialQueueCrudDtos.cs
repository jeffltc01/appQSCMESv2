namespace MESv2.Api.DTOs;

public class CreateMaterialQueueItemDto
{
    public Guid ProductId { get; set; }
    public Guid? VendorMillId { get; set; }
    public Guid? VendorProcessorId { get; set; }
    public string HeatNumber { get; set; } = string.Empty;
    public string CoilNumber { get; set; } = string.Empty;
    public string? LotNumber { get; set; }
    public int Quantity { get; set; }
}

public class UpdateMaterialQueueItemDto
{
    public Guid? ProductId { get; set; }
    public Guid? VendorMillId { get; set; }
    public Guid? VendorProcessorId { get; set; }
    public string? HeatNumber { get; set; }
    public string? CoilNumber { get; set; }
    public string? LotNumber { get; set; }
    public int? Quantity { get; set; }
}

public class CreateFitupQueueItemDto
{
    public Guid ProductId { get; set; }
    public Guid VendorHeadId { get; set; }
    public string? LotNumber { get; set; }
    public string? HeatNumber { get; set; }
    public string? CoilSlabNumber { get; set; }
    public string CardCode { get; set; } = string.Empty;
}

public class UpdateFitupQueueItemDto
{
    public Guid? ProductId { get; set; }
    public Guid? VendorHeadId { get; set; }
    public string? LotNumber { get; set; }
    public string? HeatNumber { get; set; }
    public string? CoilSlabNumber { get; set; }
    public string? CardCode { get; set; }
}
