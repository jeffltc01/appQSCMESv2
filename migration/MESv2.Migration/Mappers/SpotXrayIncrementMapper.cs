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
            Seam1ShotDateTime = row.Seam1Date as DateTime?,
            Seam1Result = (string?)row.Seam1Result,
            Seam1Trace1ShotNo = (string?)row.Seam1Trace1ShotNo,
            Seam1Trace1DateTime = row.Seam1Trace1ShotDate as DateTime?,
            Seam1Trace1Result = (string?)row.Seam1Trace1Result,
            Seam1Trace2ShotNo = (string?)row.Seam1Trace2ShotNo,
            Seam1Trace2DateTime = row.Seam1Trace2ShotDate as DateTime?,
            Seam1Trace2Result = (string?)row.Seam1Trace2Result,
            Seam1FinalShotNo = (string?)row.Seam1FinalShotNo,
            Seam1FinalDateTime = row.Seam1FinalShotDate as DateTime?,
            Seam1FinalResult = (string?)row.Seam1FinalResult,

            Seam2ShotNo = (string?)row.Seam2ShotNo,
            Seam2ShotDateTime = row.Seam2Date as DateTime?,
            Seam2Result = (string?)row.Seam2Result,
            Seam2Trace1ShotNo = (string?)row.Seam2Trace1ShotNo,
            Seam2Trace1DateTime = row.Seam2Trace1ShotDate as DateTime?,
            Seam2Trace1Result = (string?)row.Seam2Trace1Result,
            Seam2Trace2ShotNo = (string?)row.Seam2Trace2ShotNo,
            Seam2Trace2DateTime = row.Seam2Trace2ShotDate as DateTime?,
            Seam2Trace2Result = (string?)row.Seam2Trace2Result,
            Seam2FinalShotNo = (string?)row.Seam2FinalShotNo,
            Seam2FinalDateTime = row.Seam2FinalShotDate as DateTime?,
            Seam2FinalResult = (string?)row.Seam2FinalResult,

            Seam3ShotNo = (string?)row.Seam3ShotNo,
            Seam3ShotDateTime = row.Seam3Date as DateTime?,
            Seam3Result = (string?)row.Seam3Result,
            Seam3Trace1ShotNo = (string?)row.Seam3Trace1ShotNo,
            Seam3Trace1DateTime = row.Seam3Trace1ShotDate as DateTime?,
            Seam3Trace1Result = (string?)row.Seam3Trace1Result,
            Seam3Trace2ShotNo = (string?)row.Seam3Trace2ShotNo,
            Seam3Trace2DateTime = row.Seam3Trace2ShotDate as DateTime?,
            Seam3Trace2Result = (string?)row.Seam3Trace2Result,
            Seam3FinalShotNo = (string?)row.Seam3FinalShotNo,
            Seam3FinalDateTime = row.Seam3FinalShotDate as DateTime?,
            Seam3FinalResult = (string?)row.Seam3FinalResult,

            Seam4ShotNo = (string?)row.Seam4ShotNo,
            Seam4ShotDateTime = row.Seam4Date as DateTime?,
            Seam4Result = (string?)row.Seam4Result,
            Seam4Trace1ShotNo = (string?)row.Seam4Trace1ShotNo,
            Seam4Trace1DateTime = row.Seam4Trace1ShotDate as DateTime?,
            Seam4Trace1Result = (string?)row.Seam4Trace1Result,
            Seam4Trace2ShotNo = (string?)row.Seam4Trace2ShotNo,
            Seam4Trace2DateTime = row.Seam4Trace2ShotDate as DateTime?,
            Seam4Trace2Result = (string?)row.Seam4Trace2Result,
            Seam4FinalShotNo = (string?)row.Seam4FinalShotNo,
            Seam4FinalDateTime = row.Seam4FinalShotDate as DateTime?,
            Seam4FinalResult = (string?)row.Seam4FinalResult,

            CreatedByUserId = (Guid?)row.CreatedByUserId,
            CreatedDateTime = (DateTime?)row.CreatedDateTime,
            ModifiedByUserId = (Guid?)row.ModifiedByUserId,
            ModifiedDateTime = (DateTime?)row.ModifiedDateTime
        };
    }
}
