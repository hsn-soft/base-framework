using HsnSoft.Base.EventBus.Inbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EventBus.EntityFrameworkCore.Tests;

public sealed class EfCoreInboxStoreTests : IAsyncLifetime, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ServiceProvider _provider = null!;

    public sealed class InboxContext(DbContextOptions<InboxContext> options) : DbContext(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ConfigureEventInbox();
    }

    public async Task InitializeAsync() => await Start();

    public void Dispose() => _connection.Dispose();

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    private async Task Start(Action<EventInboxOptions>? configure = null)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }

        ServiceCollection services = new();
        services.AddDbContext<InboxContext>(options => options.UseSqlite(_connection));
        services.AddEventInbox<InboxContext>(configure);
        _provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<InboxContext>().Database.EnsureCreatedAsync();
    }

    // every call in a scope of its own, as the dispatcher makes them
    private async Task<T> InScope<T>(Func<IInboxStore, Task<T>> action)
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<IInboxStore>());
    }

    private async Task<InboxMessage> Row(Guid id)
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        return await scope.ServiceProvider.GetRequiredService<InboxContext>().Set<InboxMessage>().AsNoTracking().SingleAsync(message => message.Id == id);
    }

    [Fact]
    public async Task ANewMessage_IsTaken_AndItsRowSaysWhoAndWhen()
    {
        Guid id = Guid.NewGuid();

        InboxStart start = await InScope(async store => await store.TryStartAsync(id, "OrderCreated", "corr-1"));

        InboxMessage row = await Row(id);
        Assert.Equal(InboxStart.Started, start);
        Assert.Equal((InboxStatuses.Started, "OrderCreated", "corr-1", 0), (row.Status, row.EventName, row.CorrelationId, row.RetryCount));
        Assert.Null(row.ProcessedAt);
    }

    [Fact]
    public async Task ASecondDeliveryWhileTheFirstIsRunning_IsLeftAlone()
    {
        Guid id = Guid.NewGuid();
        await InScope(async store => await store.TryStartAsync(id, "OrderCreated", null));

        InboxStart second = await InScope(async store => await store.TryStartAsync(id, "OrderCreated", null));

        Assert.Equal(InboxStart.InProgress, second);
        Assert.Equal(0, (await Row(id)).RetryCount);
    }

    [Fact]
    public async Task AMessageThatWasHandled_IsNeverHandledAgain()
    {
        Guid id = Guid.NewGuid();
        await InScope(async store => await store.TryStartAsync(id, "OrderCreated", null));
        await InScope(async store =>
        {
            await store.CompleteAsync(id);
            return true;
        });

        InboxStart again = await InScope(async store => await store.TryStartAsync(id, "OrderCreated", null));

        InboxMessage row = await Row(id);
        Assert.Equal(InboxStart.AlreadyHandled, again);
        Assert.Equal(InboxStatuses.Completed, row.Status);
        Assert.NotNull(row.ProcessedAt);
        Assert.Null(row.Error);
    }

    [Fact]
    public async Task AMessageThatFailed_IsTakenAgainWithItsRetryCount_UpToTheLimit()
    {
        await _provider.DisposeAsync();
        await Start(options => options.MaxRetryCount = 2);
        Guid id = Guid.NewGuid();
        await InScope(async store => await store.TryStartAsync(id, "OrderCreated", null));

        List<InboxStart> starts = [];
        for (int round = 0; round < 3; round++)
        {
            await InScope(async store =>
            {
                await store.FailAsync(id, $"failure {round}");
                return true;
            });
            starts.Add(await InScope(async store => await store.TryStartAsync(id, "OrderCreated", null)));
        }

        InboxMessage row = await Row(id);
        Assert.Equal([InboxStart.Started, InboxStart.Started, InboxStart.RetryLimitReached], starts);
        Assert.Equal((InboxStatuses.Failed, 2, "failure 2"), (row.Status, row.RetryCount, row.Error));
    }

    [Fact]
    public async Task AHandlingThatDied_IsTakenOverWhenItsLeaseHasRunOut()
    {
        await _provider.DisposeAsync();
        await Start(options => options.ProcessingLease = TimeSpan.Zero);
        Guid id = Guid.NewGuid();
        await InScope(async store => await store.TryStartAsync(id, "OrderCreated", null));
        await Task.Delay(20);

        InboxStart again = await InScope(async store => await store.TryStartAsync(id, "OrderCreated", null));

        Assert.Equal(InboxStart.Started, again);
        Assert.Equal(1, (await Row(id)).RetryCount);
    }

    [Fact]
    public async Task AFailureReasonThatIsTooLong_IsCut()
    {
        Guid id = Guid.NewGuid();
        await InScope(async store => await store.TryStartAsync(id, "OrderCreated", null));

        await InScope(async store =>
        {
            await store.FailAsync(id, new string('x', 5000));
            return true;
        });

        Assert.Equal(InboxMessage.ErrorMaxLength, (await Row(id)).Error!.Length);
    }

    [Fact]
    public async Task AnInboxThatCannotBeAsked_Throws_ItDoesNotSayTheMessageWasHandled()
    {
        // a database without the table: the insert fails and the row is not there either, so this is not a second delivery
        await using SqliteConnection empty = new("Data Source=:memory:");
        await empty.OpenAsync();
        ServiceCollection services = new();
        services.AddDbContext<InboxContext>(options => options.UseSqlite(empty));
        services.AddEventInbox<InboxContext>();
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.GetRequiredService<IInboxStore>().TryStartAsync(Guid.NewGuid(), "OrderCreated", null));
    }

    [Fact]
    public async Task TheNames_AreCutToTheirColumns_AndAnEventNeedsAName()
    {
        Guid id = Guid.NewGuid();

        await InScope(async store => await store.TryStartAsync(id, new string('e', 600), new string('c', 600)));

        InboxMessage row = await Row(id);
        Assert.Equal(InboxMessage.EventNameMaxLength, row.EventName.Length);
        Assert.Equal(InboxMessage.CorrelationIdMaxLength, row.CorrelationId!.Length);
        await Assert.ThrowsAsync<ArgumentException>(() => InScope(async store => await store.TryStartAsync(Guid.NewGuid(), " ", null)));
    }

    [Fact]
    public void TheRegistration_GivesTheServiceItsStoreAndTheOptions_AndTheTableHasItsName()
    {
        using IServiceScope scope = _provider.CreateScope();

        Assert.IsType<EfCoreInboxStore<InboxContext>>(scope.ServiceProvider.GetRequiredService<IInboxStore>());
        Assert.NotNull(_provider.GetService<InboxEnabled>());
        Assert.Equal("InboxMessage", scope.ServiceProvider.GetRequiredService<InboxContext>().Model.FindEntityType(typeof(InboxMessage))!.GetTableName());
        Assert.Throws<ArgumentNullException>(() => EventInboxServiceCollectionExtensions.AddEventInbox<InboxContext>(null!));
        Assert.Throws<ArgumentNullException>(() => InboxModelBuilderExtensions.ConfigureEventInbox(null!));
        Assert.Throws<ArgumentException>(() => new ModelBuilder().ConfigureEventInbox(" "));
    }
}
