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
        using ServiceProvider provider = Build();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "alice"), new Claim("user_id", id.ToString())], "Bearer")),
        };

        ICurrentUser user = provider.GetRequiredService<ICurrentUser>();

        Assert.Equal("alice", user.UserName);
        Assert.Equal(id, user.Id);
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
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void WithoutARequest_ThereIsNoCurrentUser()
    {
        using ServiceProvider provider = Build();

        ICurrentUser user = provider.GetRequiredService<ICurrentUser>();

        Assert.Null(user.Id);
        Assert.Null(user.UserName);
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void WithoutARequest_TheCurrentUser_IsTheOneTheCodeActsAs()
    {
        Guid id = Guid.NewGuid();
        using ServiceProvider provider = Build();
        ICurrentUser user = provider.GetRequiredService<ICurrentUser>();

        using (ActingUser.Use(new UserContext(id, "alice", ["customer", "support"])))
        {
            Assert.True(user.IsAuthenticated);
            Assert.Equal((id, "alice"), (user.Id, user.UserName));
            Assert.Equal(["customer", "support"], user.Roles);
        }

        Assert.False(user.IsAuthenticated);
        Assert.Empty(user.Roles);
    }

    [Fact]
    public void ASignedInUserOfTheRequest_WinsOverTheOneTheCodeActsAs()
    {
        Guid id = Guid.NewGuid();
        using ServiceProvider provider = Build();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "bob"), new Claim("user_id", id.ToString()), new Claim("role", "admin")], "Bearer")),
        };
        ICurrentUser user = provider.GetRequiredService<ICurrentUser>();

        using (ActingUser.Use(new UserContext(Guid.NewGuid(), "alice", ["customer"])))
        {
            Assert.Equal((id, "bob"), (user.Id, user.UserName));
            Assert.Equal(["admin"], user.Roles);
        }
    }

    [Fact]
    public void AnActingUserWithoutAnId_IsNotAuthenticated()
    {
        using ServiceProvider provider = Build();

        using (ActingUser.Use(new UserContext(null, null, [])))
        {
            Assert.False(provider.GetRequiredService<ICurrentUser>().IsAuthenticated);
        }
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
    }
}
