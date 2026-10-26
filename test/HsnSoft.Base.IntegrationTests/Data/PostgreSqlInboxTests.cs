using HsnSoft.Base.EventBus.EntityFrameworkCore;
using HsnSoft.Base.EventBus.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;

namespace HsnSoft.Base.IntegrationTests.Data;

public sealed class InboxTestContext(DbContextOptions<InboxTestContext> options) : DbContext(options)
{
    protected override void OnModelCreating(ModelBuilder modelBuilder) => modelBuilder.ConfigureEventInbox();
}

public sealed class PostgreSqlInboxTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:16").Build();
    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        _host = await Support.StartHostAsync(services => services
            .AddDbContext<InboxTestContext>(options => options.UseNpgsql(_container.GetConnectionString()))
            .AddEventInbox<InboxTestContext>(options => options.MaxRetryCount = 5));
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<InboxTestContext>().Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        _host.Dispose();
        await _container.DisposeAsync();
    }

    private async Task<T> InScope<T>(Func<IInboxStore, Task<T>> action)
    {
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        return await action(scope.ServiceProvider.GetRequiredService<IInboxStore>());
    }

    [Fact]
    public async Task OfTwentyDeliveriesOfOneMessageAtOnce_OnlyOneIsTaken()
    {
        Guid id = Guid.NewGuid();

        InboxStart[] starts = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => InScope(store => store.TryStartAsync(id, "OrderCreated", "corr"))));

        Assert.Equal(1, starts.Count(start => start == InboxStart.Started));
        Assert.Equal(19, starts.Count(start => start == InboxStart.InProgress));
    }

    [Fact]
    public async Task OfTwentyDeliveriesOfAMessageThatFailed_OnlyOneTakesItAgain()
    {
        Guid id = Guid.NewGuid();
        await InScope(store => store.TryStartAsync(id, "OrderCreated", null));
        await InScope(async store =>
        {
            await store.FailAsync(id, "the first handling failed");
            return true;
        });

        InboxStart[] starts = await Task.WhenAll(Enumerable.Range(0, 20).Select(_ => InScope(store => store.TryStartAsync(id, "OrderCreated", null))));

        Assert.Equal(1, starts.Count(start => start == InboxStart.Started));
        Assert.Equal(19, starts.Count(start => start == InboxStart.InProgress));
    }

    [Fact]
    public async Task AMessageThatWasHandled_IsNotTakenAgain_ByAnyNumberOfDeliveries()
    {
        Guid id = Guid.NewGuid();
        await InScope(store => store.TryStartAsync(id, "OrderCreated", null));
        await InScope(async store =>
        {
            await store.CompleteAsync(id);
            return true;
        });

        InboxStart[] starts = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => InScope(store => store.TryStartAsync(id, "OrderCreated", null))));

        Assert.All(starts, start => Assert.Equal(InboxStart.AlreadyHandled, start));
    }
}
