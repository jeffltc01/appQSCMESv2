using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MESv2.Api.Models;

namespace MESv2.Api.Data.Configurations;

public class AnnotationConfiguration : IEntityTypeConfiguration<Annotation>
{
    public void Configure(EntityTypeBuilder<Annotation> builder)
    {
        builder.Property(a => a.Status)
            .HasConversion<string>()
            .HasMaxLength(20);

        builder.HasOne(a => a.ProductionRecord)
            .WithMany(r => r.Annotations)
            .HasForeignKey(a => a.ProductionRecordId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.SerialNumber)
            .WithMany()
            .HasForeignKey(a => a.SerialNumberId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.DowntimeEvent)
            .WithMany(de => de.Annotations)
            .HasForeignKey(a => a.DowntimeEventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.AnnotationType)
            .WithMany(t => t.Annotations)
            .HasForeignKey(a => a.AnnotationTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.InitiatedByUser)
            .WithMany()
            .HasForeignKey(a => a.InitiatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(a => a.ResolvedByUser)
            .WithMany()
            .HasForeignKey(a => a.ResolvedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
