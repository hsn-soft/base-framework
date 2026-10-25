using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.EntityFrameworkCore.Tests;

public sealed class BaseDbContextTests : IAsyncLifetime, IDisposable
{
    private static readonly Guid s_userA = Guid.NewGuid();
    private static readonly Guid s_tenantA = Guid.NewGuid();
    private static readonly Guid s_tenantB = Guid.NewGuid();

    private readonly SqliteConnection _connection = new("Data Source=:memory:");
    private readonly FakeUser _user = new() { Id = s_userA, TenantId = s_tenantA };
    private ServiceProvider _provider = null!;

    public sealed class Line
    {
        public string Product { get; set; } = string.Empty;
    }

    public sealed class Note : ICreationAudited, IModificationAudited, ISoftDelete, IMultiTenant
    {
        private readonly List<Line> _lines = [];

        public int Id { get; set; }

        public string Text { get; set; } = string.Empty;

        public Guid TenantId { get; private set; }

        public DateTimeOffset CreatedAt { get; private set; }

        public Guid? CreatedBy { get; private set; }

        public DateTimeOffset UpdatedAt { get; private set; }

        public Guid? UpdatedBy { get; private set; }

        public bool IsDeleted { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public Guid? DeletedBy { get; private set; }

        public IReadOnlyList<Line> Lines => _lines;

        public void AddLine(string product) => _lines.Add(new Line { Product = product });

        public void AssignTenant(Guid tenantId) => TenantId = tenantId;

        public void MarkCreated(DateTimeOffset at, Guid? by) => (CreatedAt, CreatedBy) = (at, by);

        public void MarkModified(DateTimeOffset at, Guid? by) => (UpdatedAt, UpdatedBy) = (at, by);

        public void MarkDeleted(DateTimeOffset at, Guid? by) => (IsDeleted, DeletedAt, DeletedBy) = (true, at, by);
    }

    /// <summary>Only soft delete: no tenant.</summary>
    public sealed class Tag : ISoftDelete
    {
        public int Id { get; set; }

        public bool IsDeleted { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public Guid? DeletedBy { get; private set; }

        public void MarkDeleted(DateTimeOffset at, Guid? by) => (IsDeleted, DeletedAt, DeletedBy) = (true, at, by);
    }

    /// <summary>Only tenant: no soft delete.</summary>
    public sealed class Doc : IMultiTenant
    {
        public int Id { get; set; }

        public Guid TenantId { get; private set; }

        public void AssignTenant(Guid tenantId) => TenantId = tenantId;
    }

    public sealed class Plain
    {
        public int Id { get; set; }
    }

    public sealed class NotesContext(DbContextOptions<NotesContext> options, IDataFilter? dataFilter = null, ICurrentTenant? currentTenant = null)
        : BaseDbContext(options, dataFilter, currentTenant)
    {
        public DbSet<Note> Notes => Set<Note>();

        public DbSet<Tag> Tags => Set<Tag>();

        public DbSet<Doc> Docs => Set<Doc>();

        public DbSet<Plain> Plains => Set<Plain>();

        protected override void ConfigureModel(ModelBuilder modelBuilder)
            => modelBuilder.Entity<Note>().OwnsMany(note => note.Lines, line =>
            {
                line.WithOwner().HasForeignKey("NoteId");
                line.Property<int>("Id");
                line.HasKey("Id");
            }).Navigation(note => note.Lines).UsePropertyAccessMode(PropertyAccessMode.Field);
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
        services.AddDbContext<NotesContext>((provider, options) => options.UseSqlite(_connection).UseBaseEntityConcepts(provider));
        _provider = services.BuildServiceProvider();
        await using AsyncServiceScope scope = _provider.CreateAsyncScope();
        await scope.ServiceProvider.GetRequiredService<NotesContext>().Database.EnsureCreatedAsync();
    }

    public async Task DisposeAsync()
    {
        await _provider.DisposeAsync();
        await _connection.DisposeAsync();
    }

    public void Dispose() => _connection.Dispose();

    private AsyncServiceScope Scope() => _provider.CreateAsyncScope();

    private static NotesContext Db(AsyncServiceScope scope) => scope.ServiceProvider.GetRequiredService<NotesContext>();

    [Fact]
    public async Task AnAddedEntity_GetsTheAuditFields_AndTheTenantOfTheUser()
    {
        await using (AsyncServiceScope scope = Scope())
        {
            NotesContext db = Db(scope);
            db.Notes.Add(new Note { Text = "first" });
            await db.SaveChangesAsync();
        }

        await using AsyncServiceScope read = Scope();
        Note note = await Db(read).Notes.SingleAsync();
        Assert.Equal((s_userA, s_userA, s_tenantA), (note.CreatedBy, note.UpdatedBy, note.TenantId));
        Assert.NotEqual(default, note.CreatedAt);
    }

    [Fact]
    public async Task AChangedEntity_GetsTheModificationFields_WithTheUserWhoChangedIt()
    {
        await using (AsyncServiceScope scope = Scope())
        {
            Db(scope).Notes.Add(new Note { Text = "first" });
            await Db(scope).SaveChangesAsync();
        }

        Guid editor = Guid.NewGuid();
        _user.Id = editor;
        await using (AsyncServiceScope scope = Scope())
        {
            Note note = await Db(scope).Notes.SingleAsync();
            note.Text = "second";
            await Db(scope).SaveChangesAsync();
        }

        await using AsyncServiceScope read = Scope();
        Note changed = await Db(read).Notes.SingleAsync();
        Assert.Equal((s_userA, editor), (changed.CreatedBy, changed.UpdatedBy));
    }

    [Fact]
    public async Task ADeletedEntity_IsMarkedNotRemoved_WhatItOwnsStays_AndTheListsDoNotShowIt()
    {
        await using (AsyncServiceScope scope = Scope())
        {
            Note note = new() { Text = "to delete" };
            note.AddLine("pen");
            Db(scope).Notes.Add(note);
            await Db(scope).SaveChangesAsync();
        }

        await using (AsyncServiceScope scope = Scope())
        {
            Note note = await Db(scope).Notes.Include(entity => entity.Lines).SingleAsync();
            Db(scope).Notes.Remove(note);
            await Db(scope).SaveChangesAsync();
        }

        await using AsyncServiceScope read = Scope();
        Assert.Empty(await Db(read).Notes.ToListAsync());
        IDataFilter filter = read.ServiceProvider.GetRequiredService<IDataFilter>();
        using (filter.Disable<ISoftDelete>())
        {
            Note deleted = await Db(read).Notes.Include(entity => entity.Lines).SingleAsync();
            Assert.True(deleted.IsDeleted);
            Assert.Equal(s_userA, deleted.DeletedBy);
            Assert.NotNull(deleted.DeletedAt);
            Assert.Equal("pen", Assert.Single(deleted.Lines).Product);
        }
    }

    [Fact]
    public async Task AnOnlySoftDeleteEntity_IsFilteredOnItsOwn_AndWithoutAUserItsFieldsAreEmpty()
    {
        _user.Id = null;
        await using (AsyncServiceScope scope = Scope())
        {
            Db(scope).Tags.AddRange(new Tag(), new Tag());
            await Db(scope).SaveChangesAsync();
            Db(scope).Tags.Remove(await Db(scope).Tags.OrderBy(tag => tag.Id).FirstAsync());
            await Db(scope).SaveChangesAsync();
        }

        await using AsyncServiceScope read = Scope();
        Assert.Single(await Db(read).Tags.ToListAsync());
        using (read.ServiceProvider.GetRequiredService<IDataFilter>().Disable<ISoftDelete>())
        {
            Assert.Null((await Db(read).Tags.OrderBy(tag => tag.Id).FirstAsync()).DeletedBy);
        }
    }

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

        _user.AllowedTenantIds = [s_tenantB];
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

        _user.TenantId = s_tenantB;
        await using AsyncServiceScope read = Scope();
        Assert.Empty(await Db(read).Docs.ToListAsync());

        _user.IsSystemTenant = true;
        Assert.Single(await Db(read).Docs.ToListAsync());

        _user.IsSystemTenant = false;
        using (read.ServiceProvider.GetRequiredService<IDataFilter>().Disable<IMultiTenant>())
        {
            Assert.Single(await Db(read).Docs.ToListAsync());
        }
    }

    [Fact]
    public async Task WithoutATenant_NothingOfATenantIsSeen_AndTheTenantOfAnAddedEntityStaysEmpty()
    {
        _user.TenantId = null;
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
            Doc doc = new();
            doc.AssignTenant(s_tenantB);
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
    public async Task AnEntityWithNoConcepts_IsSavedAndDeletedAsAnyOther()
    {
        await using AsyncServiceScope scope = Scope();
        Plain plain = new();
        Db(scope).Plains.Add(plain);
        await Db(scope).SaveChangesAsync();

        Db(scope).Plains.Remove(plain);
        await Db(scope).SaveChangesAsync();

        Assert.Empty(await Db(scope).Plains.ToListAsync());
    }

    [Fact]
    public async Task WithoutTheConcepts_TheContextStillWorks_AndFiltersAreOnWithNoTenantSeen()
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddDbContext<NotesContext>(options => options.UseSqlite(_connection));
        await using ServiceProvider bare = services.BuildServiceProvider();
        await using AsyncServiceScope scope = bare.CreateAsyncScope();
        NotesContext db = scope.ServiceProvider.GetRequiredService<NotesContext>();

        Assert.Empty(await db.Docs.ToListAsync());
        Assert.Empty(await db.Tags.ToListAsync());
    }

    [Fact]
    public void TheRegistrationAndTheContext_RejectNull()
    {
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseEntityFramework());
        Assert.Throws<ArgumentNullException>(() => ((DbContextOptionsBuilder)null!).UseBaseEntityConcepts(_provider));
        Assert.Throws<ArgumentNullException>(() => new DbContextOptionsBuilder().UseBaseEntityConcepts(null!));
        BaseEntityInterceptor interceptor = _provider.GetRequiredService<BaseEntityInterceptor>();
        Assert.Throws<ArgumentNullException>(() => interceptor.SavingChanges(null!, default));
        Assert.Throws<ArgumentNullException>(() => interceptor.SavingChangesAsync(null!, default).AsTask().GetAwaiter().GetResult());
    }
}
