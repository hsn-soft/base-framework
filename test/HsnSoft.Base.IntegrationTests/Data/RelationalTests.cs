using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.MySql;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using HsnSoft.Base.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HsnSoft.Base.IntegrationTests.Data;

public sealed class Item
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class ItemContext(DbContextOptions<ItemContext> options) : DbContext(options)
{
    public DbSet<Item> Items => Set<Item>();
}

internal static class RelationalChecks
{
    public static async Task AssertWorksAsync(Action<IServiceCollection> register)
    {
        using IHost host = await Support.StartHostAsync(register);

        await host.Services.EnsureDatabaseCreatedAsync<ItemContext>();

        await using (AsyncServiceScope scope = host.Services.CreateAsyncScope())
        {
            ItemContext context = scope.ServiceProvider.GetRequiredService<ItemContext>();
            context.Items.Add(new Item { Name = "first" });
            await context.SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = host.Services.CreateAsyncScope())
        {
            Assert.Equal("first", await scope.ServiceProvider.GetRequiredService<ItemContext>().Items.Select(x => x.Name).SingleAsync());
        }

        Assert.Equal(HealthStatus.Healthy, (await Support.ReadyReportAsync(host.Services)).Status);
    }
}

[Collection(Collections.Postgres)]
public sealed class PostgreSqlTests(PostgresFixture postgres)
{
    [Fact]
    public Task DbContext_CreatesItsSchema_StoresRows_AndTheServiceIsReady()
    {
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Postgres", postgres.NewConnectionString()));
        return RelationalChecks.AssertWorksAsync(services => services.AddBasePostgres<ItemContext>(configuration));
    }
}

[Collection(Collections.SqlServer)]
public sealed class SqlServerTests(SqlServerFixture sqlServer)
{
    [Fact]
    public Task DbContext_CreatesItsSchema_StoresRows_AndTheServiceIsReady()
    {
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:SqlServer", sqlServer.NewConnectionString()));
        return RelationalChecks.AssertWorksAsync(services => services.AddBaseSqlServer<ItemContext>(configuration));
    }
}

[Collection(Collections.MySql)]
public sealed class MySqlTests(MySqlFixture mySql)
{
    [Fact]
    public Task DbContext_CreatesItsSchema_StoresRows_AndTheServiceIsReady()
    {
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:MySql", mySql.NewConnectionString()));
        return RelationalChecks.AssertWorksAsync(services => services.AddBaseMySql<ItemContext>(configuration));
    }
}
