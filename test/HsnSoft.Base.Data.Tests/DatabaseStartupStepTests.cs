using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Data.Tests;

public class DatabaseStartupStepTests
{
    private const string Database = "EfCore-IdentityService";

    /// <summary>A lock that one holder has at a time, and a log of who got it and gave it back.</summary>
    private sealed class FakeLock : IStartupLock, IDisposable
    {
        private readonly SemaphoreSlim _free = new(1, 1);
        private int _holders;

        public List<string> Log { get; } = [];

        public void Dispose() => _free.Dispose();

        public int MostHolders { get; private set; }

        public int Failures { get; set; }

        public async Task<IAsyncDisposable> AcquireAsync(string name, TimeSpan timeout, CancellationToken cancellationToken)
        {
            if (Failures > 0)
            {
                Failures--;
                throw new InvalidOperationException("the server is not there yet");
            }

            await _free.WaitAsync(timeout, cancellationToken);
            lock (Log)
            {
                Log.Add($"acquired {name}");
                MostHolders = Math.Max(MostHolders, ++_holders);
            }

            return new Held(this, name);
        }

        private sealed class Held(FakeLock owner, string name) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                lock (owner.Log)
                {
                    owner.Log.Add($"released {name}");
                    owner._holders--;
                }

                owner._free.Release();
                return ValueTask.CompletedTask;
            }
        }
    }

    private static ServiceProvider Services(string? owns, IStartupLock? startupLock, bool register = true)
    {
        Dictionary<string, string?> settings = [];
        if (owns is not null)
        {
            settings["SeedStartupConnections"] = owns;
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ServiceCollection services = new();
        services.AddLogging();
        if (register)
        {
            services.AddStartupDatabase(configuration, Database, _ => startupLock);
        }

        return services.BuildServiceProvider();
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("", false)]
    [InlineData("EfCore-IdentityService", true)]
    [InlineData("efcore-identityservice", true)]
    [InlineData("Mongo-X; EfCore-IdentityService", true)]
    [InlineData(" EfCore-IdentityService ;Mongo-X", true)]
    [InlineData("Mongo-X;EfCore-HistoryService", false)]
    [InlineData("EfCore-IdentityService,Mongo-X", false)]
    public void OwnershipIsWhatTheSettingNames_AllWhenItIsAbsent_NoneWhenItIsEmpty(string? owns, bool expected)
    {
        using ServiceProvider services = Services(owns, new FakeLock());

        Assert.Equal(expected, services.GetServices<StartupDatabase>().Single().Owned);
    }

    [Fact]
    public async Task ADatabaseTheHostDoesNotOwn_IsNotPrepared_AndItsLockIsNeverTaken()
    {
        FakeLock startupLock = new();
        await using ServiceProvider services = Services("Mongo-X", startupLock);
        bool ran = false;

        await services.RunDatabaseStepAsync(Database, "seed", _ =>
        {
            ran = true;
            return Task.CompletedTask;
        });

        Assert.False(ran);
        Assert.Empty(startupLock.Log);
    }

    [Fact]
    public async Task TheStep_RunsUnderTheLockOfItsDatabase_AndTheLockIsGivenBack()
    {
        FakeLock startupLock = new();
        await using ServiceProvider services = Services(Database, startupLock);

        await services.RunDatabaseStepAsync(Database, "seed", _ =>
        {
            startupLock.Log.Add("step");
            return Task.CompletedTask;
        });

        Assert.Equal([$"acquired {Database}", "step", $"released {Database}"], startupLock.Log);
    }

    [Fact]
    public async Task TheLockIsGivenBack_WhenTheStepFails_AndTheStepIsTriedAgain()
    {
        FakeLock startupLock = new();
        await using ServiceProvider services = Services(null, startupLock);
        int attempts = 0;

        await services.RunDatabaseStepAsync(Database, "seed", _ => ++attempts < 3 ? throw new InvalidOperationException("the table is not there yet") : Task.CompletedTask, retryDelay: TimeSpan.Zero);

        Assert.Equal(3, attempts);
        Assert.Equal(3, startupLock.Log.Count(entry => entry.StartsWith("released", StringComparison.Ordinal)));
    }

    [Fact]
    public async Task AServerThatIsNotThereYet_IsWaitedFor_BecauseTakingTheLockIsPartOfTheAttempt()
    {
        FakeLock startupLock = new() { Failures = 2 };
        await using ServiceProvider services = Services(null, startupLock);
        bool ran = false;

        await services.RunDatabaseStepAsync(Database, "seed", _ =>
        {
            ran = true;
            return Task.CompletedTask;
        }, retryDelay: TimeSpan.Zero);

        Assert.True(ran);
    }

    [Fact]
    public async Task ManyPodsAtOnce_RunTheStepOneAfterTheOther_EachOfThem()
    {
        FakeLock startupLock = new();
        await using ServiceProvider services = Services(Database, startupLock);
        int running = 0;
        int most = 0;
        int done = 0;

        await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => services.RunDatabaseStepAsync(Database, "seed", async cancellationToken =>
        {
            most = Math.Max(most, Interlocked.Increment(ref running));
            await Task.Delay(20, cancellationToken);
            Interlocked.Decrement(ref running);
            Interlocked.Increment(ref done);
        })));

        Assert.Equal(1, most);
        Assert.Equal(1, startupLock.MostHolders);
        Assert.Equal(8, done);
    }

    [Fact]
    public async Task ADatabaseWithNoLock_OrNotKnownToStartup_IsOnlyRetried()
    {
        await using ServiceProvider noLock = Services(null, null);
        await using ServiceProvider unknown = Services(null, null, register: false);
        int runs = 0;

        await noLock.RunDatabaseStepAsync(Database, "seed", _ => { runs++; return Task.CompletedTask; });
        await unknown.RunDatabaseStepAsync(Database, "seed", _ => { runs++; return Task.CompletedTask; });

        Assert.Equal(2, runs);
    }

    [Fact]
    public async Task NullAndBlankArguments_AreRejected()
    {
        await using ServiceProvider services = Services(null, null);

        await Assert.ThrowsAsync<ArgumentNullException>(() => DatabaseStartupSteps.RunDatabaseStepAsync(null!, Database, "x", _ => Task.CompletedTask));
        await Assert.ThrowsAsync<ArgumentException>(() => DatabaseStartupSteps.RunDatabaseStepAsync(services, " ", "x", _ => Task.CompletedTask));
        await Assert.ThrowsAsync<ArgumentException>(() => DatabaseStartupSteps.RunDatabaseStepAsync(services, Database, " ", _ => Task.CompletedTask));
        await Assert.ThrowsAsync<ArgumentNullException>(() => DatabaseStartupSteps.RunDatabaseStepAsync(services, Database, "x", null!));
        Assert.Throws<ArgumentNullException>(() => StartupDatabaseServiceCollectionExtensions.AddStartupDatabase(null!, new ConfigurationBuilder().Build(), Database, _ => null));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddStartupDatabase(null!, Database, _ => null));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddStartupDatabase(new ConfigurationBuilder().Build(), " ", _ => null));
        Assert.Throws<ArgumentNullException>(() => new ServiceCollection().AddStartupDatabase(new ConfigurationBuilder().Build(), Database, null!));
    }
}
