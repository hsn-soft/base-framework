using Hhs.IdentityService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hhs.IdentityService.EntityFrameworkCore.Factory;

internal sealed class AuthServiceDbContextFactory : IDesignTimeDbContextFactory<AuthServiceDbContext>
{
    public AuthServiceDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AuthServiceDbContext>()
            .UseNpgsql(DbContextFactoryHelper.GetConnectionStringFromConfiguration(), b =>
            {
                b.MigrationsHistoryTable("__EFMigrationsHistory");
                b.MigrationsAssembly(typeof(AuthServiceDbContext).Assembly.GetName().Name);
            });

        return new AuthServiceDbContext(null, builder.Options);
    }
}