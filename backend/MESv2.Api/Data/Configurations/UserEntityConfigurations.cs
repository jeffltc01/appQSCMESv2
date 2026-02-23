using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MESv2.Api.Models;

namespace MESv2.Api.Data.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasOne(u => u.DefaultSite)
            .WithMany(p => p.Users)
            .HasForeignKey(u => u.DefaultSiteId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(u => u.EmployeeNumber).IsUnique();
    }
}

public class ActiveSessionConfiguration : IEntityTypeConfiguration<ActiveSession>
{
    public void Configure(EntityTypeBuilder<ActiveSession> builder)
    {
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.WorkCenter)
            .WithMany()
            .HasForeignKey(s => s.WorkCenterId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.ProductionLine)
            .WithMany()
            .HasForeignKey(s => s.ProductionLineId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(s => s.Asset)
            .WithMany()
            .HasForeignKey(s => s.AssetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(s => s.UserId);
        builder.HasIndex(s => s.PlantId);
    }
}

public class IssueRequestConfiguration : IEntityTypeConfiguration<IssueRequest>
{
    public void Configure(EntityTypeBuilder<IssueRequest> builder)
    {
        builder.HasOne(ir => ir.SubmittedByUser)
            .WithMany()
            .HasForeignKey(ir => ir.SubmittedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(ir => ir.ReviewedByUser)
            .WithMany()
            .HasForeignKey(ir => ir.ReviewedByUserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(ir => ir.Status);
        builder.HasIndex(ir => ir.SubmittedByUserId);
    }
}
