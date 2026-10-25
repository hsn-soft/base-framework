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
    public void TheCurrentUser_IsTheSubjectOfTheRequestsToken()
    {
        using ServiceProvider provider = Build();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("sub", "alice")], "Bearer")),
        };

        ICurrentUser user = provider.GetRequiredService<ICurrentUser>();

        Assert.Equal("alice", user.UserId);
        Assert.True(user.IsAuthenticated);
    }

    [Fact]
    public void AnAnonymousRequest_HasNoCurrentUser()
    {
        using ServiceProvider provider = Build();
        provider.GetRequiredService<IHttpContextAccessor>().HttpContext = new DefaultHttpContext();

        ICurrentUser user = provider.GetRequiredService<ICurrentUser>();

        Assert.Null(user.UserId);
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void WithoutARequest_ThereIsNoCurrentUser()
    {
        using ServiceProvider provider = Build();

        ICurrentUser user = provider.GetRequiredService<ICurrentUser>();

        Assert.Null(user.UserId);
        Assert.False(user.IsAuthenticated);
    }

    [Fact]
    public void AHostsOwnCurrentUser_IsNotReplaced()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Jwt:Issuer"] = "i", ["Jwt:Audience"] = "a", ["Jwt:Key"] = "0123456789-0123456789-0123456789-01" }).Build();
        using ServiceProvider provider = new ServiceCollection().AddLogging().AddSingleton<ICurrentUser, FixedUser>().AddBaseJwtAuthentication(configuration).BuildServiceProvider();

        Assert.Equal("worker", provider.GetRequiredService<ICurrentUser>().UserId);
    }

    private sealed class FixedUser : ICurrentUser
    {
        public string? UserId => "worker";

        public bool IsAuthenticated => true;
    }
}
