using HsnSoft.Base.Auditing;
using HsnSoft.Base.Data;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Core.Tests;

public class EntityConceptsTests
{
    private sealed class FakeUser : ICurrentUser
    {
        public bool IsAuthenticated => Id.HasValue;

        public Guid? Id { get; init; }

        public string? UserName => null;

        public Guid? TenantId { get; init; }

        public bool IsSystemTenant { get; init; }

        public IReadOnlyList<Guid> AllowedTenantIds { get; init; } = [];
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class Row : ICreationAudited, IModificationAudited, ISoftDelete
    {
        public DateTimeOffset CreatedAt { get; private set; }

        public Guid? CreatedBy { get; private set; }

        public DateTimeOffset UpdatedAt { get; private set; }

        public Guid? UpdatedBy { get; private set; }

        public bool IsDeleted { get; private set; }

        public DateTimeOffset? DeletedAt { get; private set; }

        public Guid? DeletedBy { get; private set; }

        public void MarkCreated(DateTimeOffset at, Guid? by) => (CreatedAt, CreatedBy) = (at, by);

        public void MarkModified(DateTimeOffset at, Guid? by) => (UpdatedAt, UpdatedBy) = (at, by);

        public void MarkDeleted(DateTimeOffset at, Guid? by) => (IsDeleted, DeletedAt, DeletedBy) = (true, at, by);
    }

    private static readonly DateTimeOffset s_t1 = new(2026, 10, 25, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset s_t2 = s_t1.AddHours(1);

    [Fact]
    public void Creation_SetsTheCreationAndTheFirstModification_WithTheCurrentUser()
    {
        Guid user = Guid.NewGuid();
        Row row = new();

        new AuditPropertySetter(new FixedClock(s_t1), new FakeUser { Id = user }).SetCreation(row);

        Assert.Equal((s_t1, user, s_t1, user), (row.CreatedAt, row.CreatedBy, row.UpdatedAt, row.UpdatedBy));
    }

    [Fact]
    public void Modification_ChangesOnlyTheModificationFields()
    {
        Guid creator = Guid.NewGuid();
        Guid editor = Guid.NewGuid();
        Row row = new();
        new AuditPropertySetter(new FixedClock(s_t1), new FakeUser { Id = creator }).SetCreation(row);

        new AuditPropertySetter(new FixedClock(s_t2), new FakeUser { Id = editor }).SetModification(row);

        Assert.Equal((s_t1, creator, s_t2, editor), (row.CreatedAt, row.CreatedBy, row.UpdatedAt, row.UpdatedBy));
    }

    [Fact]
    public void Deletion_MarksTheEntityOnce_AndIsAChange()
    {
        Guid first = Guid.NewGuid();
        Row row = new();

        new AuditPropertySetter(new FixedClock(s_t1), new FakeUser { Id = first }).SetDeletion(row);
        new AuditPropertySetter(new FixedClock(s_t2), new FakeUser { Id = Guid.NewGuid() }).SetDeletion(row);

        Assert.True(row.IsDeleted);
        Assert.Equal((s_t1, first), (row.DeletedAt, row.DeletedBy));
    }

    [Fact]
    public void WithoutAUser_TheUserFieldsAreEmpty_AndWhatIsNotAuditedIsLeftAlone()
    {
        Row row = new();
        AuditPropertySetter setter = new(new FixedClock(s_t1));

        setter.SetCreation(row);
        setter.SetCreation(new object());
        setter.SetModification(new object());
        setter.SetDeletion(new object());

        Assert.Null(row.CreatedBy);
        Assert.Equal(s_t1, row.CreatedAt);
    }

    [Fact]
    public void TheSetter_RejectsNothing() => Assert.Throws<ArgumentNullException>(() => new AuditPropertySetter(TimeProvider.System).SetCreation(null!));

    [Fact]
    public void TheSetter_RejectsNullForModificationAndDeletionToo()
    {
        AuditPropertySetter setter = new(TimeProvider.System);

        Assert.Throws<ArgumentNullException>(() => setter.SetModification(null!));
        Assert.Throws<ArgumentNullException>(() => setter.SetDeletion(null!));
    }

    [Fact]
    public void FiltersAreOn_UntilAScopeTurnsThemOff_AndTheScopeRestoresThem()
    {
        DataFilter filter = new();
        Assert.True(filter.IsEnabled<ISoftDelete>());

        using (filter.Disable<ISoftDelete>())
        {
            Assert.False(filter.IsEnabled<ISoftDelete>());
            Assert.True(filter.IsEnabled<IMultiTenant>());

            using (filter.Enable<ISoftDelete>())
            {
                Assert.True(filter.IsEnabled<ISoftDelete>());
            }

            Assert.False(filter.IsEnabled<ISoftDelete>());
        }

        Assert.True(filter.IsEnabled<ISoftDelete>());
    }

    [Fact]
    public async Task AFilterSwitch_FollowsTheFlowOfTheCode_NotAnotherFlow()
    {
        DataFilter filter = new();
        using IDisposable scope = filter.Disable<ISoftDelete>();

        bool inside = await Task.Run(async () =>
        {
            await Task.Yield();
            return filter.IsEnabled<ISoftDelete>();
        });
        bool otherFlow = await Task.Run(() => { using IDisposable own = filter.Enable<ISoftDelete>(); return filter.IsEnabled<ISoftDelete>(); });

        Assert.False(inside);
        Assert.True(otherFlow);
        Assert.False(filter.IsEnabled<ISoftDelete>());
    }

    [Fact]
    public void TheTenant_IsTheUsersTenant_WithTheOwnTenantAlwaysAllowed()
    {
        Guid own = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        CurrentTenant tenant = new(new FakeUser { TenantId = own, AllowedTenantIds = [other, own] });

        Assert.True(tenant.IsAvailable);
        Assert.Equal(own, tenant.Id);
        Assert.False(tenant.IsSystemTenant);
        Assert.Equal([other, own], tenant.AllowedTenantIds);
    }

    [Fact]
    public void WithoutAUser_ThereIsNoTenant_AndNothingIsAllowed()
    {
        CurrentTenant tenant = new();

        Assert.False(tenant.IsAvailable);
        Assert.Null(tenant.Id);
        Assert.False(tenant.IsSystemTenant);
        Assert.Empty(tenant.AllowedTenantIds);
    }

    [Fact]
    public void ChangeActsAsAnotherTenant_UntilTheScopeEnds()
    {
        Guid own = Guid.NewGuid();
        Guid acting = Guid.NewGuid();
        Guid extra = Guid.NewGuid();
        CurrentTenant tenant = new(new FakeUser { TenantId = own });

        using (tenant.Change(acting, isSystemTenant: true, allowedTenantIds: [extra]))
        {
            Assert.Equal(acting, tenant.Id);
            Assert.True(tenant.IsSystemTenant);
            Assert.Equal([extra, acting], tenant.AllowedTenantIds);

            using (tenant.Change(null))
            {
                Assert.False(tenant.IsAvailable);
                Assert.Empty(tenant.AllowedTenantIds);
            }

            Assert.Equal(acting, tenant.Id);
        }

        Assert.Equal(own, tenant.Id);
        Assert.False(tenant.IsSystemTenant);
    }

    [Fact]
    public void TheRegistration_AddsTheConcepts_AndKeepsWhatTheHostRegistered()
    {
        Guid user = Guid.NewGuid();
        ServiceCollection services = new();
        services.AddSingleton<ICurrentUser>(new FakeUser { Id = user, TenantId = Guid.NewGuid() });
        services.AddSingleton<TimeProvider>(new FixedClock(s_t1));

        using ServiceProvider provider = services.AddBaseEntityConcepts().BuildServiceProvider();

        Row row = new();
        provider.GetRequiredService<IAuditPropertySetter>().SetCreation(row);
        Assert.Equal((s_t1, user), (row.CreatedAt, row.CreatedBy));
        Assert.True(provider.GetRequiredService<IDataFilter>().IsEnabled<IMultiTenant>());
        Assert.True(provider.GetRequiredService<ICurrentTenant>().IsAvailable);
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseEntityConcepts());
    }

    [Fact]
    public void TheRegistration_WorksWithoutACurrentUser()
    {
        using ServiceProvider provider = new ServiceCollection().AddBaseEntityConcepts().BuildServiceProvider();

        Row row = new();
        provider.GetRequiredService<IAuditPropertySetter>().SetCreation(row);

        Assert.Null(row.CreatedBy);
        Assert.False(provider.GetRequiredService<ICurrentTenant>().IsAvailable);
    }
}
