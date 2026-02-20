using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class TraceabilityLogMapper
{
    public static TraceabilityLog? Map(dynamic row)
    {
        return new TraceabilityLog
        {
            Id = (Guid)row.Id,
            FromSerialNumberId = (Guid?)row.SerialNumberMasterId,
            ToSerialNumberId = (Guid?)row.SerialNumberComponentId,
            FromAlphaCode = null,
            ToAlphaCode = null,
            Relationship = "component",
            Quantity = (int?)(decimal?)row.Quantity,
            TankLocation = (string?)row.TankLocation,
            Timestamp = DateTime.UtcNow
        };
    }
}
