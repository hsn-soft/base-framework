using HsnSoft.Base.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.EntityFrameworkCore.Tests;

public sealed class EfCoreStartupTests : IDisposable
{
    private const string Connection = "EfCore-Startup";

    private sealed class Note
    {
        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;
    }

    private sealed class StartupContext(DbContextOptions<StartupContext> options) : DbContext(options)
    {
        public DbSet<Note> Notes => Set<Note>();
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

    // SQLite under a name, with the lock a provider of the framework would bring
    private sealed class LockingProvider(List<string> log) : IEfCoreProvider, IStartupLockProvider
    {
        public string Name => "locking";

        public void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseSqlite(connectionString);

        public IStartupLock CreateStartupLock(string connectionString) => new RecordingLock(log);
    }

    private sealed class PlainProvider : IEfCoreProvider
    {
        public string Name => "plain";

        public void Configure(DbContextOptionsBuilder options, string connectionString) => options.UseSqlite(connectionString);
    }

    private readonly string _file = Path.Combine(Path.GetTempPath(), $"startup-{Guid.NewGuid():N}.db");

    private readonly List<string> _log = [];

    public void Dispose()
    {
        SqliteCleanup();
    }

    private void SqliteCleanup()
    {
        Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
        File.Delete(_file);
    }

    private ServiceProvider Build(IEfCoreProvider provider, string? seedStartupConnections = null)
    {
        Dictionary<string, string?> settings = new() { [$"ConnectionStrings:{Connection}"] = $"Data Source={_file}" };
        if (seedStartupConnections is not null)
        {
            settings[StartupDatabaseServiceCollectionExtensions.SeedConnectionsSetting] = seedStartupConnections;
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        return new ServiceCollection().AddLogging().AddBaseEfCore<StartupContext>(configuration, providers => providers.Add(provider), provider.Name, Connection).BuildServiceProvider();
    }

    [Fact]
    public void TheRegistration_TellsStartupAboutTheDatabase_ByTheNameOfItsConnection()
    {
        using ServiceProvider owner = Build(new LockingProvider(_log));
        using ServiceProvider visitor = Build(new LockingProvider(_log), seedStartupConnections: "EfCore-Other");

        StartupDatabase ownerOf = Assert.Single(owner.GetServices<StartupDatabase>());
        StartupDatabase visitorOf = Assert.Single(visitor.GetServices<StartupDatabase>());

        Assert.Equal(Connection, ownerOf.Name);
        Assert.True(ownerOf.Owned);
        Assert.False(visitorOf.Owned);
        Assert.Equal(Connection, owner.GetRequiredService<StartupContext<StartupContext>>().ConnectionName);
        Assert.NotNull(ownerOf.CreateLock(owner));
    }

    [Fact]
    public async Task TheSchemaAndTheSeed_AreStepsOfTheDatabase_UnderItsLock()
    {
        await using ServiceProvider services = Build(new LockingProvider(_log));

        await services.EnsureDatabaseCreatedAsync<StartupContext>();
        await services.RunContextStepAsync<StartupContext>("seed", async (context, cancellationToken) =>
        {
            context.Notes.Add(new Note { Text = "seed" });
            await context.SaveChangesAsync(cancellationToken);
        });

        Assert.Equal([$"acquired {Connection}", $"released {Connection}", $"acquired {Connection}", $"released {Connection}"], _log);
        await using AsyncServiceScope scope = services.CreateAsyncScope();
        Assert.Equal("seed", (await scope.ServiceProvider.GetRequiredService<StartupContext>().Notes.SingleAsync()).Text);
    }

    [Fact]
    public async Task ADatabaseTheHostDoesNotOwn_IsNotCreatedOrSeeded_AndItsLockIsNeverTaken()
    {
        await using ServiceProvider services = Build(new LockingProvider(_log), seedStartupConnections: string.Empty);
        bool seeded = false;

        await services.EnsureDatabaseCreatedAsync<StartupContext>();
        await services.RunContextStepAsync<StartupContext>("seed", (_, _) =>
        {
            seeded = true;
            return Task.CompletedTask;
        });

        Assert.False(seeded);
        Assert.Empty(_log);
        Assert.False(File.Exists(_file));
    }

    [Fact]
    public async Task AScopedStep_GetsTheServicesOfTheScopeItsContextIsIn()
    {
        await using ServiceProvider services = Build(new LockingProvider(_log));
        await services.EnsureDatabaseCreatedAsync<StartupContext>();
        StartupContext? first = null;
        StartupContext? second = null;

        await services.RunScopedContextStepAsync<StartupContext>("scoped", (scope, _) =>
        {
            first = scope.GetRequiredService<StartupContext>();
            second = scope.GetRequiredService<StartupContext>();
            return Task.CompletedTask;
        });

        Assert.NotNull(first);
        Assert.Same(first, second);
    }

    [Fact]
    public async Task AProviderWithNoLock_RunsTheStepWithoutOne()
    {
        await using ServiceProvider services = Build(new PlainProvider());

        await services.EnsureDatabaseCreatedAsync<StartupContext>();

        Assert.Null(Assert.Single(services.GetServices<StartupDatabase>()).CreateLock(services));
        Assert.True(File.Exists(_file));
    }

    [Fact]
    public async Task AContextThatStartupDoesNotKnow_IsPreparedLikeBefore()
    {
        ServiceCollection collection = new();
        collection.AddLogging().AddDbContext<StartupContext>(options => options.UseSqlite($"Data Source={_file}"));
        await using ServiceProvider services = collection.BuildServiceProvider();

        await services.EnsureDatabaseCreatedAsync<StartupContext>();

        Assert.True(File.Exists(_file));
    }

    [Fact]
    public async Task TheSchemaStep_IsAddedToThePersistence_AndRunsWhenTheHostStarts()
    {
        ServiceCollection collection = new();
        collection.AddLogging();
        collection.AddBaseEfCore<StartupContext>(
            new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { [$"ConnectionStrings:{Connection}"] = $"Data Source={_file}" }).Build(),
            providers => providers.Add(new LockingProvider(_log)),
            "locking",
            Connection);
        collection.AddSchemaStartupStep<StartupContext>(Connection);
        await using ServiceProvider services = collection.BuildServiceProvider();
        Assert.False(File.Exists(_file));

        // the health checks of the registration bring a hosted service of their own: the one of the steps is found by what it is
        await services.GetServices<IHostedService>().OfType<IHostedLifecycleService>().Single(service => service.GetType().Name == "DatabaseStartupService").StartingAsync(CancellationToken.None);

        Assert.True(File.Exists(_file));
        Assert.Equal([$"acquired {Connection}", $"released {Connection}"], _log);
        Assert.Throws<ArgumentNullException>(() => DbContextExtensions.AddSchemaStartupStep<StartupContext>(null!, Connection));
    }

    [Fact]
    public async Task NullAndBlankArguments_AreRejected()
    {
        await using ServiceProvider services = Build(new PlainProvider());

        await Assert.ThrowsAsync<ArgumentNullException>(() => DbContextExtensions.RunContextStepAsync<StartupContext>(null!, "x", (_, _) => Task.CompletedTask));
        await Assert.ThrowsAsync<ArgumentException>(() => services.RunContextStepAsync<StartupContext>(" ", (_, _) => Task.CompletedTask));
        await Assert.ThrowsAsync<ArgumentNullException>(() => services.RunContextStepAsync<StartupContext>("x", null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => services.RunScopedContextStepAsync<StartupContext>("x", null!));
    }
}
