using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class DefectLocationMapper
{
    public static DefectLocation? Map(dynamic row)
    {
        int? lookupId = row.LookupId as int?;

        return new DefectLocation
        {
            Id = (Guid)row.Id,
            Code = lookupId?.ToString() ?? "",
            Name = (string)(row.LocationName ?? ""),
            DefaultLocationDetail = null,
            CharacteristicId = (Guid?)row.CharacteristicId
        };
    }
}
