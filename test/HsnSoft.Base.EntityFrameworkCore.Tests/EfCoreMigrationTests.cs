using HsnSoft.Base.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.EntityFrameworkCore.Tests;

public sealed class EfCoreMigrationTests : IDisposable
{
    private const string Connection = "EfCore-Migrated";

    private sealed class Tag
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    private sealed class MigratedContext(DbContextOptions<MigratedContext> options) : DbContext(options)
    {
        public DbSet<Tag> Tags => Set<Tag>();
    }

    // the two migrations of the context, in the order of their ids: the tag table, then a column
    [DbContext(typeof(MigratedContext))]
    [Migration("20260101000000_Initial")]
    private sealed class Initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => migrationBuilder.CreateTable("Tags", table => new { Id = table.Column<int>(nullable: false).Annotation("Sqlite:Autoincrement", true), Name = table.Column<string>(nullable: false) }, constraints: table => table.PrimaryKey("PK_Tags", column => column.Id));
    }

    private sealed class RecordingLock(List<string> log) : IStartupLock
    {
        public Task<IAsyncDisposable> AcquireAsync(string name, TimeSpan timeout, CancellationToken cancellationToken)
        {
            log.Add($"acquired {name}");
            return Task.FromResult<IAsyncDisposable>(new Release(log, name));
        }

        private sealed class Release(List<string> log, string name) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                log.Add($"released {name}");
                return ValueTask.CompletedTask;
            }
        }
    }

    // SQLite under a name, with the lock a provider of the framework would bring; the model is not compared with a snapshot (the test has none)
    private sealed class LockingProvider(List<string> log) : IEfCoreProvider, IStartupLockProvider
    {
        public string Name => "locking";

        public void Configure(DbContextOptionsBuilder options, string connectionString)
            => options.UseSqlite(connectionString).ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning));

        public IStartupLock CreateStartupLock(string connectionString) => new RecordingLock(log);
    }

    private readonly string _file = Path.Combine(Path.GetTempPath(), $"migrated-{Guid.NewGuid():N}.db");

    private readonly List<string> _log = [];

    public void Dispose()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }

    private ServiceProvider Build(string? seedStartupConnections = null, bool withStep = false)
    {
        Dictionary<string, string?> settings = new() { [$"ConnectionStrings:{Connection}"] = $"Data Source={_file}" };
        if (seedStartupConnections is not null)
        {
            settings[StartupDatabaseServiceCollectionExtensions.SeedConnectionsSetting] = seedStartupConnections;
        }

        ServiceCollection collection = new();
        collection.AddLogging().AddBaseEfCore<MigratedContext>(new ConfigurationBuilder().AddInMemoryCollection(settings).Build(), providers => providers.Add(new LockingProvider(_log)), "locking", Connection);
        if (withStep)
        {
            collection.AddMigrationStartupStep<MigratedContext>(Connection);
        }

        return collection.BuildServiceProvider();
    }

    [Fact]
    public async Task TheMigrations_ArePutInTheDatabase_UnderItsLock_AndRunningThemAgainChangesNothing()
    {
        await using ServiceProvider services = Build();

        await services.MigrateDatabaseAsync<MigratedContext>();
        await services.MigrateDatabaseAsync<MigratedContext>();

        Assert.Equal([$"acquired {Connection}", $"released {Connection}", $"acquired {Connection}", $"released {Connection}"], _log);
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        MigratedContext context = scope.ServiceProvider.GetRequiredService<MigratedContext>();
        context.Tags.Add(new Tag { Name = "a" });
        await context.SaveChangesAsync();
        Assert.Equal("20260101000000_Initial", Assert.Single(await context.Database.GetAppliedMigrationsAsync()));
        Assert.Empty(await context.Database.GetPendingMigrationsAsync());
    }

    [Fact]
    public async Task ADatabaseTheHostDoesNotOwn_IsNotMigrated()
    {
        await using ServiceProvider services = Build(seedStartupConnections: string.Empty);

        await services.MigrateDatabaseAsync<MigratedContext>();

        Assert.Empty(_log);
        Assert.False(File.Exists(_file));
    }

    [Fact]
    public async Task TheMigrationStep_IsAddedToThePersistence_AndRunsWhenTheHostStarts()
    {
        await using ServiceProvider services = Build(withStep: true);
        Assert.False(File.Exists(_file));

        // the health checks of the registration bring a hosted service of their own: the one of the steps is found by what it is
        await services.GetServices<IHostedService>().OfType<IHostedLifecycleService>().Single(service => service.GetType().Name == "DatabaseStartupService").StartingAsync(CancellationToken.None);

        Assert.True(File.Exists(_file));
        Assert.Equal([$"acquired {Connection}", $"released {Connection}"], _log);
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        Assert.Single(await scope.ServiceProvider.GetRequiredService<MigratedContext>().Database.GetAppliedMigrationsAsync());
    }

    [Fact]
    public async Task NullArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => DbContextExtensions.AddMigrationStartupStep<MigratedContext>(null!, Connection));
        await Assert.ThrowsAsync<ArgumentNullException>(() => DbContextExtensions.MigrateDatabaseAsync<MigratedContext>(null!));
    }
}
