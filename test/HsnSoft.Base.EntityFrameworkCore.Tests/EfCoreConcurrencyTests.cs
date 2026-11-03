using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore.Tests;

/// <summary>An entity with a stamp is protected from a lost update: a change made since the caller read it is a 409, never a silent overwrite.</summary>
public sealed class EfCoreConcurrencyTests : IAsyncLifetime, IDisposable
{
    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private ServiceProvider _provider = null!;

    public sealed class Doc(string title) : FullAuditedEntity<Guid>(Guid.NewGuid()), IHasConcurrencyStamp
    {
        public string Title { get; set; } = title;

        public string ConcurrencyStamp { get; private set; } = ConcurrencyStamps.New();

        public void SetConcurrencyStamp(string stamp) => ConcurrencyStamp = stamp;
    }

    public sealed class Plain(int id, string title) : Entity<int>(id), IHasConcurrencyStamp
    {
        public string Title { get; set; } = title;

        public string ConcurrencyStamp { get; private set; } = ConcurrencyStamps.New();

        public void SetConcurrencyStamp(string stamp) => ConcurrencyStamp = stamp;
    }

    public sealed class ConcurrencyContext(DbContextOptions<ConcurrencyContext> options, HsnSoft.Base.Data.IDataFilter? dataFilter = null) : BaseDbContext(options, dataFilter)
    {
        public DbSet<Doc> Docs => Set<Doc>();

        public DbSet<Plain> Plains => Set<Plain>();

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Doc>().Property(doc => doc.Id).ValueGeneratedNever();
            modelBuilder.Entity<Plain>().Property(plain => plain.Id).ValueGeneratedNever();
        }
    }

    private sealed class FakeUser : ICurrentUser
    {
        public bool IsAuthenticated => false;

        public Guid? Id => null;

        public string? UserName => null;
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        ServiceCollection services = new();
        services.AddLogging().AddSingleton<ICurrentUser>(new FakeUser()).AddBaseEntityFramework();
        services.AddDbContext<ConcurrencyContext>((provider, options) => options.UseSqlite(_connection).UseBaseEntityConcepts(provider));
        services.AddEfCoreRepository<ConcurrencyContext, Doc, Guid>().AddEfCoreRepository<ConcurrencyContext, Plain, int>();
        _provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<ConcurrencyContext>().Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public void Dispose() => _connection.Dispose();

    private static IRepository<Doc, Guid> Docs(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IRepository<Doc, Guid>>();

    private static IEfCoreRepository<Doc, Guid> EfDocs(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IEfCoreRepository<Doc, Guid>>();

    private static IRepository<Plain, int> Plains(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IRepository<Plain, int>>();

    private async Task<Doc> AddDocAsync()
    {
        Doc doc = new("first");
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await Docs(scope).AddAsync(doc);
        return doc;
    }

    private static void AssertConflict(BaseHttpException exception)
    {
        Assert.Equal(409, exception.HttpStatusCode);
        Assert.Equal(ConcurrencyConflict.MessageKey, exception.MessageKey);
    }

    [Fact]
    public void TheStampIsTheConcurrencyToken_AndHasAMaximumLength()
    {
        using AsyncServiceScope scope = _provider.CreateAsyncScope();
        Microsoft.EntityFrameworkCore.Metadata.IProperty stamp = scope.ServiceProvider.GetRequiredService<ConcurrencyContext>().Model.FindEntityType(typeof(Doc))!.FindProperty(nameof(IHasConcurrencyStamp.ConcurrencyStamp))!;

        Assert.True(stamp.IsConcurrencyToken);
        Assert.Equal(ConcurrencyStamps.MaxLength, stamp.GetMaxLength());
    }

    [Fact]
    public async Task AChange_GivesTheEntityANewStamp()
    {
        Doc doc = await AddDocAsync();
        string first = doc.ConcurrencyStamp;

        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            Doc loaded = (await Docs(scope).FindAsync(doc.Id))!;
            loaded.Title = "second";
            await Docs(scope).UpdateAsync(loaded);
        }

        await using AsyncServiceScope read = _provider.CreateAsyncScope();
        Doc stored = (await Docs(read).FindAsync(doc.Id))!;
        Assert.Equal("second", stored.Title);
        Assert.NotEqual(first, stored.ConcurrencyStamp);
    }

    [Fact]
    public async Task AChange_WithTheStampTheCallerSaw_IsSaved()
    {
        Doc doc = await AddDocAsync();

        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        Doc loaded = (await Docs(scope).FindAsync(doc.Id))!;
        loaded.SetConcurrencyStamp(doc.ConcurrencyStamp);
        loaded.Title = "second";
        await Docs(scope).UpdateAsync(loaded);

        await using AsyncServiceScope read = _provider.CreateAsyncScope();
        Assert.Equal("second", (await Docs(read).FindAsync(doc.Id))!.Title);
    }

    [Fact]
    public async Task AChange_WithAStampThatIsNoLongerTheStoredOne_Is409_AndChangesNothing()
    {
        Doc doc = await AddDocAsync();
        string seen = doc.ConcurrencyStamp;
        await using (AsyncServiceScope other = _provider.CreateAsyncScope())
        {
            Doc loaded = (await Docs(other).FindAsync(doc.Id))!;
            loaded.Title = "by someone else";
            await Docs(other).UpdateAsync(loaded);
        }

        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            Doc loaded = (await Docs(scope).FindAsync(doc.Id))!;
            loaded.SetConcurrencyStamp(seen);
            loaded.Title = "lost update";
            AssertConflict(await Assert.ThrowsAsync<BaseHttpException>(() => Docs(scope).UpdateAsync(loaded)));
        }

        await using AsyncServiceScope read = _provider.CreateAsyncScope();
        Assert.Equal("by someone else", (await Docs(read).FindAsync(doc.Id))!.Title);
    }

    [Fact]
    public async Task TwoReadersOfTheSameEntity_TheSecondToSaveGetsA409()
    {
        Doc doc = await AddDocAsync();
        await using AsyncServiceScope first = _provider.CreateAsyncScope();
        await using AsyncServiceScope second = _provider.CreateAsyncScope();
        Doc ofFirst = (await Docs(first).FindAsync(doc.Id))!;
        Doc ofSecond = (await Docs(second).FindAsync(doc.Id))!;

        ofFirst.Title = "first wins";
        await Docs(first).UpdateAsync(ofFirst);
        ofSecond.Title = "second loses";

        AssertConflict(await Assert.ThrowsAsync<BaseHttpException>(() => Docs(second).UpdateAsync(ofSecond)));
    }

    [Fact]
    public async Task AChange_OfAnEntityThatWasRemoved_Is409()
    {
        Plain plain = new(1, "gone");
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await Plains(scope).AddAsync(plain);
        await using (AsyncServiceScope other = _provider.CreateAsyncScope())
        {
            await Plains(other).DeleteAsync((await Plains(other).FindAsync(1))!);
        }

        plain.Title = "still here?";
        AssertConflict(await Assert.ThrowsAsync<BaseHttpException>(() => Plains(scope).UpdateAsync(plain)));
    }

    [Fact]
    public async Task ADelete_WithAStaleStamp_Is409_ForAHardAndASoftDelete()
    {
        Plain plain = new(2, "hard");
        Doc doc = await AddDocAsync();
        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            await Plains(scope).AddAsync(plain);
        }

        await using (AsyncServiceScope other = _provider.CreateAsyncScope())
        {
            Plain loaded = (await Plains(other).FindAsync(2))!;
            loaded.Title = "changed";
            await Plains(other).UpdateAsync(loaded);
            Doc loadedDoc = (await Docs(other).FindAsync(doc.Id))!;
            loadedDoc.Title = "changed";
            await Docs(other).UpdateAsync(loadedDoc);
        }

        await using AsyncServiceScope scope2 = _provider.CreateAsyncScope();
        Plain hard = (await Plains(scope2).FindAsync(2))!;
        hard.SetConcurrencyStamp(plain.ConcurrencyStamp);
        AssertConflict(await Assert.ThrowsAsync<BaseHttpException>(() => Plains(scope2).DeleteAsync(hard)));
        Doc soft = (await Docs(scope2).FindAsync(doc.Id))!;
        soft.SetConcurrencyStamp(doc.ConcurrencyStamp);
        AssertConflict(await Assert.ThrowsAsync<BaseHttpException>(() => Docs(scope2).DeleteAsync(soft)));
    }

    [Fact]
    public async Task ADelete_WithTheCurrentStamp_Works()
    {
        Plain plain = new(3, "bye");
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await Plains(scope).AddAsync(plain);

        await Plains(scope).DeleteAsync(plain);

        Assert.Null(await Plains(scope).FindAsync(3));
    }

    [Fact]
    public async Task AStatementThatChangesRows_GivesThemANewStamp_SoAnEarlierReaderIsStale()
    {
        Doc doc = await AddDocAsync();
        await using AsyncServiceScope reader = _provider.CreateAsyncScope();
        Doc seen = (await Docs(reader).FindAsync(doc.Id))!;

        await using (AsyncServiceScope writer = _provider.CreateAsyncScope())
        {
            Assert.Equal(1, await EfDocs(writer).UpdateWhereAsync(candidate => candidate.Id == doc.Id, set => set.SetProperty(candidate => candidate.Title, "by statement")));
        }

        seen.Title = "stale";
        AssertConflict(await Assert.ThrowsAsync<BaseHttpException>(() => Docs(reader).UpdateAsync(seen)));
    }
}
