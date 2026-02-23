using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MESv2.Api.Models;

namespace MESv2.Api.Data.Configurations;

public class SpotXrayIncrementConfiguration : IEntityTypeConfiguration<SpotXrayIncrement>
{
    public void Configure(EntityTypeBuilder<SpotXrayIncrement> builder)
    {
        builder.HasOne(s => s.ProductionRecord)
            .WithMany()
            .HasForeignKey(s => s.ManufacturingLogId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.InspectTankSn)
            .WithMany()
            .HasForeignKey(s => s.InspectTankId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Welder1).WithMany().HasForeignKey(s => s.Welder1Id).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Welder2).WithMany().HasForeignKey(s => s.Welder2Id).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Welder3).WithMany().HasForeignKey(s => s.Welder3Id).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Welder4).WithMany().HasForeignKey(s => s.Welder4Id).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Seam1Trace1Tank).WithMany().HasForeignKey(s => s.Seam1Trace1TankId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Seam1Trace2Tank).WithMany().HasForeignKey(s => s.Seam1Trace2TankId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Seam2Trace1Tank).WithMany().HasForeignKey(s => s.Seam2Trace1TankId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Seam2Trace2Tank).WithMany().HasForeignKey(s => s.Seam2Trace2TankId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Seam3Trace1Tank).WithMany().HasForeignKey(s => s.Seam3Trace1TankId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Seam3Trace2Tank).WithMany().HasForeignKey(s => s.Seam3Trace2TankId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Seam4Trace1Tank).WithMany().HasForeignKey(s => s.Seam4Trace1TankId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(s => s.Seam4Trace2Tank).WithMany().HasForeignKey(s => s.Seam4Trace2TankId).OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ModifiedByUser)
            .WithMany()
            .HasForeignKey(s => s.ModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.ManufacturingLogId);
        builder.HasIndex(s => s.InspectTankId);
    }
}

public class SpotXrayIncrementTankConfiguration : IEntityTypeConfiguration<SpotXrayIncrementTank>
{
    public void Configure(EntityTypeBuilder<SpotXrayIncrementTank> builder)
    {
        builder.HasOne(t => t.SpotXrayIncrement)
            .WithMany(i => i.IncrementTanks)
            .HasForeignKey(t => t.SpotXrayIncrementId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.SerialNumber)
            .WithMany()
            .HasForeignKey(t => t.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.SpotXrayIncrementId);
        builder.HasIndex(t => t.SerialNumberId);
    }
}

public class XrayShotCounterConfiguration : IEntityTypeConfiguration<XrayShotCounter>
{
    public void Configure(EntityTypeBuilder<XrayShotCounter> builder)
    {
        builder.HasOne(c => c.Plant)
            .WithMany()
            .HasForeignKey(c => c.PlantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.PlantId, c.CounterDate })
            .IsUnique();
    }
}

public class XrayQueueItemConfiguration : IEntityTypeConfiguration<XrayQueueItem>
{
    public void Configure(EntityTypeBuilder<XrayQueueItem> builder)
    {
        builder.HasOne(x => x.WorkCenter)
            .WithMany()
            .HasForeignKey(x => x.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.SerialNumber)
            .WithMany()
            .HasForeignKey(x => x.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Operator)
            .WithMany()
            .HasForeignKey(x => x.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.WorkCenterId);
        builder.HasIndex(x => x.SerialNumberId);
    }
}
