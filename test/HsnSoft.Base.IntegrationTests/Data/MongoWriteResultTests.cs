using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace HsnSoft.Base.IntegrationTests.Data;

/// <summary>
/// What a write of the MongoDB repository gives back and how it fails on a server that has no transactions: the same instance, a key for an entity that has none, <see cref="WriteOutcome.NotModified"/> for an entity that FindAsync returned and did not change, and one
/// <see cref="RepositoryException"/> that names every record that was not there or not current. Each document is atomic, the batch is not: the others of a failed update were written, and an insert that failed removes the ones it had added.
/// </summary>
[Collection(Collections.Mongo)]
public sealed class MongoWriteResultTests(MongoFixture mongo) : IAsyncLifetime
{
    private IHost _host = null!;

    public sealed class Ledger(Guid id, string title) : FullAuditedEntity<Guid>(id), IHasConcurrencyStamp
    {
        public Ledger(string title)
            : this(Guid.Empty, title)
        {
        }

        public string Title { get; set; } = title;

        public string ConcurrencyStamp { get; private set; } = string.Empty;

        public void SetConcurrencyStamp(string stamp) => ConcurrencyStamp = stamp;
    }

    public sealed class Stub(Guid id, string label) : Entity<Guid>(id)
    {
        public string Label { get; set; } = label;
    }

    private sealed class FakeUser : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid? Id { get; } = Guid.Parse("22222222-2222-2222-2222-222222222222");

        public string? UserName => null;
    }

    public async Task InitializeAsync()
    {
        MongoEntityMaps.RegisterBases<Guid>();
        if (!BsonClassMap.IsClassMapRegistered(typeof(Ledger)))
        {
            BsonClassMap.RegisterClassMap<Ledger>(map =>
            {
                map.MapProperty(ledger => ledger.Title);
                map.MapProperty(ledger => ledger.ConcurrencyStamp);
                map.MapCreator(ledger => new Ledger(ledger.Id, ledger.Title));
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Stub)))
        {
            BsonClassMap.RegisterClassMap<Stub>(map =>
            {
                map.MapProperty(stub => stub.Label);
                map.MapCreator(stub => new Stub(stub.Id, stub.Label));
            });
        }

        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Mongo", mongo.NewUrl()));
        _host = await Support.StartHostAsync(services => services
            .AddSingleton<ICurrentUser>(new FakeUser())
            .AddBaseMongo(configuration)
            .AddMongoRepository<Ledger, Guid>("ledgers")
            .AddMongoRepository<Stub, Guid>("stubs")
            .AddMongoUnitOfWork());

        // a unique index on the label of a stub, to be broken
        await _host.Services.GetRequiredService<IMongoDatabase>().GetCollection<Stub>("stubs").Indexes.CreateOneAsync(new CreateIndexModel<Stub>(Builders<Stub>.IndexKeys.Ascending(stub => stub.Label), new CreateIndexOptions { Unique = true, Name = "ux_stub_label" }));
    }

    public Task DisposeAsync()
    {
        _host.Dispose();
        return Task.CompletedTask;
    }

    private AsyncServiceScope Scope() => _host.Services.CreateAsyncScope();

    private static IMongoRepository<Ledger, Guid> Ledgers(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IMongoRepository<Ledger, Guid>>();

    private static IMongoRepository<Stub, Guid> Stubs(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IMongoRepository<Stub, Guid>>();

    private async Task<List<Ledger>> SeedAsync(int count)
    {
        await using AsyncServiceScope scope = Scope();
        List<Ledger> ledgers = [.. Enumerable.Range(1, count).Select(number => new Ledger($"ledger {number}"))];
        await Ledgers(scope).AddManyAsync(ledgers);
        return ledgers;
    }

    private async Task<Ledger?> ReadAsync(Guid id)
    {
        await using AsyncServiceScope scope = Scope();
        return await Ledgers(scope).FindAsync(id);
    }

    private static async Task<List<Ledger>> FindAllAsync(AsyncServiceScope scope, IEnumerable<Ledger> seeded)
    {
        List<Ledger> found = [];
        foreach (Ledger ledger in seeded)
        {
            found.Add((await Ledgers(scope).FindAsync(ledger.Id))!);
        }

        return found;
    }

    // ---- Add ----

    [Fact]
    public async Task AddAsync_GivesBackTheSameInstance_WithWhatWasWrittenBeforeTheInsert()
    {
        Ledger ledger = new("first");
        await using AsyncServiceScope scope = Scope();

        Ledger added = await Ledgers(scope).AddAsync(ledger);

        Assert.Same(ledger, added);
        Assert.NotEqual(Guid.Empty, added.Id);
        Assert.Equal(Guid.Parse("22222222-2222-2222-2222-222222222222"), added.CreatedBy);
        Assert.NotEmpty(added.ConcurrencyStamp);
        Assert.Equal(added.ConcurrencyStamp, (await ReadAsync(added.Id))!.ConcurrencyStamp);
    }

    [Fact]
    public async Task AnEmptyGuidKey_IsGiven_AndAKeyThatWasGiven_IsKept()
    {
        Guid chosen = Guid.NewGuid();
        await using AsyncServiceScope scope = Scope();

        Ledger made = await Ledgers(scope).AddAsync(new Ledger("no key"));
        Ledger kept = await Ledgers(scope).AddAsync(new Ledger(chosen, "my key"));
        Stub plain = await Stubs(scope).AddAsync(new Stub(Guid.Empty, "plain"));

        Assert.Equal(7, made.Id.Version);
        Assert.Equal(chosen, kept.Id);
        Assert.NotEqual(Guid.Empty, plain.Id);
        Assert.NotNull(await ReadAsync(made.Id));
        Assert.NotNull(await ReadAsync(chosen));
    }

    [Fact]
    public async Task AddManyAsync_GivesKeys_AndAddsAll()
    {
        await using AsyncServiceScope scope = Scope();
        List<Ledger> ledgers = [new("a"), new("b"), new("c")];

        await Ledgers(scope).AddManyAsync(ledgers);

        Assert.Equal(3, ledgers.Select(ledger => ledger.Id).Distinct().Count());
        Assert.Equal(3, await Ledgers(scope).CountAsync());
        await Ledgers(scope).AddManyAsync([]);   // nothing to add is no call
    }

    [Fact]
    public async Task AddManyAsync_ThatFailsInTheMiddle_RemovesWhatItHadAdded_SoItIsAllOrNone()
    {
        Ledger existing = (await SeedAsync(1))[0];
        await using AsyncServiceScope scope = Scope();
        List<Ledger> batch = [new("d"), new("e"), new(existing.Id, "again"), new("f")];

        await Assert.ThrowsAnyAsync<Exception>(() => Ledgers(scope).AddManyAsync(batch));

        await using AsyncServiceScope check = Scope();
        Assert.Equal(1, await Ledgers(check).CountAsync());   // the two that went in before the one that failed are gone, and the one after it never went in
        Assert.Null(await Ledgers(check).FindAsync(batch[0].Id));
        Assert.Null(await Ledgers(check).FindAsync(batch[1].Id));
        Assert.Null(await Ledgers(check).FindAsync(batch[3].Id));
        Assert.Equal("ledger 1", (await Ledgers(check).FindAsync(existing.Id))!.Title);
    }

    // ---- Update ----

    [Fact]
    public async Task UpdateAsync_OfAnEntityThatFindAsyncReturnedAndDidNotChange_IsNotModified_AndWritesNothing()
    {
        Ledger seeded = (await SeedAsync(1))[0];
        await using AsyncServiceScope scope = Scope();
        Ledger ledger = (await Ledgers(scope).FindAsync(seeded.Id))!;
        (string stamp, DateTimeOffset? updatedAt) = (ledger.ConcurrencyStamp, ledger.UpdatedAt);

        WriteOutcome outcome = await Ledgers(scope).UpdateAsync(ledger);

        Assert.Equal(WriteOutcome.NotModified, outcome);
        Assert.Equal((stamp, updatedAt), (ledger.ConcurrencyStamp, ledger.UpdatedAt));   // the stamp and the audit fields stay
        Assert.Equal(stamp, (await ReadAsync(seeded.Id))!.ConcurrencyStamp);
    }

    [Fact]
    public async Task UpdateAsync_OfAnEntityThatChanged_IsModified_GivesANewStamp_AndIsNotModifiedAgainUntilItChangesAgain()
    {
        Ledger seeded = (await SeedAsync(1))[0];
        await using AsyncServiceScope scope = Scope();
        Ledger ledger = (await Ledgers(scope).FindAsync(seeded.Id))!;
        string before = ledger.ConcurrencyStamp;

        ledger.Title = "changed";
        Assert.Equal(WriteOutcome.Modified, await Ledgers(scope).UpdateAsync(ledger));
        string after = ledger.ConcurrencyStamp;
        Assert.NotEqual(before, after);
        Assert.Equal(WriteOutcome.NotModified, await Ledgers(scope).UpdateAsync(ledger));   // what was just written is the new state of reference
        Assert.Equal(after, ledger.ConcurrencyStamp);

        ledger.Title = "changed again";
        Assert.Equal(WriteOutcome.Modified, await Ledgers(scope).UpdateAsync(ledger));
        Assert.Equal("changed again", (await ReadAsync(seeded.Id))!.Title);
    }

    [Fact]
    public async Task UpdateAsync_OfAnEntityThatWasNotRead_IsSavedWhole_SoItIsModified()
    {
        Ledger seeded = (await SeedAsync(1))[0];
        Ledger detached = new(seeded.Id, "whole");
        detached.SetConcurrencyStamp(seeded.ConcurrencyStamp);
        await using AsyncServiceScope scope = Scope();

        Assert.Equal(WriteOutcome.Modified, await Ledgers(scope).UpdateAsync(detached));
        Assert.Equal("whole", (await ReadAsync(seeded.Id))!.Title);
    }

    [Fact]
    public async Task UpdateAsync_OfARecordThatIsGone_IsNotFound_404_AndOfAStaleOne_AConflict_409()
    {
        List<Ledger> seeded = await SeedAsync(2);
        await using AsyncServiceScope scope = Scope();
        Ledger gone = (await Ledgers(scope).FindAsync(seeded[0].Id))!;
        Ledger stale = (await Ledgers(scope).FindAsync(seeded[1].Id))!;
        await using (AsyncServiceScope other = Scope())
        {
            await Ledgers(other).DeleteAsync((await Ledgers(other).FindAsync(seeded[0].Id))!);   // soft deleted: gone to the caller
            Ledger theirs = (await Ledgers(other).FindAsync(seeded[1].Id))!;
            theirs.Title = "theirs";
            await Ledgers(other).UpdateAsync(theirs);
        }

        gone.Title = "late";
        stale.Title = "late";
        RepositoryException missing = await Assert.ThrowsAsync<RepositoryException>(() => Ledgers(scope).UpdateAsync(gone));
        RepositoryException conflict = await Assert.ThrowsAsync<RepositoryException>(() => Ledgers(scope).UpdateAsync(stale));

        Assert.Equal((RepositoryFailure.NotFound, 404, RepositoryException.NotFoundCode, nameof(Ledger)), (missing.Failure, missing.HttpStatusCode, missing.ErrorCode, missing.EntityName));
        Assert.Equal(seeded[0].Id.ToString(), Assert.Single(missing.Ids));
        Assert.Equal((RepositoryFailure.ConcurrencyConflict, 409, RepositoryException.ConflictCode), (conflict.Failure, conflict.HttpStatusCode, conflict.ErrorCode));
        Assert.Equal(seeded[1].Id.ToString(), Assert.Single(conflict.Ids));
    }

    // ---- UpdateMany ----

    [Fact]
    public async Task UpdateManyAsync_SaysHowManyWereWritten_AndLeavesTheOthersAlone()
    {
        List<Ledger> seeded = await SeedAsync(3);
        await using AsyncServiceScope scope = Scope();
        List<Ledger> ledgers = await FindAllAsync(scope, seeded);
        string untouched = ledgers[2].ConcurrencyStamp;
        ledgers[0].Title = "one";
        ledgers[1].Title = "two";

        WriteResult result = await Ledgers(scope).UpdateManyAsync(ledgers);

        Assert.Equal((3, 2, 1), (result.Requested, result.Modified, result.NotModified));
        Assert.Equal(untouched, (await ReadAsync(seeded[2].Id))!.ConcurrencyStamp);
        Assert.Equal("two", (await ReadAsync(seeded[1].Id))!.Title);
        Assert.Equal(new WriteResult(0, 0), await Ledgers(scope).UpdateManyAsync([]));
    }

    [Fact]
    public async Task UpdateManyAsync_WithNothingChanged_IsAllNotModified()
    {
        List<Ledger> seeded = await SeedAsync(2);
        await using AsyncServiceScope scope = Scope();

        WriteResult result = await Ledgers(scope).UpdateManyAsync(await FindAllAsync(scope, seeded));

        Assert.Equal((2, 0, 2), (result.Requested, result.Modified, result.NotModified));
    }

    [Fact]
    public async Task UpdateManyAsync_OneRecordIsGone_NamesIt_AndTheOthersWereWritten_WithoutATransaction()
    {
        List<Ledger> seeded = await SeedAsync(3);
        await using AsyncServiceScope scope = Scope();
        List<Ledger> ledgers = await FindAllAsync(scope, seeded);
        await using (AsyncServiceScope other = Scope())
        {
            await Ledgers(other).DeleteAsync((await Ledgers(other).FindAsync(seeded[1].Id))!);
        }

        ledgers.ForEach(ledger => ledger.Title = "late");
        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Ledgers(scope).UpdateManyAsync(ledgers));

        Assert.Equal((RepositoryFailure.NotFound, 404), (failed.Failure, failed.HttpStatusCode));
        Assert.Equal(seeded[1].Id.ToString(), Assert.Single(failed.Ids));
        // each document is atomic and the batch is not: the two that were there were written (a transaction writes all or none)
        Assert.Equal("late", (await ReadAsync(seeded[0].Id))!.Title);
        Assert.Equal("late", (await ReadAsync(seeded[2].Id))!.Title);
    }

    [Fact]
    public async Task UpdateManyAsync_OneRecordIsStale_IsAConflict_ThatNamesOnlyIt()
    {
        List<Ledger> seeded = await SeedAsync(2);
        await using AsyncServiceScope scope = Scope();
        List<Ledger> ledgers = await FindAllAsync(scope, seeded);
        await using (AsyncServiceScope other = Scope())
        {
            Ledger theirs = (await Ledgers(other).FindAsync(seeded[0].Id))!;
            theirs.Title = "theirs";
            await Ledgers(other).UpdateAsync(theirs);
        }

        ledgers.ForEach(ledger => ledger.Title = "mine");
        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Ledgers(scope).UpdateManyAsync(ledgers));

        Assert.Equal((RepositoryFailure.ConcurrencyConflict, 409), (failed.Failure, failed.HttpStatusCode));
        Assert.Equal(seeded[0].Id.ToString(), Assert.Single(failed.Ids));
        Assert.Equal("theirs", (await ReadAsync(seeded[0].Id))!.Title);
    }

    // ---- Delete ----

    [Fact]
    public async Task DeleteAsync_AndDeleteManyAsync_OfRecordsThatAreGone_AreNotFound_404_AndNameThem()
    {
        Stub never = new(Guid.NewGuid(), "never");
        Stub alsoNever = new(Guid.NewGuid(), "also never");
        await using AsyncServiceScope scope = Scope();

        RepositoryException one = await Assert.ThrowsAsync<RepositoryException>(() => Stubs(scope).DeleteAsync(never));
        RepositoryException many = await Assert.ThrowsAsync<RepositoryException>(() => Stubs(scope).DeleteManyAsync([never, alsoNever]));

        Assert.Equal(never.Id.ToString(), Assert.Single(one.Ids));
        Assert.Equal(404, many.HttpStatusCode);
        Assert.Equal(new[] { never.Id.ToString(), alsoNever.Id.ToString() }.Order(StringComparer.Ordinal), many.Ids.Order(StringComparer.Ordinal));
    }

    [Fact]
    public async Task DeleteManyAsync_OfDocumentsRemovedForGood_ChecksFirst_SoNothingIsRemovedWhenOneIsNotThere()
    {
        await using AsyncServiceScope scope = Scope();
        Stub there = await Stubs(scope).AddAsync(new Stub(Guid.Empty, "there"));
        Stub gone = new(Guid.NewGuid(), "gone");

        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Stubs(scope).DeleteManyAsync([there, gone]));

        Assert.Equal(gone.Id.ToString(), Assert.Single(failed.Ids));
        Assert.NotNull(await Stubs(scope).FindAsync(there.Id));   // a batch that removes documents for good is checked first: the one that was there is still there
    }

    // ---- a server without transactions ----

    [Fact]
    public async Task TheMethodsThatNeedATransaction_SayThatTheServerIsStandalone()
    {
        await using AsyncServiceScope scope = Scope();
        List<Ledger> ledgers = [new("a")];

        await Assert.ThrowsAsync<NotSupportedException>(() => Ledgers(scope).AddManyInTransactionAsync(ledgers));
        await Assert.ThrowsAsync<NotSupportedException>(() => Ledgers(scope).UpdateManyInTransactionAsync(ledgers));
        await Assert.ThrowsAsync<NotSupportedException>(() => Ledgers(scope).DeleteManyInTransactionAsync(ledgers));
        await Assert.ThrowsAsync<NotSupportedException>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ExecuteAsync(_ => Task.CompletedTask));
        Assert.Equal(0, await Ledgers(scope).CountAsync());
    }

    [Fact]
    public async Task TheTransactionalMethods_RejectWhatIsNotGiven()
    {
        await using AsyncServiceScope scope = Scope();

        await Assert.ThrowsAsync<ArgumentNullException>(() => Ledgers(scope).AddManyInTransactionAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Ledgers(scope).UpdateManyInTransactionAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Ledgers(scope).DeleteManyInTransactionAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => scope.ServiceProvider.GetRequiredService<IUnitOfWork>().ExecuteAsync((Func<CancellationToken, Task>)null!));
        Assert.Throws<ArgumentNullException>(() => MongoUnitOfWorkServiceCollectionExtensions.AddMongoUnitOfWork(null!));
    }

    // ---- a duplicate of a unique index ----

    [Fact]
    public async Task AValueThatAnotherDocumentHas_IsADuplicateKey_409_NamingTheIndex_ForEveryWrite()
    {
        await using AsyncServiceScope scope = Scope();
        await Stubs(scope).AddAsync(new Stub(Guid.Empty, "taken"));

        RepositoryException add = await Assert.ThrowsAsync<RepositoryException>(() => Stubs(scope).AddAsync(new Stub(Guid.Empty, "taken")));
        Stub other = await Stubs(scope).AddAsync(new Stub(Guid.Empty, "free"));
        other.Label = "taken";
        RepositoryException update = await Assert.ThrowsAsync<RepositoryException>(() => Stubs(scope).UpdateAsync(other));
        other.Label = "taken";
        RepositoryException updateMany = await Assert.ThrowsAsync<RepositoryException>(() => Stubs(scope).UpdateManyAsync([new Stub(other.Id, "taken")]));

        foreach (RepositoryException failed in new[] { add, update, updateMany })
        {
            Assert.Equal((RepositoryFailure.DuplicateKey, 409, RepositoryException.DuplicateCode, nameof(Stub)), (failed.Failure, failed.HttpStatusCode, failed.ErrorCode, failed.EntityName));
            Assert.Equal("ux_stub_label", failed.Constraint);
            Assert.DoesNotContain("taken", failed.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public async Task AddManyAsync_WithADuplicate_IsADuplicateKey_AndRemovesWhatItHadAdded()
    {
        await using AsyncServiceScope scope = Scope();
        await Stubs(scope).AddAsync(new Stub(Guid.Empty, "existing"));
        List<Stub> batch = [new(Guid.Empty, "first"), new(Guid.Empty, "second"), new(Guid.Empty, "existing"), new(Guid.Empty, "fourth")];

        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Stubs(scope).AddManyAsync(batch));

        Assert.Equal(("ux_stub_label", RepositoryFailure.DuplicateKey), (failed.Constraint, failed.Failure));
        Assert.Equal(1, await Stubs(scope).CountAsync());   // first and second were added and removed again, the fourth never went in
    }

    [Fact]
    public async Task AWriteThatFailsForAnotherReason_IsNotTurnedIntoADuplicateKey()
    {
        using CancellationTokenSource cancelled = new();
        await cancelled.CancelAsync();
        await using AsyncServiceScope scope = Scope();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Stubs(scope).AddAsync(new Stub(Guid.Empty, "never"), cancelled.Token));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => Stubs(scope).UpdateManyAsync([new Stub(Guid.NewGuid(), "never")], cancelled.Token));
    }
}
