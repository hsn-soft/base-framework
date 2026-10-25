using System.Linq.Expressions;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Domain.Sorting;
using HsnSoft.Base.MongoDB;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Testcontainers.MongoDb;

namespace HsnSoft.Base.IntegrationTests.Data;

public sealed class MongoRepositoryTests : IAsyncLifetime
{
    private static readonly Guid s_tenantA = Guid.NewGuid();
    private static readonly Guid s_tenantB = Guid.NewGuid();

    private readonly MongoDbContainer _container = new MongoDbBuilder("mongo:7.0").Build();
    private readonly FakeUser _user = new() { Id = Guid.NewGuid(), TenantId = s_tenantA };
    private IHost _host = null!;

    public sealed class Product(string name, int price) : FullAuditedEntity<Guid>(Guid.NewGuid()), IMultiTenant
    {
        public string Name { get; set; } = name;

        public int Price { get; set; } = price;

        public Guid TenantId { get; private set; }
    }

    // not soft deleted, no tenant: a delete removes it
    public sealed class Tag(Guid id, string label) : Entity<Guid>(id)
    {
        public string Label { get; set; } = label;
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

    private static readonly SortFields<Product> s_sort = new SortFields<Product>().Add("name", product => product.Name).Add("price", product => product.Price);

    public async Task InitializeAsync()
    {
        await _container.StartAsync();
        RegisterClassMaps();
        string url = new MongoUrlBuilder(_container.GetConnectionString()) { DatabaseName = "repo", AuthenticationSource = "admin" }.ToString();
        IConfiguration configuration = Support.Configuration(("ConnectionStrings:Mongo", url));
        _host = await Support.StartHostAsync(services => services
            .AddSingleton<ICurrentUser>(_user)
            .AddBaseMongo(configuration)
            .AddMongoRepository<Product, Guid>("products")
            .AddMongoRepository<Tag, Guid>("tags"));
    }

    public async Task DisposeAsync()
    {
        _host.Dispose();
        await _container.DisposeAsync();
    }

    private static void RegisterClassMaps()
    {
        // the maps of the base classes (the id, the audit fields) first; an entity's map only has what is its own
        MongoEntityMaps.RegisterBases<Guid>();
        if (!BsonClassMap.IsClassMapRegistered(typeof(Product)))
        {
            BsonClassMap.RegisterClassMap<Product>(map =>
            {
                map.MapProperty(product => product.Name);
                map.MapProperty(product => product.Price);
                map.MapProperty(product => product.TenantId);
                map.MapCreator(product => new Product(product.Name, product.Price));
            });
        }

        if (!BsonClassMap.IsClassMapRegistered(typeof(Tag)))
        {
            BsonClassMap.RegisterClassMap<Tag>(map =>
            {
                map.MapProperty(tag => tag.Label);
                map.MapCreator(tag => new Tag(tag.Id, tag.Label));
            });
        }
    }

    private AsyncServiceScope Scope() => _host.Services.CreateAsyncScope();

    private static IRepository<Product, Guid> Products(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<IRepository<Product, Guid>>();

    private async Task SeedAsync(int count)
    {
        await using AsyncServiceScope scope = Scope();
        for (int number = 1; number <= count; number++)
        {
            await Products(scope).AddAsync(new Product($"item-{number:00}", number * 10));
        }
    }

    [Fact]
    public async Task TheRepository_DoesWhatTheOtherStoresDo_OnARealMongoDb()
    {
        // find, add, the concepts written before the write
        Product lamp = new("lamp", 5);
        await using (AsyncServiceScope scope = Scope())
        {
            await Products(scope).AddAsync(lamp);
            Product? byId = await Products(scope).FindAsync(lamp.Id);
            Product? byName = await Products(scope).FindAsync(candidate => candidate.Name == "lamp");

            Assert.Equal(lamp.Id, byId?.Id);
            Assert.Equal(lamp.Id, byName?.Id);
            Assert.Equal((_user.Id, s_tenantA), (byId!.CreatedBy, byId.TenantId));
            Assert.Null(await Products(scope).FindAsync(Guid.NewGuid()));
        }

        // a page, a list, a count
        await SeedAsync(25);
        await using (AsyncServiceScope scope = Scope())
        {
            PagedQueryResult<Product> page = await Products(scope).GetPagedListAsync(new PagedQueryOptions<Product> { Sort = s_sort.Parse("name desc", "name"), PageNumber = 2, MaxResultCount = 10, Filter = product => product.Name.StartsWith("item") });
            PagedQueryResult<Product> byKey = await Products(scope).GetPagedListAsync(new PagedQueryOptions<Product>());
            IReadOnlyList<Product> list = await Products(scope).GetListAsync(new ListQueryOptions<Product> { Filter = product => product.Price >= 230, Sort = s_sort.Parse("price desc, name", "name"), MaxResultCount = 2 });
            IReadOnlyList<Product> all = await Products(scope).GetListAsync(new ListQueryOptions<Product>());

            Assert.Equal(25, page.TotalCount);
            Assert.Equal(Enumerable.Range(6, 10).Reverse().Select(number => $"item-{number:00}"), page.Items.Select(product => product.Name));
            Assert.Equal((10, 26), (byKey.Items.Count, (int)byKey.TotalCount));
            Assert.Equal(byKey.Items.Select(product => product.Id).Order(), byKey.Items.Select(product => product.Id));
            Assert.Equal(["item-25", "item-24"], list.Select(product => product.Name));
            Assert.Equal(26, all.Count);
            Assert.Equal(26, await Products(scope).CountAsync());
            Assert.Equal(2, await Products(scope).CountAsync(product => product.Price >= 240));
        }

        // an update replaces the document; the modification is marked
        await using (AsyncServiceScope scope = Scope())
        {
            Product tracked = (await Products(scope).FindAsync(candidate => candidate.Name == "item-01"))!;
            tracked.Price = 111;
            await Products(scope).UpdateAsync(tracked);
            Assert.Equal(111, (await Products(scope).FindAsync(tracked.Id))!.Price);
            Assert.NotNull((await Products(scope).FindAsync(tracked.Id))!.UpdatedBy);
        }

        // a delete marks it, an other tenant does not see it, ignoring the filters does
        await using (AsyncServiceScope scope = Scope())
        {
            await Products(scope).DeleteAsync((await Products(scope).FindAsync(lamp.Id))!);

            Assert.Null(await Products(scope).FindAsync(lamp.Id));
            Assert.False(await Products(scope).AnyAsync(product => product.Name == "lamp"));
            Assert.True(await Products(scope).AnyAsync(product => product.Name == "lamp", ignoreFilters: true));
            Assert.Equal(25, await Products(scope).CountAsync());
        }

        _user.TenantId = s_tenantB;
        await using (AsyncServiceScope scope = Scope())
        {
            Assert.Equal(0, await Products(scope).CountAsync());
            Assert.Empty(await Products(scope).GetListAsync(new ListQueryOptions<Product>()));
        }

        _user.TenantId = s_tenantA;
    }

    [Fact]
    public async Task AnEntityThatIsNotSoftDeleted_IsRemoved_AndTheArgumentsAreChecked()
    {
        await using AsyncServiceScope scope = Scope();
        IRepository<Tag, Guid> tags = scope.ServiceProvider.GetRequiredService<IRepository<Tag, Guid>>();
        Tag tag = new(Guid.NewGuid(), "blue");
        await tags.AddAsync(tag);

        Assert.Equal(1, await tags.CountAsync());
        await tags.DeleteAsync(tag);
        Assert.Equal(0, await tags.CountAsync());

        await Assert.ThrowsAsync<ArgumentNullException>(() => tags.AddAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => tags.UpdateAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => tags.DeleteAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => tags.FindAsync((Expression<Func<Tag, bool>>)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => tags.AnyAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => tags.GetListAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => tags.GetPagedListAsync(null!));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddMongoRepository<Tag, Guid>("tags"));
        Assert.Throws<ArgumentException>(() => new ServiceCollection().AddMongoRepository<Tag, Guid>(" "));
    }

    public sealed class ProductDto
    {
        public Guid Id { get; set; }

        public string Name { get; set; } = string.Empty;
    }

    [Fact]
    public async Task ADto_IsReadByTheDocumentStore_WithOnlyItsFields()
    {
        await SeedAsync(5);
        await using AsyncServiceScope scope = Scope();
        IRepository<Product, Guid> products = Products(scope);
        Product first = (await products.GetListAsync(new ListQueryOptions<Product> { Sort = s_sort.Parse("name", "name"), MaxResultCount = 1 })).Single();

        ProductDto? one = await products.FindAsync<ProductDto>(first.Id);
        IReadOnlyList<ProductDto> list = await products.GetListAsync<ProductDto>(new ListQueryOptions<Product> { Sort = s_sort.Parse("name desc", "name"), MaxResultCount = 2 });
        PagedQueryResult<ProductDto> page = await products.GetPagedListAsync<ProductDto>(new PagedQueryOptions<Product> { Sort = s_sort.Parse("name", "name"), PageNumber = 2, MaxResultCount = 2 });
        IReadOnlyList<ProductDto> given = await products.GetListAsync<ProductDto>(new ListQueryOptions<Product> { MaxResultCount = 1 }, product => new ProductDto { Name = "x" + product.Name });

        Assert.Equal(("item-01", first.Id), (one?.Name, one?.Id));
        Assert.Equal(["item-05", "item-04"], list.Select(product => product.Name));
        Assert.Equal(5, page.TotalCount);
        Assert.Equal(["item-03", "item-04"], page.Items.Select(product => product.Name));
        Assert.StartsWith("xitem-", given.Single().Name);
        Assert.Null(await products.FindAsync<ProductDto>(Guid.NewGuid()));
        await Assert.ThrowsAsync<ArgumentNullException>(() => products.GetListAsync<ProductDto>(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => products.GetPagedListAsync<ProductDto>(null!));
    }

    [Fact]
    public async Task ManyEntities_AreAddedUpdatedAndDeleted_InOneGo_OnARealMongoDb()
    {
        await using AsyncServiceScope scope = Scope();
        IRepository<Product, Guid> products = Products(scope);
        Product[] items = [new("a", 1), new("b", 2), new("c", 3)];

        await products.AddManyAsync(items);
        await products.AddManyAsync([]);
        IReadOnlyList<Product> added = await products.GetListAsync(new ListQueryOptions<Product> { Sort = s_sort.Parse("name", "name") });
        Assert.Equal(["a", "b", "c"], added.Select(product => product.Name));
        Assert.All(added, product => Assert.Equal((_user.Id, s_tenantA), (product.CreatedBy, product.TenantId)));

        foreach (Product product in added)
        {
            product.Price *= 10;
        }

        await products.UpdateManyAsync(added);
        await products.UpdateManyAsync([]);
        Assert.Equal([10, 20, 30], (await products.GetListAsync(new ListQueryOptions<Product> { Sort = s_sort.Parse("name", "name") })).Select(product => product.Price));

        await products.DeleteManyAsync(added.Where(product => product.Price < 25));
        await products.DeleteManyAsync([]);
        Assert.Equal(["c"], (await products.GetListAsync(new ListQueryOptions<Product>())).Select(product => product.Name));
        Assert.True(await products.AnyAsync(product => product.Name == "a", ignoreFilters: true));
        await Assert.ThrowsAsync<ArgumentNullException>(() => products.AddManyAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => products.UpdateManyAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => products.DeleteManyAsync(null!));
    }

    [Fact]
    public async Task DeleteWhere_MarksWhatMatches_WithoutLoadingIt_AndOnlyInTheTenantOfTheCaller()
    {
        await SeedAsync(5);
        _user.TenantId = s_tenantB;
        await using (AsyncServiceScope other = Scope())
        {
            await Products(other).AddAsync(new Product("item-01", 1));
        }

        _user.TenantId = s_tenantA;
        await using AsyncServiceScope scope = Scope();
        IRepository<Product, Guid> products = Products(scope);

        long deleted = await products.DeleteWhereAsync(product => product.Price <= 30);

        Assert.Equal(3, deleted);
        Assert.Equal(0, await products.DeleteWhereAsync(product => product.Price <= 30));
        Assert.Equal(["item-04", "item-05"], (await products.GetListAsync(new ListQueryOptions<Product> { Sort = s_sort.Parse("name", "name") })).Select(product => product.Name));
        IMongoCollection<Product> collection = scope.ServiceProvider.GetRequiredService<IMongoDatabase>().GetCollection<Product>("products");
        List<Product> marked = await collection.Find(product => product.IsDeleted).ToListAsync();
        Assert.Equal(3, marked.Count);
        Assert.All(marked, product => Assert.Equal((_user.Id, true, _user.Id), (product.DeletedBy, product.DeletedAt.HasValue, product.UpdatedBy)));
        Assert.Equal(1, await collection.CountDocumentsAsync(product => product.TenantId == s_tenantB && !product.IsDeleted));
        await Assert.ThrowsAsync<ArgumentNullException>(() => products.DeleteWhereAsync(null!));
    }

    [Fact]
    public async Task DeleteWhere_RemovesWhatCannotBeSoftDeleted()
    {
        await using AsyncServiceScope scope = Scope();
        IRepository<Tag, Guid> tags = scope.ServiceProvider.GetRequiredService<IRepository<Tag, Guid>>();
        await tags.AddManyAsync([new Tag(Guid.NewGuid(), "x"), new Tag(Guid.NewGuid(), "y"), new Tag(Guid.NewGuid(), "z")]);

        Assert.Equal(2, await tags.DeleteWhereAsync(tag => tag.Label != "x"));
        Assert.Equal(1, await tags.CountAsync());
    }

    [Fact]
    public async Task UpdateWhere_ChangesACounterOnlyWhileTheConditionHolds_WhateverTheNumberOfCallersAtOnce()
    {
        Product product = new("counter", 10);
        await using (AsyncServiceScope seed = Scope())
        {
            await Products(seed).AddAsync(product);
        }

        // twenty callers, each wants one of the ten that are there: the test and the change are one step, so exactly ten get one
        long[] changed = await Task.WhenAll(Enumerable.Range(0, 20).Select(async _ =>
        {
            await using AsyncServiceScope scope = Scope();
            return await scope.ServiceProvider.GetRequiredService<IMongoRepository<Product, Guid>>()
                .UpdateWhereAsync(candidate => candidate.Id == product.Id && candidate.Price >= 1, set => set.Inc(candidate => candidate.Price, -1));
        }));

        await using AsyncServiceScope read = Scope();
        Product? stored = await Products(read).FindAsync(product.Id);
        Assert.Equal(10, changed.Count(count => count == 1));
        Assert.Equal(10, changed.Count(count => count == 0));
        Assert.Equal(0, stored!.Price);
        Assert.Equal(_user.Id, stored.UpdatedBy);
        await Assert.ThrowsAsync<ArgumentNullException>(() => read.ServiceProvider.GetRequiredService<IMongoRepository<Product, Guid>>().UpdateWhereAsync(null!, set => set.Inc(candidate => candidate.Price, 1)));
        await Assert.ThrowsAsync<ArgumentNullException>(() => read.ServiceProvider.GetRequiredService<IMongoRepository<Product, Guid>>().UpdateWhereAsync(candidate => true, null!));
    }
}
