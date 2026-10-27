using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.MultiTenancy.Tests;

public class CurrentTenantTests
{
    private sealed class FakeSource(TenantContext? tenant) : ITenantSource
    {
        public TenantContext? Current => tenant;
    }

    private sealed class WithPrivateSetter : IMultiTenant
    {
        public Guid TenantId { get; private set; }
    }

    private sealed class WithoutSetter : IMultiTenant
    {
        public Guid TenantId => Guid.Empty;
    }

    [Fact]
    public void TheTenant_IsWhatTheSourceSays_WithTheOwnTenantAlwaysAllowed()
    {
        Guid own = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        CurrentTenant tenant = new([new FakeSource(new TenantContext(own, AllowedTenantIds: [other, own]))]);

        Assert.True(tenant.IsAvailable);
        Assert.Equal(own, tenant.Id);
        Assert.False(tenant.IsSystemTenant);
        Assert.Equal([other, own], tenant.AllowedTenantIds);
    }

    [Fact]
    public void TheFirstSourceThatKnowsATenantWins()
    {
        Guid second = Guid.NewGuid();
        CurrentTenant tenant = new([new FakeSource(null), new FakeSource(new TenantContext(second, IsSystemTenant: true)), new FakeSource(new TenantContext(Guid.NewGuid()))]);

        Assert.Equal(second, tenant.Id);
        Assert.True(tenant.IsSystemTenant);
    }

    [Fact]
    public void WithoutASource_ThereIsNoTenant_AndNothingIsAllowed()
    {
        CurrentTenant tenant = new();

        Assert.False(tenant.IsAvailable);
        Assert.Null(tenant.Id);
        Assert.False(tenant.IsSystemTenant);
        Assert.Empty(tenant.AllowedTenantIds);
    }

    [Fact]
    public void ASourceWithoutAnIdHasNoTenantButMayAllowOthers()
    {
        Guid other = Guid.NewGuid();
        CurrentTenant tenant = new([new FakeSource(new TenantContext(null, AllowedTenantIds: [other]))]);

        Assert.False(tenant.IsAvailable);
        Assert.Equal([other], tenant.AllowedTenantIds);
    }

    [Fact]
    public void ChangeActsAsAnotherTenant_UntilTheScopeEnds()
    {
        Guid own = Guid.NewGuid();
        Guid acting = Guid.NewGuid();
        Guid extra = Guid.NewGuid();
        CurrentTenant tenant = new([new FakeSource(new TenantContext(own))]);

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
    public async Task ChangeFollowsTheFlowOfTheCode_NotOtherFlows()
    {
        CurrentTenant tenant = new();
        Guid acting = Guid.NewGuid();

        using (tenant.Change(acting))
        {
            Assert.Equal(acting, await Task.Run(() => tenant.Id));
        }

        Assert.Null(await Task.Run(() => tenant.Id));
    }

    [Fact]
    public void AddBaseMultiTenancy_RegistersTheTenant_AndKeepsWhatTheHostRegistered()
    {
        ServiceCollection services = new();
        services.AddSingleton<ITenantSource>(new FakeSource(new TenantContext(Guid.NewGuid())));
        services.AddBaseMultiTenancy();
        services.AddBaseMultiTenancy();

        using ServiceProvider provider = services.BuildServiceProvider();
        Assert.IsType<CurrentTenant>(provider.GetRequiredService<ICurrentTenant>());
        Assert.True(provider.GetRequiredService<ICurrentTenant>().IsAvailable);
        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ICurrentTenant));
        Assert.Throws<ArgumentNullException>(() => MultiTenancyServiceCollectionExtensions.AddBaseMultiTenancy(null!));
    }

    [Fact]
    public void TheTenantAssigner_WritesAPrivateSetter_OnlyWhenThereIsNoTenantYet()
    {
        Guid first = Guid.NewGuid();
        WithPrivateSetter entity = new();

        TenantAssigner.Assign(entity, first);
        TenantAssigner.Assign(entity, Guid.NewGuid());

        Assert.Equal(first, entity.TenantId);
    }

    [Fact]
    public void TheTenantAssigner_SaysWhenThereIsNoSetter_AndRejectsNull()
    {
        Assert.Throws<InvalidOperationException>(() => TenantAssigner.Assign(new WithoutSetter(), Guid.NewGuid()));
        Assert.Throws<ArgumentNullException>(() => TenantAssigner.Assign(null!, Guid.NewGuid()));
    }
}
