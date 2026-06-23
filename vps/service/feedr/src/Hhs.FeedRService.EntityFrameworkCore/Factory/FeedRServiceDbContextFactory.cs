using Hhs.FeedRService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hhs.FeedRService.EntityFrameworkCore.Factory;

internal sealed class FeedRServiceDbContextFactory : IDesignTimeDbContextFactory<FeedRServiceDbContext>
{
    public FeedRServiceDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<FeedRServiceDbContext>()
            .UseNpgsql(DbContextFactoryHelper.GetConnectionStringFromConfiguration(), b =>
            {
                b.MigrationsHistoryTable("__EFMigrationsHistory");
                b.MigrationsAssembly(typeof(FeedRServiceDbContext).Assembly.GetName().Name);
            });

        return new FeedRServiceDbContext(null, builder.Options);
    }
}