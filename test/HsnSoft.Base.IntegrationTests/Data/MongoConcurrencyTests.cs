using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;

namespace HsnSoft.Base.IntegrationTests.Data;

/// <summary>A document with a stamp is replaced or removed only where its stamp is the one the caller saw; any other is a 409 and the document is as it was.</summary>
[Collection(Collections.Mongo)]
public sealed class MongoConcurrencyTests(MongoFixture mongo) : IAsyncLifetime
{
    private IHost _host = null!;

    public sealed class Memo(string title) : Entity<Guid>(Guid.NewGuid()), IHasConcurrencyStamp
    {
        public string Title { get; set; } = title;

        public string ConcurrencyStamp { get; private set; } = string.Empty;

        public void SetConcurrencyStamp(string stamp) => ConcurrencyStamp = stamp;
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
        if (!BsonClassMap.IsClassMapRegistered(typeof(Memo)))
        {
            BsonClassMap.RegisterClassMap<Memo>(map =>
            {
                map.MapProperty(memo => memo.Title);
                map.MapProperty(memo => memo.ConcurrencyStamp);
                map.MapCreator(memo => new Memo(memo.Title));
            });
        }

        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Mongo", mongo.NewUrl()));
        _host = await Support.StartHostAsync(services => services
            .AddSingleton<ICurrentUser>(new FakeUser())
            .AddBaseMongo(configuration)
            .AddMongoRepository<Memo, Guid>("memos"));
    }

    public Task DisposeAsync()
    {
        _host.Dispose();
        return Task.CompletedTask;
    }

    private IRepository<Memo, Guid> Memos(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IRepository<Memo, Guid>>();

    private async Task<Memo> AddAsync(string title)
    {
        Memo memo = new(title);
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        await Memos(scope).AddAsync(memo);
        return memo;
    }

    private async Task ChangeBySomeoneElseAsync(Guid id)
    {
        await using AsyncServiceScope other = _host.Services.CreateAsyncScope();
        Memo loaded = (await Memos(other).FindAsync(id))!;
        loaded.Title = "by someone else";
        await Memos(other).UpdateAsync(loaded);
    }

    private static void AssertConflict(BaseHttpException exception)
    {
        Assert.Equal(409, exception.HttpStatusCode);
        Assert.Equal(ConcurrencyConflict.MessageKey, exception.MessageKey);
    }

    [Fact]
    public async Task AnAddedDocument_HasAStamp_AndEveryChangeGivesItANewOne()
    {
        Memo memo = await AddAsync("first");
        string added = memo.ConcurrencyStamp;

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        Memo loaded = (await Memos(scope).FindAsync(memo.Id))!;
        loaded.Title = "second";
        await Memos(scope).UpdateAsync(loaded);

        Memo stored = (await Memos(scope).FindAsync(memo.Id))!;
        Assert.NotEmpty(added);
        Assert.Equal("second", stored.Title);
        Assert.NotEqual(added, stored.ConcurrencyStamp);
    }

    [Fact]
    public async Task AReplace_WithTheStampTheCallerSaw_Works_AndWithAStaleOne_Is409()
    {
        Memo memo = await AddAsync("first");
        string seen = memo.ConcurrencyStamp;
        await ChangeBySomeoneElseAsync(memo.Id);

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        Memo stale = new("lost update");
        stale.SetConcurrencyStamp(seen);
        typeof(Entity<Guid>).GetProperty(nameof(Entity<>.Id))!.SetValue(stale, memo.Id);
        AssertConflict(await Assert.ThrowsAsync<BaseHttpException>(() => Memos(scope).UpdateAsync(stale)));

        Memo current = (await Memos(scope).FindAsync(memo.Id))!;
        Assert.Equal("by someone else", current.Title);
        current.Title = "saw it";
        await Memos(scope).UpdateAsync(current);
        Assert.Equal("saw it", (await Memos(scope).FindAsync(memo.Id))!.Title);
    }

    [Fact]
    public async Task ABulkReplace_AndADelete_WithAStaleStamp_AreAlso409()
    {
        Memo memo = await AddAsync("first");
        string seen = memo.ConcurrencyStamp;
        await ChangeBySomeoneElseAsync(memo.Id);

        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        Memo bulk = (await Memos(scope).FindAsync(memo.Id))!;
        bulk.SetConcurrencyStamp(seen);
        AssertConflict(await Assert.ThrowsAsync<BaseHttpException>(() => Memos(scope).UpdateManyAsync([bulk])));

        Memo toDelete = (await Memos(scope).FindAsync(memo.Id))!;
        toDelete.SetConcurrencyStamp(seen);
        AssertConflict(await Assert.ThrowsAsync<BaseHttpException>(() => Memos(scope).DeleteAsync(toDelete)));
        Memo toDeleteMany = (await Memos(scope).FindAsync(memo.Id))!;
        toDeleteMany.SetConcurrencyStamp(seen);
        AssertConflict(await Assert.ThrowsAsync<BaseHttpException>(() => Memos(scope).DeleteManyAsync([toDeleteMany])));

        Memo fresh = (await Memos(scope).FindAsync(memo.Id))!;
        await Memos(scope).DeleteAsync(fresh);
        Assert.Null(await Memos(scope).FindAsync(memo.Id));
    }

    [Fact]
    public async Task AnUpdateThatLoadsNothing_GivesANewStamp()
    {
        Memo memo = await AddAsync("first");
        await using AsyncServiceScope scope = _host.Services.CreateAsyncScope();
        Memo seen = (await Memos(scope).FindAsync(memo.Id))!;

        Assert.Equal(1, await scope.ServiceProvider.GetRequiredService<IMongoRepository<Memo, Guid>>().UpdateWhereAsync(candidate => candidate.Id == memo.Id, update => update.Set(candidate => candidate.Title, "by statement")));

        seen.Title = "stale";
        AssertConflict(await Assert.ThrowsAsync<BaseHttpException>(() => Memos(scope).UpdateAsync(seen)));
    }
}
