using HsnSoft.Base.Test.Integration.Models;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace HsnSoft.Base.Test.Integration.Fixtures;

public class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();

    public TestEfCoreDbContext Context { get; private set; }

    public async Task InitializeAsync()
    {
        await _container.StartAsync();

        var options = new DbContextOptionsBuilder<TestEfCoreDbContext>()
            .UseNpgsql(_container.GetConnectionString())
            .Options;

        Context = new TestEfCoreDbContext(options);
        await Context.Database.MigrateAsync();
    }

    public async Task DisposeAsync()
    {
        if (Context != null)
        {
            await Context.DisposeAsync();
        }

        await _container.DisposeAsync();
    }

    public async Task CleanDatabase()
    {
        await Context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Tests\" RESTART IDENTITY CASCADE;");
    }
}
