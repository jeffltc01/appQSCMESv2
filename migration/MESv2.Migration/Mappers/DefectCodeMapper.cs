using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class DefectCodeMapper
{
    public static DefectCode? Map(dynamic row)
    {
        int? defectCode = row.DefectCode as int?;
        return new DefectCode
        {
            Id = (Guid)row.Id,
            Code = defectCode?.ToString() ?? "",
            Name = (string)(row.DefectName ?? ""),
            Severity = ((int?)row.DefectSeverity)?.ToString(),
            SystemType = (string?)row.SystemTypeName
        };
    }
}
