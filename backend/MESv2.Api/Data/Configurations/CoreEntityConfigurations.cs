using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MESv2.Api.Models;

namespace MESv2.Api.Data.Configurations;

public class PlantConfiguration : IEntityTypeConfiguration<Plant>
{
    public void Configure(EntityTypeBuilder<Plant> builder)
    {
        builder.HasOne(p => p.CurrentPlantGear)
            .WithMany()
            .HasForeignKey(p => p.CurrentPlantGearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(p => p.Code).IsUnique();
    }
}

public class ProductionLineConfiguration : IEntityTypeConfiguration<ProductionLine>
{
    public void Configure(EntityTypeBuilder<ProductionLine> builder)
    {
        builder.HasOne(p => p.Plant)
            .WithMany(p => p.ProductionLines)
            .HasForeignKey(p => p.PlantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkCenterConfiguration : IEntityTypeConfiguration<WorkCenter>
{
    public void Configure(EntityTypeBuilder<WorkCenter> builder)
    {
        builder.Property(w => w.ProductionSequence)
            .HasPrecision(18, 6);

        builder.HasOne(w => w.WorkCenterType)
            .WithMany(t => t.WorkCenters)
            .HasForeignKey(w => w.WorkCenterTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.MaterialQueueForWC)
            .WithMany()
            .HasForeignKey(w => w.MaterialQueueForWCId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class WorkCenterProductionLineConfiguration : IEntityTypeConfiguration<WorkCenterProductionLine>
{
    public void Configure(EntityTypeBuilder<WorkCenterProductionLine> builder)
    {
        builder.HasOne(wcpl => wcpl.WorkCenter)
            .WithMany(w => w.WorkCenterProductionLines)
            .HasForeignKey(wcpl => wcpl.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(wcpl => wcpl.ProductionLine)
            .WithMany()
            .HasForeignKey(wcpl => wcpl.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(wcpl => new { wcpl.WorkCenterId, wcpl.ProductionLineId })
            .IsUnique();
    }
}

public class AssetConfiguration : IEntityTypeConfiguration<Asset>
{
    public void Configure(EntityTypeBuilder<Asset> builder)
    {
        builder.Property(a => a.LaneName).HasMaxLength(50);

        builder.HasOne(a => a.WorkCenter)
            .WithMany(w => w.Assets)
            .HasForeignKey(a => a.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ProductionLine)
            .WithMany()
            .HasForeignKey(a => a.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ShiftScheduleConfiguration : IEntityTypeConfiguration<ShiftSchedule>
{
    public void Configure(EntityTypeBuilder<ShiftSchedule> builder)
    {
        builder.HasOne(s => s.Plant)
            .WithMany()
            .HasForeignKey(s => s.PlantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.PlantId, s.EffectiveDate })
            .IsUnique();

        builder.Property(s => s.MondayHours).HasPrecision(5, 2);
        builder.Property(s => s.TuesdayHours).HasPrecision(5, 2);
        builder.Property(s => s.WednesdayHours).HasPrecision(5, 2);
        builder.Property(s => s.ThursdayHours).HasPrecision(5, 2);
        builder.Property(s => s.FridayHours).HasPrecision(5, 2);
        builder.Property(s => s.SaturdayHours).HasPrecision(5, 2);
        builder.Property(s => s.SundayHours).HasPrecision(5, 2);
    }
}

public class WorkCenterCapacityTargetConfiguration : IEntityTypeConfiguration<WorkCenterCapacityTarget>
{
    public void Configure(EntityTypeBuilder<WorkCenterCapacityTarget> builder)
    {
        builder.HasOne(t => t.WorkCenterProductionLine)
            .WithMany(wcpl => wcpl.CapacityTargets)
            .HasForeignKey(t => t.WorkCenterProductionLineId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.PlantGear)
            .WithMany()
            .HasForeignKey(t => t.PlantGearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new { t.WorkCenterProductionLineId, t.TankSize, t.PlantGearId })
            .IsUnique()
            .HasFilter("[TankSize] IS NOT NULL");

        builder.Property(t => t.TargetUnitsPerHour)
            .HasPrecision(10, 2);
    }
}

public class PlantGearConfiguration : IEntityTypeConfiguration<PlantGear>
{
    public void Configure(EntityTypeBuilder<PlantGear> builder)
    {
        builder.HasOne(g => g.Plant)
            .WithMany(p => p.PlantGears)
            .HasForeignKey(g => g.PlantId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PlantPrinterConfiguration : IEntityTypeConfiguration<PlantPrinter>
{
    public void Configure(EntityTypeBuilder<PlantPrinter> builder)
    {
        builder.HasOne(pp => pp.Plant)
            .WithMany(p => p.PlantPrinters)
            .HasForeignKey(pp => pp.PlantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pp => new { pp.PlantId, pp.PrintLocation })
            .IsUnique();
    }
}

public class SiteScheduleConfiguration : IEntityTypeConfiguration<SiteSchedule>
{
    public void Configure(EntityTypeBuilder<SiteSchedule> builder)
    {
        builder.HasOne(s => s.Plant)
            .WithMany()
            .HasForeignKey(s => s.PlantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.PlantId);
    }
}

public class BarcodeCardConfiguration : IEntityTypeConfiguration<BarcodeCard>
{
    public void Configure(EntityTypeBuilder<BarcodeCard> builder)
    {
        builder.HasIndex(b => b.CardValue);
    }
}
