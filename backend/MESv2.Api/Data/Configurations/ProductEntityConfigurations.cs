using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MESv2.Api.Models;

namespace MESv2.Api.Data.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasOne(p => p.ProductType)
            .WithMany(t => t.Products)
            .HasForeignKey(p => p.ProductTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductPlantConfiguration : IEntityTypeConfiguration<ProductPlant>
{
    public void Configure(EntityTypeBuilder<ProductPlant> builder)
    {
        builder.HasOne(pp => pp.Product)
            .WithMany(p => p.ProductPlants)
            .HasForeignKey(pp => pp.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pp => pp.Plant)
            .WithMany()
            .HasForeignKey(pp => pp.PlantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(pp => new { pp.ProductId, pp.PlantId })
            .IsUnique();
    }
}
