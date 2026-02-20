using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class ChangeLogMapper
{
    public static ChangeLog? Map(dynamic row)
    {
        return new ChangeLog
        {
            Id = (Guid)row.Id,
            RecordTable = (string)(row.RecordTable ?? ""),
            RecordId = (Guid?)row.RecordId ?? Guid.Empty,
            ChangeDateTime = (DateTime?)row.ChangeDateTime ?? DateTime.UtcNow,
            ChangeByUserId = (Guid?)row.ChangeByUserId ?? Guid.Empty,
            FieldName = (string)(row.FieldName ?? ""),
            FromValue = (string?)row.FromValue,
            ToValue = (string?)row.ToValue,
            FromValueId = (Guid?)row.FromValueId,
            ToValueId = (Guid?)row.ToValueId
        };
    }
}
