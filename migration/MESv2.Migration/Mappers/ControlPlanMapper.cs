using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class ControlPlanMapper
{
    public static ControlPlan? Map(dynamic row)
    {
        int enabled = (int?)row.CollectionEnabled ?? 0;
        int gateCheck = (int?)row.IsGateCheck ?? 0;

        return new ControlPlan
        {
            Id = (Guid)row.Id,
            CharacteristicId = (Guid)row.CharacteristicId,
            WorkCenterId = (Guid)row.CollectionWorkCenterId,
            IsEnabled = enabled != 0,
            ResultType = (string)(row.ResultType ?? ""),
            IsGateCheck = gateCheck != 0
        };
    }
}
