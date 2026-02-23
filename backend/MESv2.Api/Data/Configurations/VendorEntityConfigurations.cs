using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MESv2.Api.Models;

namespace MESv2.Api.Data.Configurations;

public class VendorConfiguration : IEntityTypeConfiguration<Vendor>
{
    public void Configure(EntityTypeBuilder<Vendor> builder)
    {
        builder.HasIndex(v => v.VendorType);
    }
}

public class VendorPlantConfiguration : IEntityTypeConfiguration<VendorPlant>
{
    public void Configure(EntityTypeBuilder<VendorPlant> builder)
    {
        builder.HasOne(vp => vp.Vendor)
            .WithMany(v => v.VendorPlants)
            .HasForeignKey(vp => vp.VendorId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(vp => vp.Plant)
            .WithMany()
            .HasForeignKey(vp => vp.PlantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(vp => new { vp.VendorId, vp.PlantId })
            .IsUnique();
    }
}
