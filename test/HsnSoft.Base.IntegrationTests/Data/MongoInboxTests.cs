using HsnSoft.Base.EventBus.Inbox;
using HsnSoft.Base.EventBus.MongoDB;
using HsnSoft.Base.MongoDB;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HsnSoft.Base.IntegrationTests.Data;

[Collection(Collections.Mongo)]
public sealed class MongoInboxTests(MongoFixture mongo) : IAsyncLifetime
{
    private string _url = null!;
    private IHost _host = null!;

    public async Task InitializeAsync()
    {
        _url = mongo.NewUrl();
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Mongo", _url));
        _host = await Support.StartHostAsync(services => services
            .AddBaseMongo(configuration)
            .AddEventInbox("InboxMessage", configure: options => options.MaxRetryCount = 2));
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
            .AddBaseMongo(Support.Configuration(("ConnectionStrings:Mongo", _url)))
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

    private async Task<BsonDocument> RetentionIndex()
    {
        IMongoCollection<InboxMessage> collection = _host.Services.GetRequiredService<IMongoDatabase>().GetCollection<InboxMessage>("InboxMessage");
        List<BsonDocument> indexes = await (await collection.Indexes.ListAsync()).ToListAsync();
        return indexes.SingleOrDefault(index => index["name"] == "inbox_processed_ttl") ?? [];
    }

    [Fact]
    public async Task TheRetentionIndex_IsMadeAtStart_ChangedInPlace_AndMongoRemovesOnlyHandledDocuments()
    {
        IMongoCollection<InboxMessage> collection = _host.Services.GetRequiredService<IMongoDatabase>().GetCollection<InboxMessage>("InboxMessage");
        DateTime now = DateTime.UtcNow;
        InboxMessage Document(string status, TimeSpan ago) => new() { Id = Guid.NewGuid(), EventName = "E", Status = status, StartedAt = now - ago, CreatedAt = now - ago, ProcessedAt = status == InboxStatuses.Completed ? now - ago : null };
        InboxMessage oldHandled = Document(InboxStatuses.Completed, TimeSpan.FromDays(3));
        InboxMessage recentHandled = Document(InboxStatuses.Completed, TimeSpan.FromHours(1));
        InboxMessage oldStarted = Document(InboxStatuses.Started, TimeSpan.FromDays(3));
        await collection.InsertManyAsync([oldHandled, recentHandled, oldStarted]);

        // a service that asks for the cleanup: MongoDB removes by itself, and the retention can be changed later
        MongoInboxRetention first = new(collection, Options.Create(new EventInboxCleanupOptions { RetentionPeriod = TimeSpan.FromDays(7) }), NullLogger<MongoInboxRetention>.Instance);
        await first.StartAsync(CancellationToken.None);
        await Support.EventuallyAsync(async () => (await RetentionIndex()).Contains("expireAfterSeconds"), "the retention index");
        Assert.Equal(7 * 86400, (await RetentionIndex())["expireAfterSeconds"].ToInt64());
        Assert.Equal(InboxStatuses.Completed, (await RetentionIndex())["partialFilterExpression"]["Status"].AsString);
        await first.StopAsync(CancellationToken.None);

        MongoInboxRetention second = new(collection, Options.Create(new EventInboxCleanupOptions { RetentionPeriod = TimeSpan.FromDays(2) }), NullLogger<MongoInboxRetention>.Instance);
        await second.StartAsync(CancellationToken.None);
        await Support.EventuallyAsync(async () => (await RetentionIndex())["expireAfterSeconds"].ToInt64() == 2 * 86400, "the retention to be changed in place");
        await second.StopAsync(CancellationToken.None);

        // the server removes what is handled and older than the retention (its pass is every second in this container); a started document is never removed
        await Support.EventuallyAsync(async () => await collection.CountDocumentsAsync(row => row.Id == oldHandled.Id) == 0, "MongoDB to remove the old handled document", TimeSpan.FromSeconds(30));
        Assert.Equal(1, await collection.CountDocumentsAsync(row => row.Id == recentHandled.Id));
        Assert.Equal(1, await collection.CountDocumentsAsync(row => row.Id == oldStarted.Id));
    }
}
