using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Sorting;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore.Tests;

public sealed class EfCoreRepositoryTests : IAsyncLifetime, IDisposable
{
    private static readonly Guid s_tenantA = Guid.NewGuid();
    private static readonly Guid s_tenantB = Guid.NewGuid();

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly FakeUser _user = new() { Id = Guid.NewGuid(), TenantId = s_tenantA };
    private ServiceProvider _provider = null!;

    public sealed class Item(string name, int price) : FullAuditedEntity<Guid>(Guid.NewGuid()), IMultiTenant
    {
        public string Name { get; set; } = name;

        public int Price { get; set; } = price;

        public Guid TenantId { get; private set; }
    }

    public sealed class Counter(int id) : Entity<int>(id)
    {
        public int Value { get; set; }
    }

    public sealed class RepositoryContext(DbContextOptions<RepositoryContext> options, HsnSoft.Base.Data.IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
        : BaseDbContext(options, dataFilter, currentTenant)
    {
        public DbSet<Item> Items => Set<Item>();

        public DbSet<Counter> Counters => Set<Counter>();

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Item>().Property(item => item.Id).ValueGeneratedNever();
            modelBuilder.Entity<Counter>().Property(counter => counter.Id).ValueGeneratedNever();
        }
    }

    private sealed class FakeUser : ICurrentUser
    {
        public bool IsAuthenticated => Id.HasValue;

        public Guid? Id { get; set; }

        public string? UserName => null;

        public Guid? TenantId { get; set; }

        public bool IsSystemTenant { get; set; }

        public IReadOnlyList<Guid> AllowedTenantIds { get; set; } = [];
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        ServiceCollection services = new();
        services.AddLogging().AddSingleton<ICurrentUser>(_user).AddBaseEntityFramework();
        services.AddDbContext<RepositoryContext>((provider, options) => options.UseSqlite(_connection).UseBaseEntityConcepts(provider));
        services.AddEfCoreRepository<RepositoryContext, Item, Guid>().AddEfCoreRepository<RepositoryContext, Counter, int>();
        _provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<RepositoryContext>().Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public void Dispose() => _connection.Dispose();

    private static readonly SortFields<Item> s_sort = new SortFields<Item>().Add("name", item => item.Name).Add("price", item => item.Price);

    private static IRepository<Item, Guid> Items(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IRepository<Item, Guid>>();

    private async Task SeedAsync(int count)
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        for (int number = 1; number <= count; number++)
        {
            await Items(scope).AddAsync(new Item($"item-{number:00}", number * 10));
        }
    }

    [Fact]
    public async Task AnAddedEntity_IsFoundById_ByPredicate_AndKeepsTheConcepts()
    {
        Item item = new("lamp", 5);
        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            await Items(scope).AddAsync(item);
        }

        await using AsyncServiceScope read = _provider.CreateAsyncScope();
        Item? byId = await Items(read).FindAsync(item.Id);
        Item? byName = await Items(read).FindAsync(candidate => candidate.Name == "lamp");

        Assert.Equal(item.Id, byId?.Id);
        Assert.Equal(item.Id, byName?.Id);
        Assert.Equal((_user.Id, s_tenantA), (byId!.CreatedBy, byId.TenantId));
        Assert.Null(await Items(read).FindAsync(Guid.NewGuid()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Items(read).FindAsync((System.Linq.Expressions.Expression<Func<Item, bool>>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Items(read).AddAsync(null!));
    }

    [Fact]
    public async Task AnEntityWithAnIntKey_IsFoundToo()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        IRepository<Counter, int> counters = scope.ServiceProvider.GetRequiredService<IRepository<Counter, int>>();
        await counters.AddAsync(new Counter(7) { Value = 1 });

        Assert.Equal(1, (await counters.FindAsync(7))?.Value);
        Assert.Null(await counters.FindAsync(8));
        Assert.Equal(1, await counters.CountAsync());
    }

    [Fact]
    public async Task APageIsSortedAsAsked_AndSaysHowManyThereAre()
    {
        await SeedAsync(25);
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        PagedQueryResult<Item> page = await Items(scope).GetPagedListAsync(new PagedQueryOptions<Item> { Sort = s_sort.Parse("name desc", "name"), PageNumber = 2, MaxResultCount = 10 });
        PagedQueryResult<Item> filtered = await Items(scope).GetPagedListAsync(new PagedQueryOptions<Item> { Filter = item => item.Price > 200, Sort = s_sort.Parse("price", "price"), MaxResultCount = 3 });

        Assert.Equal(25, page.TotalCount);
        Assert.Equal(Enumerable.Range(6, 10).Reverse().Select(number => $"item-{number:00}"), page.Items.Select(item => item.Name));
        Assert.Equal(5, filtered.TotalCount);
        Assert.Equal(["item-21", "item-22", "item-23"], filtered.Items.Select(item => item.Name));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Items(scope).GetPagedListAsync(null!));
    }

    [Fact]
    public async Task APageWithoutASorting_IsOrderedByTheKey_AndHasATenPerPageDefault()
    {
        await SeedAsync(12);
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        PagedQueryResult<Item> first = await Items(scope).GetPagedListAsync(new PagedQueryOptions<Item>());
        PagedQueryResult<Item> second = await Items(scope).GetPagedListAsync(new PagedQueryOptions<Item> { PageNumber = 2 });

        Assert.Equal((10, 2, 12), (first.Items.Count, second.Items.Count, (int)first.TotalCount));
        Assert.Empty(first.Items.Select(item => item.Id).Intersect(second.Items.Select(item => item.Id)));
        Assert.Equal(first.Items.Select(item => item.Id).Order(), first.Items.Select(item => item.Id));
    }

    [Fact]
    public async Task AListHasASortingAFilterAndALimit()
    {
        await SeedAsync(8);
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();

        IReadOnlyList<Item> list = await Items(scope).GetListAsync(new ListQueryOptions<Item>
        {
            Filter = item => item.Price >= 30,
            Sort = s_sort.Parse("price desc, name", "name"),
            MaxResultCount = 3,
        });
        IReadOnlyList<Item> all = await Items(scope).GetListAsync(new ListQueryOptions<Item>());

        Assert.Equal(["item-08", "item-07", "item-06"], list.Select(item => item.Name));
        Assert.Equal(8, all.Count);
        await Assert.ThrowsAsync<ArgumentNullException>(() => Items(scope).GetListAsync(null!));
    }

    [Fact]
    public async Task CountAndAny_FollowTheFilters_UnlessTheyAreSwitchedOff()
    {
        await SeedAsync(3);
        Item deleted = new("gone", 1);
        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            await Items(scope).AddAsync(deleted);
            Item loaded = (await Items(scope).FindAsync(deleted.Id))!;
            await Items(scope).DeleteAsync(loaded);
        }

        await using AsyncServiceScope read = _provider.CreateAsyncScope();
        Assert.Equal(3, await Items(read).CountAsync());
        Assert.Equal(2, await Items(read).CountAsync(item => item.Price >= 20));
        Assert.False(await Items(read).AnyAsync(item => item.Name == "gone"));
        Assert.True(await Items(read).AnyAsync(item => item.Name == "gone", ignoreFilters: true));
        Assert.Null(await Items(read).FindAsync(deleted.Id));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Items(read).AnyAsync(null!));
    }

    [Fact]
    public async Task ADeletedEntity_IsMarkedNotRemoved_AndAnotherTenantsEntityIsNotSeen()
    {
        Item mine = new("mine", 1);
        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            await Items(scope).AddAsync(mine);
        }

        _user.TenantId = s_tenantB;
        await using (AsyncServiceScope other = _provider.CreateAsyncScope())
        {
            Assert.Null(await Items(other).FindAsync(mine.Id));
            Assert.Equal(0, await Items(other).CountAsync());
        }

        _user.TenantId = s_tenantA;
        await using AsyncServiceScope again = _provider.CreateAsyncScope();
        await Items(again).DeleteAsync((await Items(again).FindAsync(mine.Id))!);
        Assert.Null(await Items(again).FindAsync(mine.Id));
        Assert.True(await Items(again).AnyAsync(item => item.Id == mine.Id, ignoreFilters: true));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Items(again).DeleteAsync(null!));
    }

    [Fact]
    public async Task AnUpdate_SavesATrackedEntitysChanges_AndADetachedOnesToo()
    {
        await SeedAsync(2);
        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            Item tracked = (await Items(scope).FindAsync(item => item.Name == "item-01"))!;
            tracked.Price = 111;
            await Items(scope).UpdateAsync(tracked);
        }

        Item detached;
        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            detached = (await Items(scope).GetListAsync(new ListQueryOptions<Item> { Filter = item => item.Name == "item-02" })).Single();
        }

        detached.Price = 222;
        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            await Items(scope).UpdateAsync(detached);
        }

        await using AsyncServiceScope read = _provider.CreateAsyncScope();
        Assert.Equal(111, (await Items(read).FindAsync(item => item.Name == "item-01"))!.Price);
        Assert.Equal(222, (await Items(read).FindAsync(item => item.Name == "item-02"))!.Price);
        Assert.NotNull((await Items(read).FindAsync(item => item.Name == "item-01"))!.UpdatedBy);
        await Assert.ThrowsAsync<ArgumentNullException>(() => Items(read).UpdateAsync(null!));
    }

    [Fact]
    public void TheRegistration_RejectsNull() => Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddEfCoreRepository<RepositoryContext, Item, Guid>());

    private static IEfCoreRepository<Item, Guid> EfItems(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IEfCoreRepository<Item, Guid>>();

    private async Task<Item[]> AllAsync(bool ignoreFilters)
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        RepositoryContext db = scope.ServiceProvider.GetRequiredService<RepositoryContext>();
        return await (ignoreFilters ? db.Items.IgnoreQueryFilters() : db.Items).AsNoTracking().OrderBy(item => item.Name).ToArrayAsync();
    }

    [Fact]
    public async Task ManyEntitiesAreAdded_UpdatedAndDeleted_InOneGo_WithTheConcepts()
    {
        Item[] items = [new("a", 1), new("b", 2), new("c", 3)];
        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            await Items(scope).AddManyAsync(items);
            await Items(scope).AddManyAsync([]);
        }

        Item[] added = await AllAsync(false);
        Assert.Equal(["a", "b", "c"], added.Select(item => item.Name));
        Assert.All(added, item => Assert.Equal((_user.Id, s_tenantA), (item.CreatedBy, item.TenantId)));

        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            Item tracked = (await Items(scope).FindAsync(item => item.Name == "a"))!;
            tracked.Price = 10;
            Item detached = added.Single(item => item.Name == "b");
            detached.Price = 20;
            await Items(scope).UpdateManyAsync([tracked, detached]);
        }

        Assert.Equal([10, 20, 3], (await AllAsync(false)).Select(item => item.Price));

        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            await Items(scope).DeleteManyAsync(await Items(scope).GetListAsync(new ListQueryOptions<Item> { Filter = item => item.Price < 15 }));
        }

        Assert.Equal(["b"], (await AllAsync(false)).Select(item => item.Name));
        Assert.Equal(3, (await AllAsync(true)).Length);
        await using AsyncServiceScope check = _provider.CreateAsyncScope();
        await Assert.ThrowsAsync<ArgumentNullException>(() => Items(check).AddManyAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Items(check).UpdateManyAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => Items(check).DeleteManyAsync(null!));
    }

    [Fact]
    public async Task DeleteWhere_MarksWhatMatches_WithoutLoadingIt_AndLeavesTheOthersAlone()
    {
        await SeedAsync(5);
        _user.TenantId = s_tenantB;
        await using (AsyncServiceScope other = _provider.CreateAsyncScope())
        {
            await Items(other).AddAsync(new Item("item-01", 1));
        }

        _user.TenantId = s_tenantA;
        long deleted;
        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            deleted = await Items(scope).DeleteWhereAsync(item => item.Price <= 30);
            Assert.Equal(0, await Items(scope).DeleteWhereAsync(item => item.Price <= 30));
        }

        Assert.Equal(3, deleted);
        Assert.Equal(["item-04", "item-05"], (await AllAsync(false)).Select(item => item.Name));
        Item[] everything = await AllAsync(true);
        Item[] marked = [.. everything.Where(item => item.IsDeleted)];
        Assert.Equal(3, marked.Length);
        Assert.All(marked, item => Assert.Equal((_user.Id, true, s_tenantA), (item.DeletedBy, item.DeletedAt.HasValue, item.TenantId)));
        Assert.All(marked, item => Assert.Equal(_user.Id, item.UpdatedBy));
        Assert.Contains(everything, item => item.TenantId == s_tenantB && !item.IsDeleted);
        await using AsyncServiceScope check = _provider.CreateAsyncScope();
        await Assert.ThrowsAsync<ArgumentNullException>(() => Items(check).DeleteWhereAsync(null!));
    }

    [Fact]
    public async Task DeleteWhere_RemovesWhatCannotBeSoftDeleted()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        IRepository<Counter, int> counters = scope.ServiceProvider.GetRequiredService<IRepository<Counter, int>>();
        await counters.AddManyAsync([new Counter(1) { Value = 1 }, new Counter(2) { Value = 2 }, new Counter(3) { Value = 3 }]);

        long deleted = await counters.DeleteWhereAsync(counter => counter.Value >= 2);

        Assert.Equal(2, deleted);
        Assert.Equal(1, await counters.CountAsync());
    }

    [Fact]
    public async Task UpdateWhere_ChangesTheNamedProperties_AndWritesTheAuditFields()
    {
        await SeedAsync(4);
        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            int changed = await EfItems(scope).UpdateWhereAsync(item => item.Price >= 30, set => set.SetProperty(item => item.Name, "changed"));

            Assert.Equal(2, changed);
        }

        Item[] items = await AllAsync(false);
        Assert.Equal(["changed", "changed", "item-01", "item-02"], items.Select(item => item.Name));
        Assert.All(items.Where(item => item.Name == "changed"), item => Assert.Equal(_user.Id, item.UpdatedBy));
        await using AsyncServiceScope check = _provider.CreateAsyncScope();
        await Assert.ThrowsAsync<ArgumentNullException>(() => EfItems(check).UpdateWhereAsync(null!, set => { }));
        await Assert.ThrowsAsync<ArgumentNullException>(() => EfItems(check).UpdateWhereAsync(item => true, null!));
    }

    [Fact]
    public async Task UpdateWhere_OnAnEntityWithoutAuditFields_ChangesOnlyWhatIsNamed()
    {
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        IEfCoreRepository<Counter, int> counters = scope.ServiceProvider.GetRequiredService<IEfCoreRepository<Counter, int>>();
        await counters.AddAsync(new Counter(9) { Value = 1 });

        await counters.UpdateWhereAsync(counter => counter.Id == 9, set => set.SetProperty(counter => counter.Value, 5));

        Assert.Equal(5, (await counters.GetListAsync(new ListQueryOptions<Counter>())).Single().Value);
    }
}
