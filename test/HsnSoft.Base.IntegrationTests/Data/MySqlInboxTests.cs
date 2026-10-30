using HsnSoft.Base.EventBus.EntityFrameworkCore;
using HsnSoft.Base.EventBus.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.IntegrationTests.Data;

[Collection(Collections.MySql)]
public sealed class MySqlInboxTests(MySqlFixture mySql) : IAsyncLifetime
{
    private string _connection = null!;
    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        // once: the options of the context are built for every scope, and every scope has to reach the same database
        _connection = mySql.NewConnectionString();
        _host = await Support.StartHostAsync(services => services
            .AddDbContext<InboxTestContext>(options => options.UseMySQL(_connection + ";Connection Timeout=60;Default Command Timeout=60"))
            .AddEventInbox<InboxTestContext>(options => options.MaxRetryCount = 5));
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<InboxTestContext>().Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync()
    {
        _host.Dispose();
        return Task.CompletedTask;
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

    [Fact]
    public async Task AnInsertThatFailsForAnotherReasonThanTheRowBeingThere_IsThrown_NotTakenForASecondDelivery()
    {
        // a rule of the table that this message breaks: the insert fails and no row is there, so it is not a duplicate
        await using (AsyncServiceScope scope = _host.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<InboxTestContext>().Database.ExecuteSqlRawAsync("ALTER TABLE InboxMessage ADD CONSTRAINT no_poison CHECK (EventName <> 'Poison')");
        }

        await Assert.ThrowsAsync<DbUpdateException>(() => InScope(store => store.TryStartAsync(Guid.NewGuid(), "Poison", null)));
    }
}
