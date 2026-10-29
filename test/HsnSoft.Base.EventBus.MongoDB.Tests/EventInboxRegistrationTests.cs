using HsnSoft.Base.EventBus.Inbox;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;

namespace HsnSoft.Base.EventBus.MongoDB.Tests;

public sealed class EventInboxRegistrationTests
{
    [Fact]
    public void TheRegistration_GivesTheServiceItsStoreOnTheCollectionItNames()
    {
        // the database is only a handle here: nothing is read or written when the store is made
        IMongoDatabase database = new MongoClient("mongodb://localhost:1").GetDatabase("unit");
        ServiceCollection services = new();
        services.AddSingleton(database);
        services.AddEventInbox("OrderInbox", configure: options => options.MaxRetryCount = 7);
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<MongoInboxStore>(scope.ServiceProvider.GetRequiredService<IInboxStore>());
        Assert.NotNull(provider.GetService<InboxEnabled>());
        Assert.Equal(7, provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EventInboxOptions>>().Value.MaxRetryCount);
    }

    [Fact]
    public void ADatabaseUnderAKey_IsUsedWhenTheKeyIsGiven()
    {
        IMongoDatabase keyed = new MongoClient("mongodb://localhost:1").GetDatabase("keyed");
        ServiceCollection services = new();
        services.AddKeyedSingleton("stock", keyed);
        services.AddEventInbox(serviceKey: "stock");
        using ServiceProvider provider = services.BuildServiceProvider();
        using IServiceScope scope = provider.CreateScope();

        Assert.IsType<MongoInboxStore>(scope.ServiceProvider.GetRequiredService<IInboxStore>());
    }

    [Fact]
    public void TheCleanup_IsARegistrationOfItsOwn_WithItsSettings()
    {
        IMongoDatabase database = new MongoClient("mongodb://localhost:1").GetDatabase("unit");
        ServiceCollection services = new();
        services.AddSingleton(database);
        services.AddSingleton(typeof(Microsoft.Extensions.Logging.ILogger<>), typeof(Microsoft.Extensions.Logging.Abstractions.NullLogger<>));
        services.AddEventInbox();
        using (ServiceProvider without = services.BuildServiceProvider())
        {
            Assert.Empty(without.GetServices<Microsoft.Extensions.Hosting.IHostedService>());
        }

        services.AddEventInboxCleanup(configure: options => options.RetentionPeriod = TimeSpan.FromDays(30));
        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.IsType<MongoInboxRetention>(Assert.Single(provider.GetServices<Microsoft.Extensions.Hosting.IHostedService>()));
        Assert.Equal(TimeSpan.FromDays(30), provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EventInboxCleanupOptions>>().Value.RetentionPeriod);
    }

    [Fact]
    public void TheCleanupSettings_AreReadFromTheEventInboxCleanupSection()
    {
        Microsoft.Extensions.Configuration.IConfiguration configuration = new Microsoft.Extensions.Configuration.ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EventInbox:Cleanup:RetentionPeriod"] = "30.00:00:00",
            ["EventInbox:Cleanup:BatchSize"] = "50",
        }).Build();
        ServiceCollection services = new();
        services.AddEventInboxCleanup(configuration: configuration);
        using ServiceProvider provider = services.BuildServiceProvider();

        EventInboxCleanupOptions options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<EventInboxCleanupOptions>>().Value;

        Assert.Equal((TimeSpan.FromDays(30), 50), (options.RetentionPeriod, options.BatchSize));
    }

    [Fact]
    public async Task ARetentionOfZero_MakesNoIndex()
    {
        MongoInboxRetention retention = new(
            new MongoClient("mongodb://localhost:1/?serverSelectionTimeoutMS=100").GetDatabase("unit").GetCollection<InboxMessage>("InboxMessage"),
            Microsoft.Extensions.Options.Options.Create(new EventInboxCleanupOptions { RetentionPeriod = TimeSpan.Zero }),
            Microsoft.Extensions.Logging.Abstractions.NullLogger<MongoInboxRetention>.Instance);

        await retention.StartAsync(CancellationToken.None);
        await retention.ExecuteTask!.WaitAsync(TimeSpan.FromSeconds(10));
        await retention.StopAsync(CancellationToken.None);

        // nothing was asked of MongoDB (it is not even reachable): the worker simply ended
        Assert.True(retention.ExecuteTask.IsCompletedSuccessfully);
    }

    [Fact]
    public async Task ADatabaseThatIsNotThereYet_IsWaitedFor_AndStopsWithTheService()
    {
        List<string> messages = [];
        MongoInboxRetention retention = new(
            new MongoClient("mongodb://localhost:1/?serverSelectionTimeoutMS=100").GetDatabase("unit").GetCollection<InboxMessage>("InboxMessage"),
            Microsoft.Extensions.Options.Options.Create(new EventInboxCleanupOptions()),
            new CapturingLogger<MongoInboxRetention>(messages));

        await retention.StartAsync(CancellationToken.None);
        for (int attempt = 0; attempt < 100 && messages.Count == 0; attempt++)
        {
            await Task.Delay(100);
        }

        // it waits to try again: the stop of the service ends the wait at once
        await retention.StopAsync(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Contains(messages, message => message.Contains("could not be made", StringComparison.Ordinal));
    }

    private sealed class CapturingLogger<T>(List<string> messages) : Microsoft.Extensions.Logging.ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
        {
            lock (messages)
            {
                messages.Add(formatter(state, exception));
            }
        }
    }

    [Fact]
    public void TheMistakes_AreToldWithTheirParameter()
    {
        Assert.Throws<ArgumentNullException>(() => EventInboxServiceCollectionExtensions.AddEventInbox(null!));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddEventInbox(" "));
    }

    [Fact]
    public async Task AnEventNeedsAName()
    {
        MongoInboxStore store = new(new MongoClient("mongodb://localhost:1").GetDatabase("unit").GetCollection<InboxMessage>("InboxMessage"), Microsoft.Extensions.Options.Options.Create(new EventInboxOptions()));

        await Assert.ThrowsAsync<ArgumentException>(() => store.TryStartAsync(Guid.NewGuid(), " ", null));
    }
}
