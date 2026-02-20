using MESv2.Api.Models;

namespace MESv2.Migration.Mappers;

public static class SpotXrayIncrementMapper
{
    public static SpotXrayIncrement? Map(dynamic row)
    {
        int isDraft = (int?)row.IsDraft ?? 0;

        return new SpotXrayIncrement
        {
            Id = (Guid)row.Id,
            ManufacturingLogId = (Guid)row.ManufacturingLogId,
            IncrementNo = (string)(row.IncrementNo ?? ""),
            OverallStatus = (string)(row.OverallStatus ?? ""),
            LaneNo = (string)(row.LaneNo ?? ""),
            IsDraft = isDraft != 0,
            TankSize = (int?)row.TankSize,
            InspectTank = (string?)row.InspectTank,
            InspectTankId = (Guid?)row.InspectTankId,
            Seam1ShotNo = (string?)row.Seam1ShotNo,
            Seam1ShotDateTime = (row.Seam1ShotDateTime as DateTime?)?.ToString("o") ?? (string?)row.Seam1ShotDateTime,
            Seam1InitialResult = (string?)row.Seam1InitialResult,
            Seam1FinalResult = (string?)row.Seam1FinalResult,
            Seam2ShotNo = (string?)row.Seam2ShotNo,
            Seam2ShotDateTime = (row.Seam2ShotDateTime as DateTime?)?.ToString("o") ?? (string?)row.Seam2ShotDateTime,
            Seam2InitialResult = (string?)row.Seam2InitialResult,
            Seam2FinalResult = (string?)row.Seam2FinalResult,
            Seam3ShotNo = (string?)row.Seam3ShotNo,
            Seam3ShotDateTime = (row.Seam3ShotDateTime as DateTime?)?.ToString("o") ?? (string?)row.Seam3ShotDateTime,
            Seam3InitialResult = (string?)row.Seam3InitialResult,
            Seam3FinalResult = (string?)row.Seam3FinalResult,
            Seam4ShotNo = (string?)row.Seam4ShotNo,
            Seam4ShotDateTime = (row.Seam4ShotDateTime as DateTime?)?.ToString("o") ?? (string?)row.Seam4ShotDateTime,
            Seam4InitialResult = (string?)row.Seam4InitialResult,
            Seam4FinalResult = (string?)row.Seam4FinalResult,
            Welder1 = (string?)row.Welder1,
            Welder2 = (string?)row.Welder2,
            Welder3 = (string?)row.Welder3,
            Welder4 = (string?)row.Welder4,
            CreatedByUserId = (Guid?)row.CreatedByUserId,
            CreatedDateTime = (DateTime?)row.CreatedDateTime,
            ModifiedByUserId = (Guid?)row.ModifiedByUserId,
            ModifiedDateTime = (DateTime?)row.ModifiedDateTime
        };
    }
}
