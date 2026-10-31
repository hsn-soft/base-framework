using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace HsnSoft.Base.Data.Tests;

public class DatabaseStartupServiceTests
{
    private sealed class Marker;

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

    private static ServiceProvider Build(List<string> log, string? seedStartupConnections, Action<IServiceCollection> steps)
    {
        Dictionary<string, string?> settings = [];
        if (seedStartupConnections is not null)
        {
            settings[StartupDatabaseServiceCollectionExtensions.SeedConnectionsSetting] = seedStartupConnections;
        }

        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        ServiceCollection services = new();
        services.AddLogging();
        services.AddScoped<Marker>();
        services.AddStartupDatabase(configuration, "Db-A", _ => new RecordingLock(log));
        services.AddStartupDatabase(configuration, "Db-B", _ => new RecordingLock(log));
        steps(services);
        return services.BuildServiceProvider();
    }

    private static Task StartingAsync(IServiceProvider services) => Assert.IsAssignableFrom<IHostedLifecycleService>(Assert.Single(services.GetServices<IHostedService>())).StartingAsync(CancellationToken.None);

    [Fact]
    public async Task TheSteps_RunInTheOrderTheyWereAdded_EachInItsDatabaseLock_AndInAScopeOfItsOwn()
    {
        List<string> log = [];
        Marker? first = null;
        await using ServiceProvider services = Build(log, null, collection => collection
            .AddDatabaseStartupStep("Db-A", "schema", (scope, _) =>
            {
                first = scope.GetRequiredService<Marker>();
                log.Add("schema");
                return Task.CompletedTask;
            })
            .AddDatabaseStartupStep("Db-B", "indexes", (scope, _) =>
            {
                log.Add(ReferenceEquals(first, scope.GetRequiredService<Marker>()) ? "indexes (same scope)" : "indexes (new scope)");
                return Task.CompletedTask;
            })
            .AddDatabaseStartupStep("Db-A", "seed", (_, _) =>
            {
                log.Add("seed");
                return Task.CompletedTask;
            }));

        await StartingAsync(services);

        Assert.Equal(
            ["acquired Db-A", "schema", "released Db-A", "acquired Db-B", "indexes (new scope)", "released Db-B", "acquired Db-A", "seed", "released Db-A"],
            log);
    }

    [Fact]
    public async Task ADatabaseTheHostDoesNotOwn_HasItsStepsSkipped_TheOthersRun()
    {
        List<string> log = [];
        await using ServiceProvider services = Build(log, "Db-B", collection => collection
            .AddDatabaseStartupStep("Db-A", "schema", (_, _) =>
            {
                log.Add("schema of A");
                return Task.CompletedTask;
            })
            .AddDatabaseStartupStep("Db-B", "schema", (_, _) =>
            {
                log.Add("schema of B");
                return Task.CompletedTask;
            }));

        await StartingAsync(services);

        Assert.Equal(["acquired Db-B", "schema of B", "released Db-B"], log);
    }

    [Fact]
    public async Task ManyStepsAreOneHostedService_AndItDoesNothingButInStarting()
    {
        List<string> log = [];
        await using ServiceProvider services = Build(log, null, collection => collection
            .AddDatabaseStartupStep("Db-A", "one", (_, _) => Task.CompletedTask)
            .AddDatabaseStartupStep("Db-A", "two", (_, _) => Task.CompletedTask));
        IHostedLifecycleService service = Assert.IsAssignableFrom<IHostedLifecycleService>(Assert.Single(services.GetServices<IHostedService>()));

        await service.StartAsync(CancellationToken.None);
        await service.StartedAsync(CancellationToken.None);
        await service.StoppingAsync(CancellationToken.None);
        await service.StopAsync(CancellationToken.None);
        await service.StoppedAsync(CancellationToken.None);

        Assert.Empty(log);
    }

    [Fact]
    public void NullAndBlankArguments_AreRejected()
    {
        ServiceCollection services = new();

        Assert.Throws<ArgumentNullException>(() => DatabaseStartupServiceCollectionExtensions.AddDatabaseStartupStep(null!, "Db", "x", (_, _) => Task.CompletedTask));
        Assert.Throws<ArgumentException>(() => services.AddDatabaseStartupStep(" ", "x", (_, _) => Task.CompletedTask));
        Assert.Throws<ArgumentException>(() => services.AddDatabaseStartupStep("Db", " ", (_, _) => Task.CompletedTask));
        Assert.Throws<ArgumentNullException>(() => services.AddDatabaseStartupStep("Db", "x", null!));
    }
}
