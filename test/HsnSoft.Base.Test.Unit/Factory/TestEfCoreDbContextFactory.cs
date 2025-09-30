using HsnSoft.Base.Test.Unit.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HsnSoft.Base.Test.Unit.Factory;

internal sealed class TestEfCoreDbContextFactory : IDesignTimeDbContextFactory<TestEfCoreDbContext>
{
    public TestEfCoreDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TestEfCoreDbContext>()
            .UseNpgsql("Host=localhost;Port=5432;Database=TEST_Performance_Data;User ID=postgres;Password=postgres;Pooling=true;Connection Lifetime=0;", b =>
            {
                b.MigrationsHistoryTable("__EFMigrationsHistory");
                b.MigrationsAssembly(typeof(TestEfCoreDbContext).Assembly.GetName().Name);
            });

        return new TestEfCoreDbContext(builder.Options);
    }
}