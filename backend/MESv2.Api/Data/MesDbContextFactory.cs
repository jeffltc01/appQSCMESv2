using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MESv2.Api.Data;

/// <summary>
/// Used by 'dotnet ef migrations' commands at design time.
/// Targets SQL Server so migration snapshots match the test/prod provider.
/// No actual database connection is needed for generating migrations.
/// </summary>
public class MesDbContextFactory : IDesignTimeDbContextFactory<MesDbContext>
{
    public MesDbContext CreateDbContext(string[] args)
    {
        var options = new DbContextOptionsBuilder<MesDbContext>()
            .UseSqlServer("Server=.;Database=MESv2_DesignTime;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new MesDbContext(options);
    }
}
