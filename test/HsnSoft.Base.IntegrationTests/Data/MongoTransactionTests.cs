using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;

namespace HsnSoft.Base.IntegrationTests.Data;

/// <summary>
/// The transactions of the MongoDB repository on a replica set: everything the work of a unit of work does through the repositories commits together or is rolled back together (reads see the work's own writes), and the three methods of a repository
/// that write many in a transaction write all or none, whatever fails.
/// </summary>
[Collection(Collections.MongoReplicaSet)]
public sealed class MongoTransactionTests(MongoReplicaSetFixture mongo) : IAsyncLifetime
{
    private IHost _host = null!;

    public sealed class Entry(Guid id, string title) : FullAuditedEntity<Guid>(id), IHasConcurrencyStamp
    {
        public Entry(string title)
            : this(Guid.Empty, title)
        {
        }

        public string Title { get; set; } = title;

        public string ConcurrencyStamp { get; private set; } = string.Empty;

        public void SetConcurrencyStamp(string stamp) => ConcurrencyStamp = stamp;
    }

    public sealed class Marker(Guid id, string label) : Entity<Guid>(id)
    {
        public string Label { get; set; } = label;
    }

    private sealed class FakeUser : ICurrentUser
    {
        public bool IsAuthenticated => false;

        public Guid? Id => null;

        public string? UserName => null;
    }

    public async Task InitializeAsync()
    {
        MongoEntityMaps.RegisterBases<Guid>();
        if (!BsonClassMap.IsClassMapRegistered(typeof(Entry)))
        {
            BsonClassMap.RegisterClassMap<Entry>(map =>
            {
                map.MapProperty(entry => entry.Title);
                map.MapProperty(entry => entry.ConcurrencyStamp);
                map.MapCreator(entry => new Entry(entry.Id, entry.Title));
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Marker)))
        {
            BsonClassMap.RegisterClassMap<Marker>(map =>
            {
                map.MapProperty(marker => marker.Label);
                map.MapCreator(marker => new Marker(marker.Id, marker.Label));
            });
        }

        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Mongo", mongo.NewUrl()));
        _host = await Support.StartHostAsync(services => services
            .AddSingleton<ICurrentUser>(new FakeUser())
            .AddBaseMongo(configuration)
            .AddMongoRepository<Entry, Guid>("entries")
            .AddMongoRepository<Marker, Guid>("markers")
            .AddMongoUnitOfWork());

        // a collection is made outside a transaction (a transaction cannot make one); the first write of a test would otherwise be the one that does
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        Entry first = await Entries(scope).AddAsync(new Entry("warm up"));
        await Entries(scope).DeleteWhereAsync(entry => entry.Id == first.Id);
        Marker warm = await Markers(scope).AddAsync(new Marker(Guid.Empty, "warm up"));
        await Markers(scope).DeleteAsync(warm);
    }

    public Task DisposeAsync()
    {
        _host.Dispose();
        return Task.CompletedTask;
    }

    private AsyncServiceScope Scope() => _host.Services.CreateAsyncScope();

    private static IMongoRepository<Entry, Guid> Entries(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IMongoRepository<Entry, Guid>>();

    private static IMongoRepository<Marker, Guid> Markers(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IMongoRepository<Marker, Guid>>();

    private static IUnitOfWork Work(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

    private async Task<List<Entry>> SeedAsync(int count)
    {
        await using AsyncServiceScope scope = Scope();
        List<Entry> entries = [.. Enumerable.Range(1, count).Select(number => new Entry($"entry {number}"))];
        await Entries(scope).AddManyAsync(entries);
        return entries;
    }

    private async Task<long> CountAsync()
    {
        await using AsyncServiceScope scope = Scope();
        return await Entries(scope).CountAsync();
    }

    // ---- the unit of work ----

    [Fact]
    public async Task TheWorkOfAUnitOfWork_IsCommittedTogether()
    {
        await using AsyncServiceScope scope = Scope();
        Entry entry = new("in the transaction");
        Marker marker = new(Guid.Empty, "also");

        await Work(scope).ExecuteAsync(async token =>
        {
            await Entries(scope).AddAsync(entry, token);
            await Markers(scope).AddAsync(marker, token);
        });

        await using AsyncServiceScope check = Scope();
        Assert.NotNull(await Entries(check).FindAsync(entry.Id));
        Assert.NotNull(await Markers(check).FindAsync(marker.Id));
    }

    [Fact]
    public async Task TheWorkOfAUnitOfWork_ThatFails_IsRolledBack_AllOfIt()
    {
        await using AsyncServiceScope scope = Scope();
        Entry entry = new("never");
        Marker marker = new(Guid.Empty, "never");

        await Assert.ThrowsAsync<InvalidOperationException>(() => Work(scope).ExecuteAsync(async token =>
        {
            await Entries(scope).AddAsync(entry, token);
            await Markers(scope).AddAsync(marker, token);
            throw new InvalidOperationException("the work failed after two writes");
        }));

        await using AsyncServiceScope check = Scope();
        Assert.Null(await Entries(check).FindAsync(entry.Id));
        Assert.Null(await Markers(check).FindAsync(marker.Id));
    }

    [Fact]
    public async Task TheWork_SeesItsOwnWrites_AndOthersDoNotUntilItCommits()
    {
        await using AsyncServiceScope scope = Scope();
        Entry entry = new("mine");
        bool seenInside = false;
        bool seenOutside = true;

        await Work(scope).ExecuteAsync(async token =>
        {
            await Entries(scope).AddAsync(entry, token);
            seenInside = await Entries(scope).FindAsync(entry.Id, token) is not null;
            // what another request reads: a flow of its own (the transaction follows the async flow it was opened in, so this one is cut from it)
            Task<bool> outside;
            using (ExecutionContext.SuppressFlow())
            {
                outside = Task.Run(async () =>
                {
                    await using AsyncServiceScope other = Scope();
                    return await Entries(other).FindAsync(entry.Id, token) is not null;
                }, token);
            }

            seenOutside = await outside;
        });

        Assert.True(seenInside);
        Assert.False(seenOutside);
        await using AsyncServiceScope check = Scope();
        Assert.NotNull(await Entries(check).FindAsync(entry.Id));
    }

    [Fact]
    public async Task TheWork_GivesBackWhatItReturns()
    {
        await using AsyncServiceScope scope = Scope();

        Guid id = await Work(scope).ExecuteAsync(async token => (await Entries(scope).AddAsync(new Entry("returned"), token)).Id);

        Assert.NotEqual(Guid.Empty, id);
        await using AsyncServiceScope check = Scope();
        Assert.NotNull(await Entries(check).FindAsync(id));
    }

    [Fact]
    public async Task AUnitOfWorkInsideAUnitOfWork_JoinsIt_SoTheOuterOneRollsBackBoth()
    {
        await using AsyncServiceScope scope = Scope();
        Entry outer = new("outer");
        Entry inner = new("inner");

        await Assert.ThrowsAsync<InvalidOperationException>(() => Work(scope).ExecuteAsync(async token =>
        {
            await Entries(scope).AddAsync(outer, token);
            await Work(scope).ExecuteAsync(async innerToken => await Entries(scope).AddAsync(inner, innerToken), token);   // joins: it does not commit on its own
            throw new InvalidOperationException("the outer work failed");
        }));

        await using AsyncServiceScope check = Scope();
        Assert.Equal(0, await Entries(check).CountAsync(entry => entry.Title == "outer" || entry.Title == "inner"));
    }

    [Fact]
    public async Task AWriteThatFailsInsideAUnitOfWork_IsARepositoryException_AndRollsBackTheRest()
    {
        List<Entry> seeded = await SeedAsync(2);
        await using AsyncServiceScope scope = Scope();
        Entry fresh = new("fresh");
        Entry gone = new(Guid.NewGuid(), "gone");

        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Work(scope).ExecuteAsync(async token =>
        {
            await Entries(scope).AddAsync(fresh, token);
            await Entries(scope).DeleteAsync(gone, token);   // not there
        }));

        Assert.Equal(404, failed.HttpStatusCode);
        Assert.Equal(gone.Id.ToString(), Assert.Single(failed.Ids));
        Assert.Equal(seeded.Count, await CountAsync());   // the add of the fresh one was rolled back
    }

    // ---- the repository's own transactional methods ----

    [Fact]
    public async Task AddManyInTransactionAsync_AddsAll()
    {
        await using AsyncServiceScope scope = Scope();
        List<Entry> entries = [new("a"), new("b"), new("c")];

        await Entries(scope).AddManyInTransactionAsync(entries);

        Assert.Equal(3, await Entries(scope).CountAsync());
        Assert.All(entries, entry => Assert.Equal(7, entry.Id.Version));
    }

    [Fact]
    public async Task AddManyInTransactionAsync_ThatFails_AddsNone()
    {
        Entry existing = (await SeedAsync(1))[0];
        await using AsyncServiceScope scope = Scope();
        List<Entry> batch = [new("d"), new("e"), new(existing.Id, "again")];

        await Assert.ThrowsAnyAsync<Exception>(() => Entries(scope).AddManyInTransactionAsync(batch));

        Assert.Equal(1, await CountAsync());
    }

    [Fact]
    public async Task UpdateManyInTransactionAsync_WritesAll_OrNoneWhenOneIsGone()
    {
        List<Entry> seeded = await SeedAsync(3);
        await using AsyncServiceScope scope = Scope();
        List<Entry> entries = [];
        foreach (Entry seededEntry in seeded)
        {
            entries.Add((await Entries(scope).FindAsync(seededEntry.Id))!);
        }

        entries.ForEach(entry => entry.Title = "changed");
        WriteResult ok = await Entries(scope).UpdateManyInTransactionAsync(entries);
        Assert.Equal((3, 3), (ok.Requested, ok.Modified));

        await using (AsyncServiceScope other = Scope())
        {
            await Entries(other).DeleteAsync((await Entries(other).FindAsync(seeded[1].Id))!);
        }

        entries.ForEach(entry => entry.Title = "late");
        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Entries(scope).UpdateManyInTransactionAsync(entries));

        Assert.Equal(seeded[1].Id.ToString(), Assert.Single(failed.Ids));
        await using AsyncServiceScope check = Scope();
        Assert.Equal("changed", (await Entries(check).FindAsync(seeded[0].Id))!.Title);   // not "late": nothing of the failed write stayed
        Assert.Equal("changed", (await Entries(check).FindAsync(seeded[2].Id))!.Title);
    }

    [Fact]
    public async Task DeleteManyInTransactionAsync_DeletesAll_OrNoneWhenOneIsGone()
    {
        await using AsyncServiceScope scope = Scope();
        List<Marker> markers = [new(Guid.Empty, "a"), new(Guid.Empty, "b")];
        await Markers(scope).AddManyAsync(markers);
        Marker gone = new(Guid.NewGuid(), "gone");

        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Markers(scope).DeleteManyInTransactionAsync([markers[0], markers[1], gone]));
        Assert.Equal(gone.Id.ToString(), Assert.Single(failed.Ids));
        Assert.Equal(2, await Markers(scope).CountAsync());   // none was deleted

        await Markers(scope).DeleteManyInTransactionAsync(markers);
        Assert.Equal(0, await Markers(scope).CountAsync());
    }

    [Fact]
    public async Task TheTransactionalMethods_JoinTheTransactionThatIsOpen()
    {
        await using AsyncServiceScope scope = Scope();
        List<Entry> entries = [new("x"), new("y")];

        await Assert.ThrowsAsync<InvalidOperationException>(() => Work(scope).ExecuteAsync(async token =>
        {
            await Entries(scope).AddManyInTransactionAsync(entries, token);   // joins: it does not commit on its own
            throw new InvalidOperationException("rolled back");
        }));

        Assert.Equal(0, await CountAsync());
    }
}
