using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class SerialNumberMapper
{
    public static SerialNumber? Map(dynamic row)
    {
        return new SerialNumber
        {
            Id = (Guid)row.Id,
            Serial = (string)(row.SerialNumber ?? ""),
            ProductId = (Guid?)row.ProductId,
            SiteCode = (string)(row.SiteCode ?? ""),
            Notes = (string?)row.Notes,
            MillVendorId = (Guid?)row.MillVendorId,
            ProcessorVendorId = (Guid?)row.ProcessorVendorId,
            HeadsVendorId = (Guid?)row.HeadsVendorId,
            CoilNumber = (string?)row.CoilNumber,
            HeatNumber = (string?)row.HeatNumber,
            LotNumber = (string?)row.LotNumber,
            ReplaceBySNId = (Guid?)row.ReplaceBySNId,
            Rs1Changed = ((bool?)row.RS1Changed) ?? false,
            Rs2Changed = ((bool?)row.RS2Changed) ?? false,
            Rs3Changed = ((bool?)row.RS3Changed) ?? false,
            Rs4Changed = ((bool?)row.RS4Changed) ?? false,
            IsObsolete = ((bool?)row.IsObsolete) ?? false,
            CreatedAt = (DateTime?)row.CreatedDateTime ?? DateTime.UtcNow,
            CreatedByUserId = (Guid?)row.CreatedByUserId,
            ModifiedDateTime = (DateTime?)row.ModifiedDateTime,
            ModifiedByUserId = (Guid?)row.ModifiedByUserId
        };
    }
}
