using System.Security.Claims;
using HsnSoft.Base.Security;

namespace HsnSoft.Base.Authentication.Tests;

public class ClaimsPrincipalExtensionsTests
{
    private static ClaimsPrincipal User(params Claim[] claims) => new(new ClaimsIdentity(claims, "test"));

    [Fact]
    public void ClaimTypes_AreTheShortJwtNames()
    {
        Assert.Equal("sub", BaseClaimTypes.UserId);
        Assert.Equal("role", BaseClaimTypes.Role);
    }

    [Fact]
    public void GetUserId_ReturnsTheSubject() => Assert.Equal("alice", User(new Claim("sub", "alice")).GetUserId());

    [Fact]
    public void GetUserId_Anonymous_IsNull() => Assert.Null(new ClaimsPrincipal(new ClaimsIdentity()).GetUserId());

    [Fact]
    public void GetRequiredUserId_ReturnsTheSubject() => Assert.Equal("alice", User(new Claim("sub", "alice")).GetRequiredUserId());

    [Fact]
    public void GetRequiredUserId_Anonymous_Throws()
        => Assert.Throws<BusinessRuleException>(() => new ClaimsPrincipal(new ClaimsIdentity()).GetRequiredUserId());

    [Fact]
    public void GetRoles_ReturnsEveryRoleClaim()
        => Assert.Equal(["admin", "customer"], User(new Claim("role", "admin"), new Claim("role", "customer"), new Claim("sub", "x")).GetRoles());

    [Fact]
    public void GetRoles_WithoutRoles_IsEmpty() => Assert.Empty(User(new Claim("sub", "x")).GetRoles());

    [Fact]
    public void NullPrincipal_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => ClaimsPrincipalExtensions.GetUserId(null!));
        Assert.Throws<ArgumentNullException>(() => ClaimsPrincipalExtensions.GetRoles(null!));
    }
}
