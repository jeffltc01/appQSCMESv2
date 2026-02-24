using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MESv2.Api.Models;

namespace MESv2.Api.Data.Configurations;

public class DemoShellFlowConfiguration : IEntityTypeConfiguration<DemoShellFlow>
{
    public void Configure(EntityTypeBuilder<DemoShellFlow> builder)
    {
        builder.Property(x => x.SerialNumber)
            .HasMaxLength(16)
            .IsRequired();

        builder.Property(x => x.CurrentStage)
            .HasMaxLength(32)
            .IsRequired();

        builder.HasOne(x => x.Plant)
            .WithMany()
            .HasForeignKey(x => x.PlantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.CreatedByUser)
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new { x.PlantId, x.ShellNumber })
            .IsUnique();

        builder.HasIndex(x => new { x.PlantId, x.CurrentStage, x.ShellNumber });
    }
}
