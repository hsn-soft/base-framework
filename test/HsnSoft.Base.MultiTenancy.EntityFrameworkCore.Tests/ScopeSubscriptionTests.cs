using HsnSoft.Base.Data;
using HsnSoft.Base.EntityFrameworkCore;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.MultiTenancy.EntityFrameworkCore.Tests;

public sealed class ScopeSubscriptionTests : IAsyncLifetime, IDisposable
{
    private static readonly Guid s_tenantA = Guid.NewGuid();
    private static readonly Guid s_tenantB = Guid.NewGuid();

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly FakeSource _source = new() { TenantId = s_tenantA, ScopeKeys = ["abc:video", "abc:text"] };
    private ServiceProvider _provider = null!;

    /// <summary>The content of a subscription: isolated by its scope key.</summary>
    public sealed class Content(string scopeKey = "") : IScopeSubscription
    {
        public int Id { get; set; }

        public string ScopeKey { get; private set; } = scopeKey;
    }

    /// <summary>A user of a tenant: isolated by its tenant, in the same context as the content.</summary>
    public sealed class Account(Guid tenantId = default) : IMultiTenant
    {
        public int Id { get; set; }

        public Guid TenantId { get; private set; } = tenantId;
    }

    /// <summary>Both: refused.</summary>
    public sealed class Both : IMultiTenant, IScopeSubscription
    {
        public int Id { get; set; }

        public Guid TenantId { get; set; }

        public string ScopeKey { get; set; } = string.Empty;
    }

    public sealed class MixedContext(DbContextOptions<MixedContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null) : MultiTenantDbContext(options, dataFilter, currentTenant)
    {
        public DbSet<Content> Contents => Set<Content>();

        public DbSet<Account> Accounts => Set<Account>();

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
        }
    }

    public sealed class RefusedContext(DbContextOptions<RefusedContext> options) : MultiTenantDbContext(options)
    {
        public DbSet<Both> Boths => Set<Both>();

        protected override void ConfigureModel(ModelBuilder modelBuilder)
        {
        }
    }

    private sealed class FakeSource : ITenantSource
    {
        public Guid? TenantId { get; set; }

        public bool IsSystemTenant { get; set; }

        public IReadOnlyList<string> ScopeKeys { get; set; } = [];

        public TenantContext? Current => new(TenantId, IsSystemTenant, [], ScopeKeys);
    }

    public async Task InitializeAsync()
    {
        await _connection.OpenAsync();
        ServiceCollection services = new();
        services.AddLogging().AddSingleton<ITenantSource>(_source).AddBaseEntityFramework().AddBaseMultiTenancyEntityFramework();
        services.AddDbContext<MixedContext>((provider, options) => options.UseSqlite(_connection).UseBaseEntityConcepts(provider));
        _provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<MixedContext>().Database.EnsureCreatedAsync();
        MixedContext db = scope.ServiceProvider.GetRequiredService<MixedContext>();
        using (scope.ServiceProvider.GetRequiredService<IDataFilter>().Disable<IMultiTenant>())
        {
            db.Contents.AddRange(new Content("abc:video"), new Content("abc:text"), new Content("xyz:video"), new Content("abc:audio"));
            db.Accounts.AddRange(new Account(s_tenantA), new Account(s_tenantB));
            await db.SaveChangesAsync();
        }
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public void Dispose() => _connection.Dispose();

    private static MixedContext Db(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<MixedContext>();

    [Fact]
    public async Task ACaller_SeesTheContentOfTheScopesOfItsToken_NotOthers()
    {
        await using AsyncServiceScope read = _provider.CreateAsyncScope();

        string[] seen = [.. (await Db(read).Contents.ToListAsync()).Select(content => content.ScopeKey).Order()];

        Assert.Equal(["abc:text", "abc:video"], seen);
    }

    [Fact]
    public async Task ACallerWithNoScopeKeys_SeesNoContent_ButTheContentOfAKeyAppearsWhenTheTokenListsIt()
    {
        _source.ScopeKeys = [];
        await using AsyncServiceScope read = _provider.CreateAsyncScope();
        Assert.Empty(await Db(read).Contents.ToListAsync());

        _source.ScopeKeys = ["abc:audio"];
        Assert.Equal("abc:audio", (await Db(read).Contents.SingleAsync()).ScopeKey);
    }

    [Fact]
    public async Task TheSystemTenant_SeesEveryScope_AndTheFilterCanBeSwitchedOff()
    {
        _source.ScopeKeys = [];
        await using AsyncServiceScope read = _provider.CreateAsyncScope();
        Assert.Empty(await Db(read).Contents.ToListAsync());

        _source.IsSystemTenant = true;
        Assert.Equal(4, await Db(read).Contents.CountAsync());

        _source.IsSystemTenant = false;
        using (read.ServiceProvider.GetRequiredService<IDataFilter>().Disable<IScopeSubscription>())
        {
            Assert.Equal(4, await Db(read).Contents.CountAsync());
        }
    }

    [Fact]
    public async Task TheTwoIsolations_AreIndependent_InOneContext()
    {
        await using AsyncServiceScope read = _provider.CreateAsyncScope();
        IDataFilter filter = read.ServiceProvider.GetRequiredService<IDataFilter>();

        // the tenant sees its own account and the content of its keys
        Assert.Equal(s_tenantA, (await Db(read).Accounts.SingleAsync()).TenantId);
        Assert.Equal(2, await Db(read).Contents.CountAsync());

        // switching the tenant filter off shows every account and changes nothing of the content, and the other way round
        using (filter.Disable<IMultiTenant>())
        {
            Assert.Equal(2, await Db(read).Accounts.CountAsync());
            Assert.Equal(2, await Db(read).Contents.CountAsync());
        }

        using (filter.Disable<IScopeSubscription>())
        {
            Assert.Equal(1, await Db(read).Accounts.CountAsync());
            Assert.Equal(4, await Db(read).Contents.CountAsync());
        }

        // a caller with keys and no tenant sees the content and none of the accounts
        _source.TenantId = null;
        Assert.Empty(await Db(read).Accounts.ToListAsync());
        Assert.Equal(2, await Db(read).Contents.CountAsync());
    }

    [Fact]
    public async Task TheScopeKeyOfANewRow_IsWhatTheProductSet_NotTheTenant()
    {
        await using (AsyncServiceScope scope = _provider.CreateAsyncScope())
        {
            Db(scope).Contents.Add(new Content("abc:video"));
            await Db(scope).SaveChangesAsync();
        }

        await using AsyncServiceScope read = _provider.CreateAsyncScope();
        Assert.Equal(2, await Db(read).Contents.CountAsync(content => content.ScopeKey == "abc:video"));
    }

    [Fact]
    public async Task AnEntityThatIsBothTenantAndScope_IsRefusedWhenTheModelIsBuilt()
    {
        await using SqliteConnection connection = new("Data Source=:memory:");
        await connection.OpenAsync();
        ServiceCollection services = new();
        services.AddLogging().AddBaseEntityFramework().AddBaseMultiTenancyEntityFramework();
        services.AddDbContext<RefusedContext>((provider, options) => options.UseSqlite(connection).UseBaseEntityConcepts(provider));
        await using ServiceProvider provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        Exception failure = await Assert.ThrowsAnyAsync<Exception>(() => scope.ServiceProvider.GetRequiredService<RefusedContext>().Boths.ToListAsync());

        Assert.Contains("Both is both IMultiTenant and IScopeSubscription", (failure as InvalidOperationException ?? (InvalidOperationException)failure.InnerException!).Message, StringComparison.Ordinal);
    }
}
