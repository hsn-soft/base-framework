using System.Security.Claims;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace HsnSoft.Base.Authentication.Tests;

public class CurrentUserTests
{
    private static ServiceProvider Build()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Issuer"] = "i", ["Jwt:Audience"] = "a", ["Jwt:Key"] = "0123456789-0123456789-0123456789-01" }).Build();
        return new ServiceCollection().AddLogging().AddBaseJwtAuthentication(configuration).BuildServiceProvider();
    }

    [Fact]
    public void TheCurrentUser_IsWhatTheTokenOfTheRequestSays()
    {
        Guid id = Guid.NewGuid();
        Guid tenant = Guid.NewGuid();
        Guid other = Guid.NewGuid();
        using ServiceProvider provider = Build();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                [
                    new Claim("sub", "alice"),
                    new Claim("user_id", id.ToString()),
                    new Claim("tenant_id", tenant.ToString()),
                    new Claim("is_system_tenant", "TRUE"),
                    new Claim("allowed_tenant_id", other.ToString()),
                    new Claim("allowed_tenant_id", "not-a-guid"),
                ],
                "Bearer")),
        };

        ICurrentUser user = provider.GetRequiredService<ICurrentUser>();

        Assert.Equal("alice", user.UserName);
        Assert.Equal(id, user.Id);
        Assert.Equal(tenant, user.TenantId);
        Assert.True(user.IsSystemTenant);
        Assert.Equal([other], user.AllowedTenantIds);
        Assert.True(user.IsAuthenticated);
    }

    [Fact]
    public void AnAnonymousRequest_HasNoCurrentUser()
    {
        using ServiceProvider provider = Build();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext();

        ICurrentUser user = provider.GetRequiredService<ICurrentUser>();

        Assert.Null(user.Id);
        Assert.Null(user.UserName);
        Assert.Null(user.TenantId);
        Assert.False(user.IsSystemTenant);
        Assert.Empty(user.AllowedTenantIds);
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void WithoutARequest_ThereIsNoCurrentUser()
    {
        using ServiceProvider provider = Build();

        ICurrentUser user = provider.GetRequiredService<ICurrentUser>();

        Assert.Null(user.Id);
        Assert.Null(user.UserName);
        Assert.Null(user.TenantId);
        Assert.False(user.IsSystemTenant);
        Assert.Empty(user.AllowedTenantIds);
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void AHostsOwnCurrentUser_IsNotReplaced()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Issuer"] = "i", ["Jwt:Audience"] = "a", ["Jwt:Key"] = "0123456789-0123456789-0123456789-01" }).Build();
        using ServiceProvider provider = new ServiceCollection().AddLogging().AddSingleton<ICurrentUser, FixedUser>().AddBaseJwtAuthentication(configuration).BuildServiceProvider();

        Assert.Equal("worker", provider.GetRequiredService<ICurrentUser>().UserName);
    }

    private sealed class FixedUser : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid? Id => null;

        public string? UserName => "worker";

        public Guid? TenantId => null;

        public bool IsSystemTenant => false;

        public IReadOnlyList<Guid> AllowedTenantIds => [];
    }
}
