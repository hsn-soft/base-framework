using HsnSoft.Base.Security;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Auditing.Tests;

public class AuditPropertySetterTests
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

    // an entity as the framework's base classes are: the read shape and the marks
    private sealed class Row : IFullAuditedObject, IMarkCreated, IMarkModified, IMarkDeleted
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

    // what only shows the audit fields, as a DTO does: it has nothing to mark and the setter leaves it alone
    private sealed class Shown : IFullAuditedObject
    {
        public DateTimeOffset CreatedAt { get; set; }

        public Guid? CreatedBy { get; set; }

        public DateTimeOffset UpdatedAt { get; set; }

        public Guid? UpdatedBy { get; set; }

        public bool IsDeleted { get; set; }

        public DateTimeOffset? DeletedAt { get; set; }

        public Guid? DeletedBy { get; set; }
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
    public void TheStamp_IsTheTimeAndTheUserOfAChangeMadeNow()
    {
        Guid user = Guid.NewGuid();

        AuditStamp stamp = new AuditPropertySetter(new FixedClock(s_t1), new FakeUser { Id = user }).Stamp();
        AuditStamp anonymous = new AuditPropertySetter(new FixedClock(s_t2)).Stamp();

        Assert.Equal(new AuditStamp(s_t1, user), stamp);
        Assert.Equal(new AuditStamp(s_t2, null), anonymous);
    }

    [Fact]
    public void WhatOnlyShowsTheAuditFields_IsLeftAlone_ByTheSetter()
    {
        Shown shown = new();
        AuditPropertySetter setter = new(new FixedClock(s_t1), new FakeUser { Id = Guid.NewGuid() });

        setter.SetCreation(shown);
        setter.SetModification(shown);
        setter.SetDeletion(shown);

        Assert.Equal(default, shown.CreatedAt);
        Assert.False(shown.IsDeleted);
    }

    [Fact]
    public void TheShapes_BuildOnEachOther_SoOneRuleCanTakeAnyOfThem()
    {
        IFullAuditedObject full = new Shown();

        Assert.IsType<IAuditedObject>(full, exactMatch: false);
        Assert.IsType<ICreationAuditedObject>(full, exactMatch: false);
        Assert.IsType<IModificationAuditedObject>(full, exactMatch: false);
        Assert.IsType<IDeletionAuditedObject>(full, exactMatch: false);
        Assert.IsType<IHasCreationTime>(full, exactMatch: false);
        Assert.IsType<IMayHaveCreator>(full, exactMatch: false);
        Assert.IsType<IHasModificationTime>(full, exactMatch: false);
        Assert.IsType<IHasDeletionTime>(full, exactMatch: false);
        Assert.IsType<ISoftDelete>(full, exactMatch: false);
    }

    [Fact]
    public void TheRegistration_AddsTheSetterAndTheClock_AndKeepsWhatTheHostRegistered()
    {
        Guid user = Guid.NewGuid();
        ServiceCollection services = new();
        services.AddSingleton<ICurrentUser>(new FakeUser { Id = user });
        services.AddSingleton<TimeProvider>(new FixedClock(s_t1));

        using ServiceProvider provider = services.AddBaseAuditing().BuildServiceProvider();

        Row row = new();
        provider.GetRequiredService<IAuditPropertySetter>().SetCreation(row);
        Assert.Equal((s_t1, user), (row.CreatedAt, row.CreatedBy));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseAuditing());
    }

    [Fact]
    public void TheRegistration_WorksWithoutACurrentUser_WithTheSystemClock()
    {
        using ServiceProvider provider = new ServiceCollection().AddBaseAuditing().BuildServiceProvider();

        Row row = new();
        provider.GetRequiredService<IAuditPropertySetter>().SetCreation(row);

        Assert.Null(row.CreatedBy);
        Assert.NotEqual(default, row.CreatedAt);
    }
}
