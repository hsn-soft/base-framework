using Hhs.ContentService.EntityFrameworkCore.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Hhs.ContentService.EntityFrameworkCore.Factory;

internal sealed class ContentServiceDbContextFactory : IDesignTimeDbContextFactory<ContentServiceDbContext>
{
    public ContentServiceDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ContentServiceDbContext>()
            .UseNpgsql(DbContextFactoryHelper.GetConnectionStringFromConfiguration(), b =>
            {
                b.MigrationsHistoryTable("__EFMigrationsHistory");
                b.MigrationsAssembly(typeof(ContentServiceDbContext).Assembly.GetName().Name);
            });

        return new ContentServiceDbContext(null, builder.Options);
    }
}