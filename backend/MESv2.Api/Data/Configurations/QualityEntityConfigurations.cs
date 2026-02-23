using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MESv2.Api.Models;

namespace MESv2.Api.Data.Configurations;

public class ControlPlanConfiguration : IEntityTypeConfiguration<ControlPlan>
{
    public void Configure(EntityTypeBuilder<ControlPlan> builder)
    {
        builder.HasOne(c => c.Characteristic)
            .WithMany()
            .HasForeignKey(c => c.CharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.WorkCenterProductionLine)
            .WithMany(wcpl => wcpl.ControlPlans)
            .HasForeignKey(c => c.WorkCenterProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CharacteristicConfiguration : IEntityTypeConfiguration<Characteristic>
{
    public void Configure(EntityTypeBuilder<Characteristic> builder)
    {
        builder.HasOne(c => c.ProductType)
            .WithMany(t => t.Characteristics)
            .HasForeignKey(c => c.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => c.Code).IsUnique();
    }
}

public class CharacteristicWorkCenterConfiguration : IEntityTypeConfiguration<CharacteristicWorkCenter>
{
    public void Configure(EntityTypeBuilder<CharacteristicWorkCenter> builder)
    {
        builder.HasOne(c => c.Characteristic)
            .WithMany()
            .HasForeignKey(c => c.CharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(c => c.WorkCenter)
            .WithMany(w => w.CharacteristicWorkCenters)
            .HasForeignKey(c => c.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DefectCodeConfiguration : IEntityTypeConfiguration<DefectCode>
{
    public void Configure(EntityTypeBuilder<DefectCode> builder)
    {
        builder.HasIndex(d => d.Code);
    }
}

public class DefectWorkCenterConfiguration : IEntityTypeConfiguration<DefectWorkCenter>
{
    public void Configure(EntityTypeBuilder<DefectWorkCenter> builder)
    {
        builder.HasOne(d => d.DefectCode)
            .WithMany()
            .HasForeignKey(d => d.DefectCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.WorkCenter)
            .WithMany(w => w.DefectWorkCenters)
            .HasForeignKey(d => d.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.EarliestDetectionWorkCenter)
            .WithMany()
            .HasForeignKey(d => d.EarliestDetectionWorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DefectLocationConfiguration : IEntityTypeConfiguration<DefectLocation>
{
    public void Configure(EntityTypeBuilder<DefectLocation> builder)
    {
        builder.HasOne(d => d.Characteristic)
            .WithMany(c => c.DefectLocations)
            .HasForeignKey(d => d.CharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class DefectLogConfiguration : IEntityTypeConfiguration<DefectLog>
{
    public void Configure(EntityTypeBuilder<DefectLog> builder)
    {
        builder.HasOne(d => d.ProductionRecord)
            .WithMany(r => r.DefectLogs)
            .HasForeignKey(d => d.ProductionRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.SerialNumber)
            .WithMany()
            .HasForeignKey(d => d.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.DefectCode)
            .WithMany(c => c.DefectLogs)
            .HasForeignKey(d => d.DefectCodeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Characteristic)
            .WithMany(c => c.DefectLogs)
            .HasForeignKey(d => d.CharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.Location)
            .WithMany(l => l.DefectLogs)
            .HasForeignKey(d => d.LocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.RepairedByUser)
            .WithMany()
            .HasForeignKey(d => d.RepairedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(d => d.CreatedByUser)
            .WithMany()
            .HasForeignKey(d => d.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Property(d => d.LocDetails1).HasPrecision(18, 6);
        builder.Property(d => d.LocDetails2).HasPrecision(18, 6);
        builder.Property(d => d.LocDetailsCode).HasMaxLength(20);

        builder.HasIndex(d => d.SerialNumberId);
    }
}
