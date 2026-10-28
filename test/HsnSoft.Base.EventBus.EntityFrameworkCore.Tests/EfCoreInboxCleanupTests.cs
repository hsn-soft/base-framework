using HsnSoft.Base.EventBus.Inbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.EntityFrameworkCore.Tests;

public sealed class EfCoreInboxCleanupTests : IAsyncLifetime
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ServiceProvider _provider = null!;

    public sealed class InboxContext(DbContextOptions<InboxContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ConfigureEventInbox();
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        await Start(options => options.BatchSize = 2);
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task Start(Action<EventInboxCleanupOptions> configure, IConfiguration? configuration = null)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDbContext<InboxContext>(options => options.UseSqlite(_connection));
        services.AddEventInboxCleanup<InboxContext>(configuration, configure);
        _provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<InboxContext>().Database.EnsureCreatedAsync();
    }

    private async Task<Guid> AddRow(string status, TimeSpan processedAgo)
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        InboxContext db = scope.ServiceProvider.GetRequiredService<InboxContext>();
        DateTime now = DateTime.UtcNow;
        InboxMessage row = new()
        {
            Id = Guid.NewGuid(),
            EventName = "OrderCreated",
            Status = status,
            StartedAt = now - processedAgo,
            CreatedAt = now - processedAgo,
            ProcessedAt = status == InboxStatuses.Completed ? now - processedAgo : null,
        };
        db.Set<InboxMessage>().Add(row);
        await db.SaveChangesAsync();
        return row.Id;
    }

    private async Task<HashSet<Guid>> Ids()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        return [.. await scope.ServiceProvider.GetRequiredService<InboxContext>().Set<InboxMessage>().AsNoTracking().Select(row => row.Id).ToListAsync()];
    }

    private IHostedService Worker() => _provider.GetServices<IHostedService>().Single();

    private static async Task RunOnce(IHostedService worker, Func<Task<bool>> done)
    {
        await worker.StartAsync(CancellationToken.None);
        for (int attempt = 0; attempt < 100 && !await done(); attempt++)
        {
            await Task.Delay(50);
        }

        await worker.StopAsync(CancellationToken.None);
    }

    [Fact]
    public async Task OnlyOldHandledRows_AreRemoved_InBatches()
    {
        List<Guid> old = [];
        for (int count = 0; count < 5; count++)
        {
            old.Add(await AddRow(InboxStatuses.Completed, TimeSpan.FromDays(8)));
        }

        Guid recent = await AddRow(InboxStatuses.Completed, TimeSpan.FromDays(1));
        Guid started = await AddRow(InboxStatuses.Started, TimeSpan.FromDays(30));
        Guid failed = await AddRow(InboxStatuses.Failed, TimeSpan.FromDays(30));

        await RunOnce(Worker(), async () => (await Ids()).Count == 3);

        Assert.Equivalent(new[] { recent, started, failed }, await Ids());
    }

    [Fact]
    public async Task ARetentionOfZero_TurnsTheCleanupOff()
    {
        await _provider.DisposeAsync();
        await _connection.CloseAsync();
        await _connection.OpenAsync();
        await Start(options => options.RetentionPeriod = TimeSpan.Zero);
        Guid old = await AddRow(InboxStatuses.Completed, TimeSpan.FromDays(400));

        await RunOnce(Worker(), () => Task.FromResult(false));

        Assert.Contains(old, await Ids());
    }

    [Fact]
    public async Task InstancesThatCleanAtTheSameTime_DoNotNeedToKnowOfEachOther()
    {
        for (int count = 0; count < 20; count++)
        {
            await AddRow(InboxStatuses.Completed, TimeSpan.FromDays(9));
        }

        Guid recent = await AddRow(InboxStatuses.Completed, TimeSpan.FromHours(1));
        // two instances of the service: two workers over the same database
        IHostedService[] workers =
        [
            Worker(),
            new EfCoreInboxCleanup<InboxContext>(_provider.GetRequiredService<IServiceScopeFactory>(), _provider.GetRequiredService<IOptions<EventInboxCleanupOptions>>(), NullLogger<EfCoreInboxCleanup<InboxContext>>.Instance),
        ];

        await Task.WhenAll(workers.Select(worker => RunOnce(worker, async () => (await Ids()).Count == 1)));

        Assert.Equivalent(new[] { recent }, await Ids());
    }

    [Fact]
    public async Task TheSettings_AreReadFromTheEventInboxCleanupSection()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["EventInbox:Cleanup:RetentionPeriod"] = "30.00:00:00",
            ["EventInbox:Cleanup:Interval"] = "00:10:00",
            ["EventInbox:Cleanup:BatchSize"] = "50",
        }).Build();
        await _provider.DisposeAsync();
        await _connection.CloseAsync();
        await _connection.OpenAsync();
        await Start(_ => { }, configuration);

        EventInboxCleanupOptions options = _provider.GetRequiredService<IOptions<EventInboxCleanupOptions>>().Value;

        Assert.Equal((TimeSpan.FromDays(30), TimeSpan.FromMinutes(10), 50), (options.RetentionPeriod, options.Interval, options.BatchSize));
    }

    [Fact]
    public void WithoutTheCall_ThereIsNoWorker()
    {
        ServiceCollection services = new();
        services.AddDbContext<InboxContext>(options => options.UseSqlite(_connection));
        services.AddEventInbox<InboxContext>();

        using ServiceProvider provider = services.BuildServiceProvider();

        Assert.Empty(provider.GetServices<IHostedService>());
    }
}
