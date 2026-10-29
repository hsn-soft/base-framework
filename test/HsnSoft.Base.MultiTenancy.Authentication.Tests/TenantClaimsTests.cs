using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.MultiTenancy.Authentication.Tests;

public class TenantClaimsTests
{
    private static ServiceProvider Build()
        => new ServiceCollection().AddBaseTenantClaims().BuildServiceProvider();

    private static ClaimsPrincipal Signed(params Claim[] claims) => new(new ClaimsIdentity(claims, "Bearer"));

    [Fact]
    public void TheTenant_IsWhatTheClaimsOfTheTokenSay()
    {
        Guid tenant = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        using ServiceProvider provider = Build();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = Signed(
                new Claim(TenantClaimTypes.TenantId, tenant.ToString()),
                new Claim(TenantClaimTypes.IsSystemTenant, "TRUE"),
                new Claim(TenantClaimTypes.AllowedTenantId, other.ToString()),
                new Claim(TenantClaimTypes.AllowedTenantId, "not-a-guid")),
        };

        ICurrentTenant current = provider.GetRequiredService<ICurrentTenant>();

        Assert.True(current.IsAvailable);
        Assert.Equal(tenant, current.Id);
        Assert.True(current.IsSystemTenant);
        Assert.Equal([other, tenant], current.AllowedTenantIds);
    }

    [Fact]
    public void ATokenWithoutTenantClaims_HasNoTenant()
    {
        using ServiceProvider provider = Build();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext { User = Signed(new Claim("sub", "alice"), new Claim(TenantClaimTypes.TenantId, "nope")) };

        ICurrentTenant current = provider.GetRequiredService<ICurrentTenant>();

        Assert.False(current.IsAvailable);
        Assert.False(current.IsSystemTenant);
        Assert.Empty(current.AllowedTenantIds);
    }

    [Fact]
    public void AnAnonymousRequest_AndNoRequest_HaveNoTenant_AndChangeStillWorks()
    {
        using ServiceProvider provider = Build();
        ICurrentTenant current = provider.GetRequiredService<ICurrentTenant>();
        Assert.False(current.IsAvailable);

        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext();
        Assert.False(current.IsAvailable);

        Guid acting = Guid.NewGuid();
        using (current.Change(acting))
        {
            Assert.Equal(acting, current.Id);
        }
    }

    [Fact]
    public void TheRegistration_AddsTheSourceOnce_AndRejectsNull()
    {
        ServiceCollection services = new();
        services.AddBaseTenantClaims().AddBaseTenantClaims();

        Assert.Single(services, descriptor => descriptor.ServiceType == typeof(ITenantSource));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBaseTenantClaims());
    }

    [Fact]
    public void TheClaimNames_AreTheShortJwtNames()
    {
        Assert.Equal(["tenant_id", "is_system_tenant", "allowed_tenant_id"], [TenantClaimTypes.TenantId, TenantClaimTypes.IsSystemTenant, TenantClaimTypes.AllowedTenantId]);
    }
}
