using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.MultiTenancy.EntityFrameworkCore.Tests;

public sealed class MultiTenantDbContextTests : IAsyncLifetime, IDisposable
{
    private static readonly Guid s_tenantA = Guid.NewGuid();
    private static readonly Guid s_tenantB = Guid.NewGuid();

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly FakeTenant _tenant = new() { TenantId = s_tenantA };
    private ServiceProvider _provider = null!;

    /// <summary>Only tenant: no soft delete.</summary>
    public sealed class Doc(Guid tenantId = default) : IMultiTenant
    {
        public int Id { get; set; }

        public Guid TenantId { get; private set; } = tenantId;
    }

    /// <summary>Tenant and soft delete: two filters on one entity.</summary>
    public sealed class Memo(Guid tenantId = default) : IMultiTenant, IDeletionAuditedObject, IMarkDeleted
    {
        public int Id { get; set; }

        public Guid TenantId { get; private set; } = tenantId;

        public bool IsDeleted { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public Guid? DeletedBy { get; private set; }

        public void MarkDeleted(DateTimeOffset at, Guid? by) => (IsDeleted, DeletedAt, DeletedBy) = (true, at, by);
    }

    /// <summary>Neither: no tenant filter.</summary>
    public sealed class Plain
    {
        public int Id { get; set; }
    }

    public sealed class TenantDbContext(DbContextOptions<TenantDbContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
        : MultiTenantDbContext(options, dataFilter, currentTenant)
    {
        public DbSet<Doc> Docs => Set<Doc>();

        public DbSet<Memo> Memos => Set<Memo>();

        public DbSet<Plain> Plains => Set<Plain>();

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
        }
    }

    private sealed class FakeTenant : ITenantSource
    {
        public Guid? TenantId { get; set; }

        public bool IsSystemTenant { get; set; }

        public IReadOnlyList<Guid> AllowedTenantIds { get; set; } = [];

        public TenantContext? Current => new(TenantId, IsSystemTenant, AllowedTenantIds);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        ServiceCollection services = new();
        services.AddLogging().AddSingleton<ITenantSource>(_tenant).AddBaseEntityFramework().AddBaseMultiTenancyEntityFramework();
        services.AddDbContext<TenantDbContext>((provider, options) => options.UseSqlite(_connection).UseBaseEntityConcepts(provider));
        _provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<TenantDbContext>().Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public void Dispose() => _connection.Dispose();

    private AsyncServiceScope Scope() => _provider.CreateAsyncScope();

    private static TenantDbContext Db(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<TenantDbContext>();

    [Fact]
    public async Task ATenantSeesItsOwnEntities_AndAnAllowedTenantsToo_NotOthers()
    {
        await using (AsyncServiceScope scope = Scope())
        {
            ICurrentTenant tenant = scope.ServiceProvider.GetRequiredService<ICurrentTenant>();
            using (tenant.Change(s_tenantA))
            {
                Db(scope).Docs.Add(new Doc());
                await Db(scope).SaveChangesAsync();
            }

            using (tenant.Change(s_tenantB))
            {
                Db(scope).Docs.Add(new Doc());
                await Db(scope).SaveChangesAsync();
            }
        }

        await using AsyncServiceScope read = Scope();
        Assert.Equal([s_tenantA], await Db(read).Docs.Select(doc => doc.TenantId).ToListAsync());

        _tenant.AllowedTenantIds = [s_tenantB];
        Assert.Equal(2, (await Db(read).Docs.ToListAsync()).Count);
    }

    [Fact]
    public async Task TheSystemTenant_SeesEveryTenant_AndTheFilterCanBeSwitchedOff()
    {
        await using (AsyncServiceScope scope = Scope())
        {
            Db(scope).Docs.Add(new Doc());
            await Db(scope).SaveChangesAsync();
        }

        _tenant.TenantId = s_tenantB;
        await using AsyncServiceScope read = Scope();
        Assert.Empty(await Db(read).Docs.ToListAsync());

        _tenant.IsSystemTenant = true;
        Assert.Single(await Db(read).Docs.ToListAsync());

        _tenant.IsSystemTenant = false;
        using (read.ServiceProvider.GetRequiredService<IDataFilter>().Disable<IMultiTenant>())
        {
            Assert.Single(await Db(read).Docs.ToListAsync());
        }
    }

    [Fact]
    public async Task WithoutATenant_NothingOfATenantIsSeen_AndTheTenantOfAnAddedEntityStaysEmpty()
    {
        _tenant.TenantId = null;
        await using (AsyncServiceScope scope = Scope())
        {
            Db(scope).Docs.Add(new Doc());
            await Db(scope).SaveChangesAsync();
        }

        await using AsyncServiceScope read = Scope();
        Assert.Empty(await Db(read).Docs.ToListAsync());
        using (read.ServiceProvider.GetRequiredService<IDataFilter>().Disable<IMultiTenant>())
        {
            Assert.Equal(Guid.Empty, (await Db(read).Docs.SingleAsync()).TenantId);
        }
    }

    [Fact]
    public async Task AnEntityWithTheTenantAlreadySet_KeepsIt()
    {
        await using (AsyncServiceScope scope = Scope())
        {
            Doc doc = new(s_tenantB);
            Db(scope).Docs.Add(doc);
            await Db(scope).SaveChangesAsync();
        }

        await using AsyncServiceScope read = Scope();
        using (read.ServiceProvider.GetRequiredService<IDataFilter>().Disable<IMultiTenant>())
        {
            Assert.Equal(s_tenantB, (await Db(read).Docs.SingleAsync()).TenantId);
        }
    }

    [Fact]
    public async Task TheTenantAndSoftDeleteFilters_BothApplyToOneEntity_AndEitherCanBeSwitchedOffAlone()
    {
        await using (AsyncServiceScope scope = Scope())
        {
            Db(scope).Memos.AddRange(new Memo(), new Memo(), new Memo(s_tenantB));
            await Db(scope).SaveChangesAsync();
            Db(scope).Memos.Remove(await Db(scope).Memos.OrderBy(memo => memo.Id).FirstAsync());
            await Db(scope).SaveChangesAsync();
        }

        await using AsyncServiceScope read = Scope();
        IDataFilter filter = read.ServiceProvider.GetRequiredService<IDataFilter>();
        Assert.Single(await Db(read).Memos.ToListAsync());
        using (filter.Disable<ISoftDelete>())
        {
            Assert.Equal(2, await Db(read).Memos.CountAsync());
        }

        using (filter.Disable<IMultiTenant>())
        {
            Assert.Equal(2, await Db(read).Memos.CountAsync());
        }
    }

    [Fact]
    public async Task AnEntityWithoutTheTenantConcept_IsNotFilteredByTenant()
    {
        await using AsyncServiceScope scope = Scope();
        Db(scope).Plains.Add(new Plain());
        await Db(scope).SaveChangesAsync();
        _tenant.TenantId = null;

        Assert.Single(await Db(scope).Plains.ToListAsync());
    }

    [Fact]
    public async Task AContextWithoutACurrentTenant_SeesNothingOfATenant()
    {
        await using (AsyncServiceScope scope = Scope())
        {
            Db(scope).Docs.Add(new Doc());
            await Db(scope).SaveChangesAsync();
        }

        ServiceCollection services = new();
        services.AddLogging();
        services.AddDbContext<TenantDbContext>(options => options.UseSqlite(_connection));
        await using ServiceProvider bare = services.BuildServiceProvider();
        await using AsyncServiceScope bareScope = bare.CreateAsyncScope();

        Assert.Empty(await bareScope.ServiceProvider.GetRequiredService<TenantDbContext>().Docs.ToListAsync());
    }

    [Fact]
    public void TheRegistration_AddsTheInterceptor_OnceAndRejectsNull()
    {
        ServiceCollection services = new();
        services.AddBaseMultiTenancyEntityFramework().AddBaseMultiTenancyEntityFramework();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(IInterceptor));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseMultiTenancyEntityFramework());
        TenantEntityInterceptor interceptor = new(new CurrentTenant());
        Assert.Throws<ArgumentNullException>(() => interceptor.SavingChanges(null!, default));
        Assert.Throws<ArgumentNullException>(() => interceptor.SavingChangesAsync(null!, default).AsTask().GetAwaiter().GetResult());
    }
}
