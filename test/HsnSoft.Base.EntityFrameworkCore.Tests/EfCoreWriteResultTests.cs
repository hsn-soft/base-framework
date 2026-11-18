using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore.Tests;

/// <summary>
/// What a write of the repository gives back and how it fails: the same instance (so no second read), a key for an entity that has none, <see cref="WriteOutcome.NotModified"/> when nothing changed, and one <see cref="RepositoryException"/> that names every record that was
/// not there or not current. A relational store writes all or nothing: one <c>SaveChanges</c> is one transaction.
/// </summary>
public sealed class EfCoreWriteResultTests : IAsyncLifetime, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ServiceProvider _provider = null!;

    public sealed class Doc(Guid id, string title) : FullAuditedEntity<Guid>(id), IHasConcurrencyStamp
    {
        public Doc(string title)
            : this(Guid.Empty, title)
        {
        }

        public string Title { get; set; } = title;

        public string ConcurrencyStamp { get; private set; } = ConcurrencyStamps.New();

        public void SetConcurrencyStamp(string stamp) => ConcurrencyStamp = stamp;
    }

    public sealed class Plain(int id, string title) : Entity<int>(id)
    {
        public string Title { get; set; } = title;
    }

    public sealed class Badge(Guid id, string code) : Entity<Guid>(id)
    {
        public string Code { get; set; } = code;
    }

    public sealed class WriteContext(DbContextOptions<WriteContext> options, HsnSoft.Base.Data.IDataFilter? dataFilter = null) : BaseDbContext(options, dataFilter)
    {
        public DbSet<Doc> Docs => Set<Doc>();

        public DbSet<Plain> Plains => Set<Plain>();

        public DbSet<Badge> Badges => Set<Badge>();

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Badge>().Property(badge => badge.Id).ValueGeneratedNever();
            modelBuilder.Entity<Badge>().HasIndex(badge => badge.Code).IsUnique();
            modelBuilder.Entity<Doc>().Property(doc => doc.Id).ValueGeneratedNever();
            modelBuilder.Entity<Plain>().Property(plain => plain.Id).ValueGeneratedNever();
        }
    }

    private sealed class FakeUser : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid? Id { get; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

        public string? UserName => null;
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        ServiceCollection services = new();
        services.AddLogging().AddSingleton<ICurrentUser>(new FakeUser()).AddBaseEntityFramework();
        services.AddDbContext<WriteContext>((provider, options) => options.UseSqlite(_connection).UseBaseEntityConcepts(provider));
        services.AddEfCoreRepository<WriteContext, Doc, Guid>().AddEfCoreRepository<WriteContext, Plain, int>().AddEfCoreRepository<WriteContext, Badge, Guid>();
        _provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<WriteContext>().Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public void Dispose() => _connection.Dispose();

    private static IRepository<Doc, Guid> Docs(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IRepository<Doc, Guid>>();

    private static IRepository<Plain, int> Plains(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IRepository<Plain, int>>();

    private static IRepository<Badge, Guid> Badges(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IRepository<Badge, Guid>>();

    private async Task<List<Doc>> SeedAsync(int count)
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        List<Doc> docs = [.. Enumerable.Range(1, count).Select(number => new Doc($"doc {number}"))];
        await Docs(scope).AddManyAsync(docs);
        return docs;
    }

    // read with FindAsync: those are followed (an entity of a list is not), so a change is told from none
    private static async Task<List<Doc>> FindAllAsync(AsyncServiceScope scope, IEnumerable<Doc> seeded)
    {
        List<Doc> found = [];
        foreach (Doc doc in seeded)
        {
            found.Add((await Docs(scope).FindAsync(doc.Id))!);
        }

        return found;
    }

    private async Task<Doc?> ReadAsync(Guid id)
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        return await Docs(scope).FindAsync(id);
    }

    // ---- Add ----

    [Fact]
    public async Task AddAsync_GivesBackTheSameInstance_WithWhatTheStoreWrote()
    {
        Doc doc = new("first");
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        Doc added = await Docs(scope).AddAsync(doc);

        Assert.Same(doc, added);
        Assert.NotEqual(Guid.Empty, added.Id);
        Assert.Equal(Guid.Parse("11111111-1111-1111-1111-111111111111"), added.CreatedBy);
        Assert.NotEqual(default, added.CreatedAt);
        Assert.False(string.IsNullOrEmpty(added.ConcurrencyStamp));
    }

    [Fact]
    public async Task AnEmptyGuidKey_IsGivenByTheStore_AndAKeyThatWasGiven_IsKept()
    {
        Guid chosen = Guid.NewGuid();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        Doc made = await Docs(scope).AddAsync(new Doc("no key"));
        Doc kept = await Docs(scope).AddAsync(new Doc(chosen, "my key"));

        Assert.NotEqual(Guid.Empty, made.Id);
        Assert.Equal(7, made.Id.Version);   // version 7: a key made later sorts after one made earlier
        Assert.Equal(chosen, kept.Id);
        Assert.NotNull(await ReadAsync(made.Id));
        Assert.NotNull(await ReadAsync(chosen));
    }

    [Fact]
    public async Task AKeyThatIsNotAGuid_IsLeftToTheStore()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        Plain plain = await Plains(scope).AddAsync(new Plain(7, "seven"));

        Assert.Equal(7, plain.Id);
    }

    [Fact]
    public async Task AddManyAsync_GivesKeys_AndAddsAllOrNone()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        List<Doc> docs = [new("a"), new("b"), new("c")];

        await Docs(scope).AddManyAsync(docs);

        Assert.All(docs, doc => Assert.NotEqual(Guid.Empty, doc.Id));
        Assert.Equal(3, docs.Select(doc => doc.Id).Distinct().Count());

        // the third has the key of the first: the database refuses it (a duplicate key), and the first two are not there either (one SaveChanges is one transaction)
        Guid taken = docs[0].Id;
        await using AsyncServiceScope second = _provider.CreateAsyncScope();
        List<Doc> batch = [new("d"), new("e"), new(taken, "again")];
        Assert.Equal(RepositoryFailure.DuplicateKey, (await Assert.ThrowsAsync<RepositoryException>(() => Docs(second).AddManyAsync(batch))).Failure);

        await using AsyncServiceScope check = _provider.CreateAsyncScope();
        Assert.Equal(3, await Docs(check).CountAsync());
        Assert.Null(await Docs(check).FindAsync(batch[0].Id));
        Assert.Null(await Docs(check).FindAsync(batch[1].Id));
    }

    // ---- Update ----

    [Fact]
    public async Task UpdateAsync_OfAnEntityThatDidNotChange_IsNotModified_AndWritesNothing()
    {
        Doc seeded = (await SeedAsync(1))[0];
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        Doc doc = (await Docs(scope).FindAsync(seeded.Id))!;
        (string stamp, DateTimeOffset? updatedAt) = (doc.ConcurrencyStamp, doc.UpdatedAt);

        WriteOutcome outcome = await Docs(scope).UpdateAsync(doc);

        Assert.Equal(WriteOutcome.NotModified, outcome);
        Assert.Equal((stamp, updatedAt), (doc.ConcurrencyStamp, doc.UpdatedAt));   // the stamp and the audit fields stay: nothing was written
        Assert.Equal(stamp, (await ReadAsync(seeded.Id))!.ConcurrencyStamp);
    }

    [Fact]
    public async Task UpdateAsync_OfAnEntityThatChanged_IsModified_AndGivesANewStamp()
    {
        Doc seeded = (await SeedAsync(1))[0];
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        Doc doc = (await Docs(scope).FindAsync(seeded.Id))!;
        string before = doc.ConcurrencyStamp;

        doc.Title = "changed";
        WriteOutcome outcome = await Docs(scope).UpdateAsync(doc);

        Assert.Equal(WriteOutcome.Modified, outcome);
        Assert.NotEqual(before, doc.ConcurrencyStamp);
        Assert.Equal("changed", (await ReadAsync(seeded.Id))!.Title);
    }

    [Fact]
    public async Task UpdateAsync_OfAnEntityThatWasNotRead_IsSavedWhole_SoItIsAlwaysModified()
    {
        Doc seeded = (await SeedAsync(1))[0];
        Doc detached = new(seeded.Id, "whole");
        detached.SetConcurrencyStamp(seeded.ConcurrencyStamp);
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        Assert.Equal(WriteOutcome.Modified, await Docs(scope).UpdateAsync(detached));
        Assert.Equal("whole", (await ReadAsync(seeded.Id))!.Title);
    }

    [Fact]
    public async Task UpdateAsync_OfARecordThatIsGone_IsNotFound_404()
    {
        Doc seeded = (await SeedAsync(1))[0];
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        Doc doc = (await Docs(scope).FindAsync(seeded.Id))!;
        await using (AsyncServiceScope other = _provider.CreateAsyncScope())
        {
            await Docs(other).DeleteAsync((await Docs(other).FindAsync(seeded.Id))!);
        }

        doc.Title = "late";
        RepositoryException gone = await Assert.ThrowsAsync<RepositoryException>(() => Docs(scope).UpdateAsync(doc));

        Assert.Equal((RepositoryFailure.NotFound, 404, RepositoryException.NotFoundCode, nameof(Doc)), (gone.Failure, gone.HttpStatusCode, gone.ErrorCode, gone.EntityName));
        Assert.Equal(seeded.Id.ToString(), Assert.Single(gone.Ids));
    }

    // ---- UpdateMany ----

    [Fact]
    public async Task UpdateManyAsync_SaysHowManyWereWritten_AndLeavesTheOthersAlone()
    {
        List<Doc> seeded = await SeedAsync(3);
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        List<Doc> docs = await FindAllAsync(scope, seeded);
        string untouchedStamp = docs.First(doc => doc.Id == seeded[2].Id).ConcurrencyStamp;
        docs.First(doc => doc.Id == seeded[0].Id).Title = "one";
        docs.First(doc => doc.Id == seeded[1].Id).Title = "two";

        WriteResult result = await Docs(scope).UpdateManyAsync(docs);

        Assert.Equal((3, 2, 1), (result.Requested, result.Modified, result.NotModified));
        Assert.Equal(untouchedStamp, (await ReadAsync(seeded[2].Id))!.ConcurrencyStamp);
        Assert.Equal("one", (await ReadAsync(seeded[0].Id))!.Title);
    }

    [Fact]
    public async Task AnEntityOfAList_IsNotFollowed_SoItIsSavedWholeAndIsModified()
    {
        List<Doc> seeded = await SeedAsync(2);
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        List<Doc> docs = [.. await Docs(scope).GetListAsync(new ListQueryOptions<Doc>())];

        Assert.Equal((2, 2, 0), (await Docs(scope).UpdateManyAsync(docs) is var result ? (result.Requested, result.Modified, result.NotModified) : default));

        // another read of the list, another scope: one entity of it is saved whole as well
        await using AsyncServiceScope again = _provider.CreateAsyncScope();
        List<Doc> second = [.. await Docs(again).GetListAsync(new ListQueryOptions<Doc>())];
        Assert.Equal(WriteOutcome.Modified, await Docs(again).UpdateAsync(second[0]));
        Assert.Equal(2, seeded.Count);
    }

    [Fact]
    public async Task UpdateManyAsync_WithNothingChanged_IsAllNotModified()
    {
        List<Doc> seeded = await SeedAsync(2);
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        List<Doc> docs = await FindAllAsync(scope, seeded);

        WriteResult result = await Docs(scope).UpdateManyAsync(docs);

        Assert.Equal((2, 0, 2), (result.Requested, result.Modified, result.NotModified));
        Assert.Equal((0, 0, 0), (await Docs(scope).UpdateManyAsync([]) is var empty ? (empty.Requested, empty.Modified, empty.NotModified) : (-1, -1, -1)));
    }

    [Fact]
    public async Task UpdateManyAsync_OneRecordIsGone_FailsAll_NamesIt_AndWritesNothing()
    {
        List<Doc> seeded = await SeedAsync(3);
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        List<Doc> docs = await FindAllAsync(scope, seeded);
        await using (AsyncServiceScope other = _provider.CreateAsyncScope())
        {
            await Docs(other).DeleteAsync((await Docs(other).FindAsync(seeded[1].Id))!);
        }

        docs.ForEach(doc => doc.Title = "late");
        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Docs(scope).UpdateManyAsync(docs));

        Assert.Equal((RepositoryFailure.NotFound, 404), (failed.Failure, failed.HttpStatusCode));
        Assert.Equal(seeded[1].Id.ToString(), Assert.Single(failed.Ids));
        Assert.Equal("doc 1", (await ReadAsync(seeded[0].Id))!.Title);   // nothing was written: the transaction was rolled back
        Assert.Equal("doc 3", (await ReadAsync(seeded[2].Id))!.Title);
    }

    [Fact]
    public async Task UpdateManyAsync_OneRecordIsStale_IsAConflict_409_ThatNamesIt()
    {
        List<Doc> seeded = await SeedAsync(2);
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        List<Doc> docs = await FindAllAsync(scope, seeded);
        await using (AsyncServiceScope other = _provider.CreateAsyncScope())
        {
            Doc theirs = (await Docs(other).FindAsync(seeded[0].Id))!;
            theirs.Title = "theirs";
            await Docs(other).UpdateAsync(theirs);
        }

        docs.ForEach(doc => doc.Title = "mine");
        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Docs(scope).UpdateManyAsync(docs));

        Assert.Equal((RepositoryFailure.ConcurrencyConflict, 409, RepositoryException.ConflictCode), (failed.Failure, failed.HttpStatusCode, failed.ErrorCode));
        Assert.Contains(seeded[0].Id.ToString(), failed.Ids);
        Assert.Equal("theirs", (await ReadAsync(seeded[0].Id))!.Title);
        Assert.Equal("doc 2", (await ReadAsync(seeded[1].Id))!.Title);   // all or nothing
    }

    // ---- Delete ----

    [Fact]
    public async Task DeleteAsync_OfARecordThatIsGone_IsNotFound_404()
    {
        Plain never = new(99, "never");
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Plains(scope).DeleteAsync(never));

        Assert.Equal((RepositoryFailure.NotFound, 404, nameof(Plain)), (failed.Failure, failed.HttpStatusCode, failed.EntityName));
        Assert.Equal("99", Assert.Single(failed.Ids));
    }

    [Fact]
    public async Task DeleteManyAsync_OneRecordIsGone_FailsAll_NamesIt_AndDeletesNone()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await Plains(scope).AddManyAsync([new Plain(1, "a"), new Plain(2, "b")]);
        await using AsyncServiceScope other = _provider.CreateAsyncScope();

        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Plains(other).DeleteManyAsync([new Plain(1, "a"), new Plain(3, "never")]));

        Assert.Equal((RepositoryFailure.NotFound, "3"), (failed.Failure, Assert.Single(failed.Ids)));
        await using AsyncServiceScope check = _provider.CreateAsyncScope();
        Assert.Equal(2, await Plains(check).CountAsync());
    }

    [Fact]
    public async Task DeleteManyAsync_DeletesWhatIsThere_AndSoftDeletesWhatCanBe()
    {
        List<Doc> seeded = await SeedAsync(2);
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        List<Doc> docs = await FindAllAsync(scope, seeded);

        await Docs(scope).DeleteManyAsync(docs);

        await using AsyncServiceScope check = _provider.CreateAsyncScope();
        Assert.Equal(0, await Docs(check).CountAsync());   // soft deleted: the filter hides them
        Assert.Equal(2, seeded.Count);
    }

    // ---- the exception ----

    [Fact]
    public async Task TheFailureIsOneException_ThatABusinessCodeCanCatch_AndTheClientCanRead()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Plains(scope).DeleteAsync(new Plain(5, "x")));

        Assert.IsAssignableFrom<BaseHttpException>(failed);
        Assert.Equal(RepositoryException.NotFoundMessageKey, failed.MessageKey);
        Assert.Equal(nameof(Plain), Assert.Single(failed.MessageArgs));
        Assert.Equal(nameof(Plain), failed.Data["entity"]);
        Assert.Equal("5", failed.Data["ids"]);
    }

    // ---- a duplicate of a unique key or index ----

    [Fact]
    public async Task AddAsync_OfAValueAnotherRecordHas_IsADuplicateKey_409_NamingTheIndex()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await Badges(scope).AddAsync(new Badge(Guid.NewGuid(), "A-1"));

        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Badges(scope).AddAsync(new Badge(Guid.NewGuid(), "A-1")));

        Assert.Equal((RepositoryFailure.DuplicateKey, 409, RepositoryException.DuplicateCode, nameof(Badge)), (failed.Failure, failed.HttpStatusCode, failed.ErrorCode, failed.EntityName));
        Assert.Contains("Code", failed.Constraint, StringComparison.Ordinal);   // SQLite names the columns
        Assert.IsAssignableFrom<DbUpdateException>(failed.InnerException);
        Assert.DoesNotContain("A-1", failed.Message, StringComparison.Ordinal);   // the value that is duplicated is not told
        Assert.DoesNotContain("A-1", string.Join(' ', failed.Data.Values.Cast<object>()), StringComparison.Ordinal);
    }

    [Fact]
    public async Task AKeyThatIsTaken_IsADuplicateKey_Too()
    {
        Guid id = Guid.NewGuid();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await Badges(scope).AddAsync(new Badge(id, "B-1"));
        await using AsyncServiceScope other = _provider.CreateAsyncScope();

        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Badges(other).AddAsync(new Badge(id, "B-2")));

        Assert.Equal(RepositoryFailure.DuplicateKey, failed.Failure);
    }

    [Fact]
    public async Task AddManyAsync_WithADuplicate_IsADuplicateKey_AndAddsNone()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await Badges(scope).AddAsync(new Badge(Guid.NewGuid(), "C-1"));
        await using AsyncServiceScope other = _provider.CreateAsyncScope();

        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Badges(other).AddManyAsync([new Badge(Guid.NewGuid(), "C-2"), new Badge(Guid.NewGuid(), "C-1"), new Badge(Guid.NewGuid(), "C-3")]));

        Assert.Equal(RepositoryFailure.DuplicateKey, failed.Failure);
        await using AsyncServiceScope check = _provider.CreateAsyncScope();
        Assert.Equal(1, await Badges(check).CountAsync());   // C-2 and C-3 are not there: one transaction
    }

    [Fact]
    public async Task UpdateAsync_ToAValueAnotherRecordHas_IsADuplicateKey()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await Badges(scope).AddAsync(new Badge(Guid.NewGuid(), "D-1"));
        Badge second = await Badges(scope).AddAsync(new Badge(Guid.NewGuid(), "D-2"));

        second.Code = "D-1";
        RepositoryException failed = await Assert.ThrowsAsync<RepositoryException>(() => Badges(scope).UpdateAsync(second));

        Assert.Equal(RepositoryFailure.DuplicateKey, failed.Failure);
    }
}
