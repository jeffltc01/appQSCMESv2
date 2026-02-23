using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MESv2.Api.Models;

namespace MESv2.Api.Data.Configurations;

public class DowntimeReasonCategoryConfiguration : IEntityTypeConfiguration<DowntimeReasonCategory>
{
    public void Configure(EntityTypeBuilder<DowntimeReasonCategory> builder)
    {
        builder.HasOne(c => c.Plant)
            .WithMany()
            .HasForeignKey(c => c.PlantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DowntimeReasonConfiguration : IEntityTypeConfiguration<DowntimeReason>
{
    public void Configure(EntityTypeBuilder<DowntimeReason> builder)
    {
        builder.HasOne(r => r.DowntimeReasonCategory)
            .WithMany(c => c.DowntimeReasons)
            .HasForeignKey(r => r.DowntimeReasonCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkCenterProductionLineDowntimeReasonConfiguration : IEntityTypeConfiguration<WorkCenterProductionLineDowntimeReason>
{
    public void Configure(EntityTypeBuilder<WorkCenterProductionLineDowntimeReason> builder)
    {
        builder.HasOne(x => x.WorkCenterProductionLine)
            .WithMany(wcpl => wcpl.WorkCenterProductionLineDowntimeReasons)
            .HasForeignKey(x => x.WorkCenterProductionLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DowntimeReason)
            .WithMany(r => r.WorkCenterProductionLineDowntimeReasons)
            .HasForeignKey(x => x.DowntimeReasonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.WorkCenterProductionLineId, x.DowntimeReasonId })
            .IsUnique();
    }
}

public class DowntimeEventConfiguration : IEntityTypeConfiguration<DowntimeEvent>
{
    public void Configure(EntityTypeBuilder<DowntimeEvent> builder)
    {
        builder.HasOne(e => e.WorkCenterProductionLine)
            .WithMany(wcpl => wcpl.DowntimeEvents)
            .HasForeignKey(e => e.WorkCenterProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OperatorUser)
            .WithMany()
            .HasForeignKey(e => e.OperatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DowntimeReason)
            .WithMany()
            .HasForeignKey(e => e.DowntimeReasonId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.WorkCenterProductionLineId, e.StartedAt });

        builder.Property(e => e.DurationMinutes)
            .HasPrecision(10, 2);
    }
}
