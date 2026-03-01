using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MESv2.Api.Models;

namespace MESv2.Api.Data.Configurations;

public class ProductionRecordConfiguration : IEntityTypeConfiguration<ProductionRecord>
{
    public void Configure(EntityTypeBuilder<ProductionRecord> builder)
    {
        builder.HasOne(r => r.SerialNumber)
            .WithMany(s => s.ProductionRecords)
            .HasForeignKey(r => r.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.WorkCenter)
            .WithMany(w => w.ProductionRecords)
            .HasForeignKey(r => r.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Asset)
            .WithMany()
            .HasForeignKey(r => r.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.ProductionLine)
            .WithMany()
            .HasForeignKey(r => r.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Operator)
            .WithMany()
            .HasForeignKey(r => r.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.PlantGear)
            .WithMany(g => g.ProductionRecords)
            .HasForeignKey(r => r.PlantGearId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.SerialNumberId, r.WorkCenterId, r.Timestamp });
    }
}

public class SerialNumberConfiguration : IEntityTypeConfiguration<SerialNumber>
{
    public void Configure(EntityTypeBuilder<SerialNumber> builder)
    {
        builder.HasOne(s => s.Product)
            .WithMany(p => p.SerialNumbers)
            .HasForeignKey(s => s.ProductId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.MillVendor)
            .WithMany()
            .HasForeignKey(s => s.MillVendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ProcessorVendor)
            .WithMany()
            .HasForeignKey(s => s.ProcessorVendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.HeadsVendor)
            .WithMany()
            .HasForeignKey(s => s.HeadsVendorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ReplaceBySN)
            .WithMany()
            .HasForeignKey(s => s.ReplaceBySNId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ModifiedByUser)
            .WithMany()
            .HasForeignKey(s => s.ModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => new { s.Serial, s.PlantId, s.CreatedAt })
            .IsUnique();

        builder.HasIndex(s => s.PlantId);
    }
}

public class InspectionRecordConfiguration : IEntityTypeConfiguration<InspectionRecord>
{
    public void Configure(EntityTypeBuilder<InspectionRecord> builder)
    {
        builder.HasOne(i => i.SerialNumber)
            .WithMany()
            .HasForeignKey(i => i.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ProductionRecord)
            .WithMany()
            .HasForeignKey(i => i.ProductionRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.WorkCenter)
            .WithMany(w => w.InspectionRecords)
            .HasForeignKey(i => i.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Operator)
            .WithMany()
            .HasForeignKey(i => i.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.ControlPlan)
            .WithMany(c => c.InspectionRecords)
            .HasForeignKey(i => i.ControlPlanId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => i.SerialNumberId);
        builder.HasIndex(i => i.ProductionRecordId);
    }
}

public class TraceabilityLogConfiguration : IEntityTypeConfiguration<TraceabilityLog>
{
    public void Configure(EntityTypeBuilder<TraceabilityLog> builder)
    {
        builder.HasOne(t => t.FromSerialNumber)
            .WithMany()
            .HasForeignKey(t => t.FromSerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ToSerialNumber)
            .WithMany()
            .HasForeignKey(t => t.ToSerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ProductionRecord)
            .WithMany()
            .HasForeignKey(t => t.ProductionRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.Timestamp);
        builder.HasIndex(t => t.FromSerialNumberId);
        builder.HasIndex(t => t.ToSerialNumberId);
        builder.HasIndex(t => t.ProductionRecordId);
    }
}

public class WelderLogConfiguration : IEntityTypeConfiguration<WelderLog>
{
    public void Configure(EntityTypeBuilder<WelderLog> builder)
    {
        builder.HasOne(w => w.ProductionRecord)
            .WithMany(r => r.WelderLogs)
            .HasForeignKey(w => w.ProductionRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.User)
            .WithMany()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(w => w.Characteristic)
            .WithMany(c => c.WelderLogs)
            .HasForeignKey(w => w.CharacteristicId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class MaterialQueueItemConfiguration : IEntityTypeConfiguration<MaterialQueueItem>
{
    public void Configure(EntityTypeBuilder<MaterialQueueItem> builder)
    {
        builder.HasOne(m => m.WorkCenter)
            .WithMany(w => w.MaterialQueueItems)
            .HasForeignKey(m => m.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.SerialNumber)
            .WithMany()
            .HasForeignKey(m => m.SerialNumberId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.Operator)
            .WithMany()
            .HasForeignKey(m => m.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(m => m.ProductionLine)
            .WithMany()
            .HasForeignKey(m => m.ProductionLineId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(m => new { m.WorkCenterId, m.Status });
    }
}

public class QueueTransactionConfiguration : IEntityTypeConfiguration<QueueTransaction>
{
    public void Configure(EntityTypeBuilder<QueueTransaction> builder)
    {
        builder.HasOne(qt => qt.WorkCenter)
            .WithMany()
            .HasForeignKey(qt => qt.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(qt => qt.ProductionLine)
            .WithMany()
            .HasForeignKey(qt => qt.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(qt => new { qt.WorkCenterId, qt.ProductionLineId, qt.Timestamp });
    }
}

public class RoundSeamSetupConfiguration : IEntityTypeConfiguration<RoundSeamSetup>
{
    public void Configure(EntityTypeBuilder<RoundSeamSetup> builder)
    {
        builder.HasOne(r => r.WorkCenter)
            .WithMany()
            .HasForeignKey(r => r.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Rs1Welder)
            .WithMany()
            .HasForeignKey(r => r.Rs1WelderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Rs2Welder)
            .WithMany()
            .HasForeignKey(r => r.Rs2WelderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Rs3Welder)
            .WithMany()
            .HasForeignKey(r => r.Rs3WelderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(r => r.Rs4Welder)
            .WithMany()
            .HasForeignKey(r => r.Rs4WelderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.WorkCenterId, r.CreatedAt });
    }
}
