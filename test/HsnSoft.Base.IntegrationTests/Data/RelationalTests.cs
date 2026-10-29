using HsnSoft.Base.EntityFrameworkCore;
using HsnSoft.Base.EntityFrameworkCore.MySql;
using HsnSoft.Base.EntityFrameworkCore.PostgreSql;
using HsnSoft.Base.EntityFrameworkCore.SqlServer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Testcontainers.MsSql;
using Testcontainers.MySql;
using Testcontainers.PostgreSql;

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

public sealed class PostgreSqlTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => Support.DisposeAsync(_container);

    [Fact]
    public Task DbContext_CreatesItsSchema_StoresRows_AndTheServiceIsReady()
    {
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Postgres", _container.GetConnectionString()));
        return RelationalChecks.AssertWorksAsync(services => services.AddBasePostgres<ItemContext>(configuration));
    }
}

public sealed class SqlServerTests : IAsyncLifetime
{
    private readonly MsSqlContainer _container = new MsSqlBuilder("mcr.microsoft.com/mssql/server:2022-latest").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => Support.DisposeAsync(_container);

    [Fact]
    public Task DbContext_CreatesItsSchema_StoresRows_AndTheServiceIsReady()
    {
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:SqlServer", _container.GetConnectionString()));
        return RelationalChecks.AssertWorksAsync(services => services.AddBaseSqlServer<ItemContext>(configuration));
    }
}

public sealed class MySqlTests : IAsyncLifetime
{
    private readonly MySqlContainer _container = new MySqlBuilder("mysql:8.4").Build();

    public Task InitializeAsync() => _container.StartAsync();

    public Task DisposeAsync() => Support.DisposeAsync(_container);

    [Fact]
    public Task DbContext_CreatesItsSchema_StoresRows_AndTheServiceIsReady()
    {
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:MySql", _container.GetConnectionString()));
        return RelationalChecks.AssertWorksAsync(services => services.AddBaseMySql<ItemContext>(configuration));
    }
}
