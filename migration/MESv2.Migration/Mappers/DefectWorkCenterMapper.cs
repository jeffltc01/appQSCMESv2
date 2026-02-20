using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class DefectWorkCenterMapper
{
    public static DefectWorkCenter? Map(dynamic row)
    {
        return new DefectWorkCenter
        {
            Id = (Guid)row.Id,
            DefectCodeId = (Guid)row.DefectId,
            WorkCenterId = (Guid)row.WorkCenterId,
            EarliestDetectionWorkCenterId = (Guid?)row.EarliestWorkCenterId
        };
    }
}
