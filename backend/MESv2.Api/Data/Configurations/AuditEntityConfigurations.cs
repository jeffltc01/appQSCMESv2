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
