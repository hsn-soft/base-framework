using Hhs.AdministrationService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hhs.AdministrationService.EntityFrameworkCore.Factory;

internal sealed class AdministrationServiceDbContextFactory : IDesignTimeDbContextFactory<AdministrationServiceDbContext>
{
    public AdministrationServiceDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AdministrationServiceDbContext>()
            .UseNpgsql(DbContextFactoryHelper.GetConnectionStringFromConfiguration(), b =>
            {
                b.MigrationsHistoryTable("__EFMigrationsHistory");
                b.MigrationsAssembly(typeof(AdministrationServiceDbContext).Assembly.GetName().Name);
            });

        return new AdministrationServiceDbContext(null, builder.Options);
    }
}