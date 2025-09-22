using HsnSoft.Base.Test.Api.EfCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HsnSoft.Base.Test.Api.EfCore.Factory;

internal sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppEfCoreDbContext>
{
    public AppEfCoreDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<AppEfCoreDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=TEST_Performance_Data;User ID=postgres;Password=postgres;Pooling=true;Connection Lifetime=0;", b =>
            {
                b.MigrationsHistoryTable("__EFMigrationsHistory");
                b.MigrationsAssembly(typeof(AppEfCoreDbContext).Assembly.GetName().Name);
            });

        return new AppEfCoreDbContext(builder.Options);
    }
}