using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Security;

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
}
