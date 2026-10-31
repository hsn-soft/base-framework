using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.Data.Tests;

public class DataSeederTests
{
    private sealed class Journal(List<string> entries)
    {
        public void Add(string entry) => entries.Add(entry);
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

    private sealed class TenantsSeeder(Journal journal) : IDataSeeder
    {
        public Task SeedAsync(CancellationToken cancellationToken)
        {
            journal.Add("tenants");
            return Task.CompletedTask;
        }
    }

    private sealed class SampleIdentitySeeder(Journal journal) : IDataSeeder
    {
        public Task SeedAsync(CancellationToken cancellationToken)
        {
            journal.Add("sample identity");
            return Task.CompletedTask;
        }
    }

    private static ServiceProvider Build(List<string> log, Dictionary<string, string?> settings)
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddSingleton(configuration);
        services.AddSingleton(new Journal(log));
        services.AddStartupDatabase(configuration, "Db-A", _ => new RecordingLock(log));
        services.AddDatabaseStartupStep("Db-A", "schema", (_, _) =>
        {
            log.Add("schema");
            return Task.CompletedTask;
        });
        services.AddDataSeeder<TenantsSeeder>("Db-A").AddDataSeeder<SampleIdentitySeeder>("Db-A");
        return services.BuildServiceProvider();
    }

    private static Task StartingAsync(IServiceProvider services) => Assert.IsAssignableFrom<IHostedLifecycleService>(Assert.Single(services.GetServices<IHostedService>())).StartingAsync(CancellationToken.None);

    [Fact]
    public async Task TheSeeders_RunAfterTheSchema_InTheOrderTheyWereAdded_EachInTheLockOfTheDatabase_WithWhatTheyTake()
    {
        List<string> log = [];
        await using ServiceProvider services = Build(log, []);

        await StartingAsync(services);

        Assert.Equal(["acquired Db-A", "schema", "released Db-A", "acquired Db-A", "tenants", "released Db-A", "acquired Db-A", "sample identity", "released Db-A"], log);
    }

    [Fact]
    public async Task AHostThatDoesNotOwnTheDatabase_RunsNoSeeder()
    {
        List<string> log = [];
        await using ServiceProvider services = Build(log, new() { [StartupDatabaseServiceCollectionExtensions.SeedConnectionsSetting] = "Db-Other" });

        await StartingAsync(services);

        Assert.Empty(log);
    }

    [Fact]
    public async Task ASeederThatTheSettingsDisable_IsNotRun_ByTheNameOfItsClass_AndTheOthersAre()
    {
        List<string> log = [];
        await using ServiceProvider services = Build(log, new() { [$"{DataSeederServiceCollectionExtensions.DisabledSetting}:0"] = " sampleidentityseeder " });

        await StartingAsync(services);

        Assert.Contains("tenants", log);
        Assert.DoesNotContain("sample identity", log);
    }

    [Fact]
    public async Task WithNoConfigurationInTheContainer_TheSeederRuns()
    {
        List<string> log = [];
        ServiceCollection collection = new();
        collection.AddLogging();
        collection.AddSingleton(new Journal(log));
        collection.AddDataSeeder<TenantsSeeder>("Db-A");
        await using ServiceProvider services = collection.BuildServiceProvider();

        await StartingAsync(services);

        Assert.Equal(["tenants"], log);
    }

    [Fact]
    public void NullAndBlankArguments_AreRejected()
    {
        Assert.Throws<ArgumentNullException>(() => DataSeederServiceCollectionExtensions.AddDataSeeder<TenantsSeeder>(null!, "Db-A"));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddDataSeeder<TenantsSeeder>(" "));
    }
}
