using HsnSoft.Base.Test.Unit.Models;
using Microsoft.EntityFrameworkCore;

namespace HsnSoft.Base.Test.Unit.Fixtures;

public class PostgresFixture : IDisposable
{
    public TestEfCoreDbContext Context { get; private set; }

    private const string ConnectionString =
        "Host=localhost;Port=5432;Database=TEST_Performance_Data;User ID=postgres;Password=postgres;Pooling=true;Connection Lifetime=0;";

    public PostgresFixture()
    {
        var options = new DbContextOptionsBuilder<TestEfCoreDbContext>()
            .UseNpgsql(ConnectionString)
            .Options;

        Context = new TestEfCoreDbContext(options);

        if (Context.Database.CanConnectAsync().GetAwaiter().GetResult())
        {
            if (Context.Database.GetPendingMigrations().Any())
            {
                // apply pending migrations
                Context.Database.Migrate();
            }
        }
        else
        {
            // first creation
            Context.Database.Migrate();
        }

        // Cleaning before testing
        CleanDatabase().GetAwaiter().GetResult();
    }

    public void Dispose()
    {
        Context?.Dispose();
    }

    public async Task CleanDatabase()
    {
        // Cleaning before testing
        await Context.Database.ExecuteSqlRawAsync("TRUNCATE TABLE \"Tests\" RESTART IDENTITY CASCADE;");
    }
}