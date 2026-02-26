using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MESv2.Api.Models;

namespace MESv2.Api.Data.Configurations;

public class ChecklistTemplateConfiguration : IEntityTypeConfiguration<ChecklistTemplate>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplate> builder)
    {
        builder.Property(t => t.TemplateCode).HasMaxLength(64);
        builder.Property(t => t.Title).HasMaxLength(200);
        builder.Property(t => t.ChecklistType).HasMaxLength(64);
        builder.Property(t => t.ScopeLevel).HasMaxLength(32);
        builder.Property(t => t.ResponseMode).HasMaxLength(16);

        builder.HasOne(t => t.Site)
            .WithMany()
            .HasForeignKey(t => t.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.WorkCenter)
            .WithMany()
            .HasForeignKey(t => t.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.ProductionLine)
            .WithMany()
            .HasForeignKey(t => t.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.CreatedByUser)
            .WithMany()
            .HasForeignKey(t => t.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.OwnerUser)
            .WithMany()
            .HasForeignKey(t => t.OwnerUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => new
        {
            t.ChecklistType,
            t.ScopeLevel,
            t.SiteId,
            t.WorkCenterId,
            t.ProductionLineId,
            t.VersionNo
        }).IsUnique();

        builder.HasIndex(t => new
        {
            t.ChecklistType,
            t.IsActive,
            t.EffectiveFromUtc,
            t.EffectiveToUtc
        });
    }
}

public class ChecklistTemplateItemConfiguration : IEntityTypeConfiguration<ChecklistTemplateItem>
{
    public void Configure(EntityTypeBuilder<ChecklistTemplateItem> builder)
    {
        builder.Property(i => i.Prompt).HasMaxLength(500);
        builder.Property(i => i.Section).HasMaxLength(200);
        builder.Property(i => i.ResponseMode).HasMaxLength(16);
        builder.Property(i => i.ResponseType).HasMaxLength(32);
        builder.Property(i => i.ResponseOptionsJson).HasMaxLength(4000);
        builder.Property(i => i.DimensionUnitOfMeasure).HasMaxLength(32);
        builder.Property(i => i.HelpText).HasMaxLength(500);

        builder.HasOne(i => i.ChecklistTemplate)
            .WithMany(t => t.Items)
            .HasForeignKey(i => i.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.ScoreType)
            .WithMany()
            .HasForeignKey(i => i.ScoreTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(i => new { i.ChecklistTemplateId, i.SortOrder });
    }
}

public class ChecklistEntryConfiguration : IEntityTypeConfiguration<ChecklistEntry>
{
    public void Configure(EntityTypeBuilder<ChecklistEntry> builder)
    {
        builder.Property(e => e.ChecklistType).HasMaxLength(64);
        builder.Property(e => e.Status).HasMaxLength(32);
        builder.Property(e => e.ResolvedFromScope).HasMaxLength(32);
        builder.Property(e => e.ResolvedTemplateCode).HasMaxLength(64);

        builder.HasOne(e => e.ChecklistTemplate)
            .WithMany()
            .HasForeignKey(e => e.ChecklistTemplateId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Site)
            .WithMany()
            .HasForeignKey(e => e.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.WorkCenter)
            .WithMany()
            .HasForeignKey(e => e.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.ProductionLine)
            .WithMany()
            .HasForeignKey(e => e.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.OperatorUser)
            .WithMany()
            .HasForeignKey(e => e.OperatorUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(e => new { e.SiteId, e.WorkCenterId, e.StartedAtUtc });
        builder.HasIndex(e => e.ChecklistType);
    }
}

public class ChecklistEntryItemResponseConfiguration : IEntityTypeConfiguration<ChecklistEntryItemResponse>
{
    public void Configure(EntityTypeBuilder<ChecklistEntryItemResponse> builder)
    {
        builder.Property(r => r.ResponseValue).HasMaxLength(4000);
        builder.Property(r => r.Note).HasMaxLength(1000);

        builder.HasOne(r => r.ChecklistEntry)
            .WithMany(e => e.Responses)
            .HasForeignKey(r => r.ChecklistEntryId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(r => r.ChecklistTemplateItem)
            .WithMany()
            .HasForeignKey(r => r.ChecklistTemplateItemId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(r => new { r.ChecklistEntryId, r.ChecklistTemplateItemId })
            .IsUnique();
    }
}

public class ScoreTypeConfiguration : IEntityTypeConfiguration<ScoreType>
{
    public void Configure(EntityTypeBuilder<ScoreType> builder)
    {
        builder.Property(s => s.Name).HasMaxLength(120);

        builder.HasOne(s => s.CreatedByUser)
            .WithMany()
            .HasForeignKey(s => s.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ModifiedByUser)
            .WithMany()
            .HasForeignKey(s => s.ModifiedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.Name);
    }
}

public class ScoreTypeValueConfiguration : IEntityTypeConfiguration<ScoreTypeValue>
{
    public void Configure(EntityTypeBuilder<ScoreTypeValue> builder)
    {
        builder.Property(v => v.Description).HasMaxLength(240);

        builder.HasOne(v => v.ScoreType)
            .WithMany(s => s.Values)
            .HasForeignKey(v => v.ScoreTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(v => new { v.ScoreTypeId, v.SortOrder });
        builder.HasIndex(v => new { v.ScoreTypeId, v.Score, v.Description }).IsUnique();
    }
}
