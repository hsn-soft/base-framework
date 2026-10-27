using HsnSoft.Base.EventBus.Inbox;
using HsnSoft.Base.EventBus.MongoDB;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace HsnSoft.Base.IntegrationTests.Data;

public sealed class MongoInboxTests : IAsyncLifetime
{
    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();
    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        string url = new MongoUrlBuilder(_container.GetConnectionString()) { DatabaseName = "inbox", AuthenticationSource = "admin" }.ToString();
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Mongo", url));
        _host = await Support.StartHostAsync(services => services
            .AddBaseMongo(configuration)
            .AddEventInbox("InboxMessage", configure: options => options.MaxRetryCount = 2));
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

    private async Task<InboxMessage> Row(Guid id)
        => await _host.Services.GetRequiredService<IMongoDatabase>().GetCollection<InboxMessage>("InboxMessage").Find(row => row.Id == id).SingleAsync();

    [Fact]
    public async Task ANewMessage_IsTaken_AndItsDocumentSaysWhoAndWhen()
    {
        Guid id = Guid.NewGuid();

        InboxStart start = await InScope(store => store.TryStartAsync(id, "OrderCreated", "corr-1"));

        InboxMessage row = await Row(id);
        Assert.Equal(InboxStart.Started, start);
        Assert.Equal((InboxStatuses.Started, "OrderCreated", "corr-1", 0), (row.Status, row.EventName, row.CorrelationId, row.RetryCount));
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
        Assert.Equal(1, (await Row(id)).RetryCount);
    }

    [Fact]
    public async Task AMessageThatWasHandled_IsNeverHandledAgain()
    {
        Guid id = Guid.NewGuid();
        await InScope(store => store.TryStartAsync(id, "OrderCreated", null));
        await InScope(async store =>
        {
            await store.CompleteAsync(id);
            return true;
        });

        InboxStart[] starts = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => InScope(store => store.TryStartAsync(id, "OrderCreated", null))));

        InboxMessage row = await Row(id);
        Assert.All(starts, start => Assert.Equal(InboxStart.AlreadyHandled, start));
        Assert.Equal(InboxStatuses.Completed, row.Status);
        Assert.NotNull(row.ProcessedAt);
    }

    [Fact]
    public async Task AMessageThatFailed_IsTakenAgainUpToTheLimit_AndTheReasonIsCut()
    {
        Guid id = Guid.NewGuid();
        await InScope(store => store.TryStartAsync(id, "OrderCreated", null));

        List<InboxStart> starts = [];
        for (int round = 0; round < 3; round++)
        {
            await InScope(async store =>
            {
                await store.FailAsync(id, new string('x', 5000));
                return true;
            });
            starts.Add(await InScope(store => store.TryStartAsync(id, "OrderCreated", null)));
        }

        InboxMessage row = await Row(id);
        Assert.Equal([InboxStart.Started, InboxStart.Started, InboxStart.RetryLimitReached], starts);
        Assert.Equal((InboxStatuses.Failed, 2, InboxMessage.ErrorMaxLength), (row.Status, row.RetryCount, row.Error!.Length));
    }

    [Fact]
    public async Task AHandlingThatDied_IsTakenOverWhenItsLeaseHasRunOut()
    {
        using IHost shortLease = await Support.StartHostAsync(services => services
            .AddBaseMongo(Support.Configuration(("ConnectionStrings:Mongo", new MongoUrlBuilder(_container.GetConnectionString()) { DatabaseName = "inbox", AuthenticationSource = "admin" }.ToString())))
            .AddEventInbox("InboxMessage", configure: options => options.ProcessingLease = TimeSpan.Zero));
        Guid id = Guid.NewGuid();
        await using (AsyncServiceScope scope = shortLease.Services.CreateAsyncScope())
        {
            await scope.ServiceProvider.GetRequiredService<IInboxStore>().TryStartAsync(id, "OrderCreated", null);
            await Task.Delay(50);

            InboxStart again = await scope.ServiceProvider.GetRequiredService<IInboxStore>().TryStartAsync(id, "OrderCreated", null);

            Assert.Equal(InboxStart.Started, again);
        }
    }

    [Fact]
    public async Task AnInboxThatCannotBeAsked_Throws_ItDoesNotSayTheMessageWasHandled()
    {
        // no MongoDB there: the insert cannot be made and the answer is not "already handled"
        IMongoCollection<InboxMessage> nowhere = new MongoClient("mongodb://127.0.0.1:1/?serverSelectionTimeoutMS=500").GetDatabase("inbox").GetCollection<InboxMessage>("InboxMessage");
        MongoInboxStore store = new(nowhere, Microsoft.Extensions.Options.Options.Create(new EventInboxOptions()));

        await Assert.ThrowsAnyAsync<Exception>(() => store.TryStartAsync(Guid.NewGuid(), "OrderCreated", null));
    }

    [Fact]
    public async Task AnInsertThatFailsForAnotherReasonThanTheDocumentBeingThere_IsThrown_NotTakenForASecondDelivery()
    {
        // a rule of the collection that this message breaks: the insert fails and no document is there, so it is not a duplicate
        IMongoDatabase database = _host.Services.GetRequiredService<IMongoDatabase>();
        await database.CreateCollectionAsync("PickyInbox", new CreateCollectionOptions<InboxMessage> { Validator = new FilterDefinitionBuilder<InboxMessage>().Ne(message => message.EventName, "Poison") });
        MongoInboxStore picky = new(database.GetCollection<InboxMessage>("PickyInbox"), Microsoft.Extensions.Options.Options.Create(new EventInboxOptions()));

        await Assert.ThrowsAsync<MongoWriteException>(() => picky.TryStartAsync(Guid.NewGuid(), "Poison", null));
    }
}
