using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MESv2.Api.Models;

namespace MESv2.Api.Data.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.Property(a => a.Id)
            .UseIdentityColumn();

        builder.HasOne(a => a.ChangedByUser)
            .WithMany()
            .HasForeignKey(a => a.ChangedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.ChangedAtUtc);
    }
}

public class ChangeLogConfiguration : IEntityTypeConfiguration<ChangeLog>
{
    public void Configure(EntityTypeBuilder<ChangeLog> builder)
    {
        builder.HasOne(c => c.ChangeByUser)
            .WithMany()
            .HasForeignKey(c => c.ChangeByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(c => new { c.RecordTable, c.RecordId });
    }
}

public class PrintLogConfiguration : IEntityTypeConfiguration<PrintLog>
{
    public void Configure(EntityTypeBuilder<PrintLog> builder)
    {
        builder.HasOne(pl => pl.SerialNumber)
            .WithMany()
            .HasForeignKey(pl => pl.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(pl => pl.RequestedByUser)
            .WithMany()
            .HasForeignKey(pl => pl.RequestedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pl => pl.SerialNumberId);
    }
}

public class FrontendTelemetryEventConfiguration : IEntityTypeConfiguration<FrontendTelemetryEvent>
{
    public void Configure(EntityTypeBuilder<FrontendTelemetryEvent> builder)
    {
        builder.Property(e => e.Id)
            .UseIdentityColumn();

        builder.Property(e => e.Category)
            .HasMaxLength(64);

        builder.Property(e => e.Source)
            .HasMaxLength(64);

        builder.Property(e => e.Severity)
            .HasMaxLength(32);

        builder.Property(e => e.Route)
            .HasMaxLength(256);

        builder.Property(e => e.Screen)
            .HasMaxLength(128);

        builder.Property(e => e.Message)
            .HasMaxLength(2048);

        builder.Property(e => e.Stack)
            .HasMaxLength(8000);

        builder.Property(e => e.MetadataJson)
            .HasMaxLength(8000);

        builder.Property(e => e.SessionId)
            .HasMaxLength(128);

        builder.Property(e => e.CorrelationId)
            .HasMaxLength(128);

        builder.Property(e => e.ApiPath)
            .HasMaxLength(256);

        builder.Property(e => e.HttpMethod)
            .HasMaxLength(16);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.WorkCenter)
            .WithMany()
            .HasForeignKey(e => e.WorkCenterId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.ProductionLine)
            .WithMany()
            .HasForeignKey(e => e.ProductionLineId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(e => e.Plant)
            .WithMany()
            .HasForeignKey(e => e.PlantId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasIndex(e => e.OccurredAtUtc);
        builder.HasIndex(e => e.Category);
        builder.HasIndex(e => e.Source);
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.WorkCenterId);
    }
}
