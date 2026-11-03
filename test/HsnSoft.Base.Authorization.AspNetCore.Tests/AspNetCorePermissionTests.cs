using System.Security.Claims;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Authorization.AspNetCore.Tests;

public static class TestCodes
{
    public static class Files
    {
        public const string Read = "endpoint.x.files.read";
        public const string Write = "endpoint.x.files.write";
    }
}

public sealed class FakeUser : ICurrentUser
{
    public bool IsAuthenticated { get; set; } = true;

    public Guid? Id { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public string? UserName { get; set; } = "alice";

    public IReadOnlyList<string> Roles { get; set; } = ["operator"];
}

public class AspNetCorePermissionTests
{
    private static PermissionGrants Grants(params (string Code, string? Value)[] entries)
        => new(entries.ToDictionary(entry => entry.Code, entry => entry.Value));

    private static DistributedPermissionStore NewStore()
        => new(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new PermissionOptions { CacheSeconds = 30, KeyPrefix = "permission" }));

    [Fact]
    public void TheAttribute_NamesThePolicyOfThePermission_AndNeedsACode()
    {
        PermissionAuthorizeAttribute attribute = new(TestCodes.Files.Read);

        Assert.Equal("permission:endpoint.x.files.read", attribute.Policy);
        Assert.Throws<ArgumentException>(() => new PermissionAuthorizeAttribute(" "));
        Assert.Throws<ArgumentNullException>(() => new PermissionAuthorizeAttribute(null!));
    }

    [Fact]
    public void ARequirement_SaysWhatItAsksFor()
    {
        PermissionRequirement requirement = new("endpoint.x.a");

        Assert.Equal("endpoint.x.a", requirement.Permission);
        Assert.Equal("PermissionRequirement: endpoint.x.a", requirement.ToString());
    }

    [Fact]
    public async Task ThePolicyProvider_MakesThePolicyOfAPermission_AndLeavesTheOthersToTheDefaultProvider()
    {
        AuthorizationOptions authorization = new();
        authorization.AddPolicy("named", policy => policy.RequireClaim("c"));
        PermissionPolicyProvider provider = new(Options.Create(authorization));

        AuthorizationPolicy? permission = await provider.GetPolicyAsync("permission:endpoint.x.a");
        AuthorizationPolicy? named = await provider.GetPolicyAsync("named");
        AuthorizationPolicy? none = await provider.GetPolicyAsync("nothing");

        Assert.Contains(permission!.Requirements, requirement => requirement is PermissionRequirement { Permission: "endpoint.x.a" });
        Assert.Contains(permission.Requirements, requirement => requirement is Microsoft.AspNetCore.Authorization.Infrastructure.DenyAnonymousAuthorizationRequirement);
        Assert.NotNull(named);
        Assert.Null(none);
        Assert.NotNull(await provider.GetDefaultPolicyAsync());
        Assert.Null(await provider.GetFallbackPolicyAsync());
        await Assert.ThrowsAsync<ArgumentNullException>(() => provider.GetPolicyAsync(null!));
    }

    [Fact]
    public async Task TheHandler_MeetsTheRequirementOnlyWhenThePermissionWasGranted()
    {
        DistributedPermissionStore store = NewStore();
        await store.SetAsync(null, new PermissionSubject(PermissionProviders.Role, "operator"), Grants((TestCodes.Files.Read, null)));
        PermissionRequirementHandler handler = new(new PermissionChecker(new FakeUser(), store, []));
        ClaimsPrincipal principal = new(new ClaimsIdentity("test"));

        AuthorizationHandlerContext granted = new([new PermissionRequirement(TestCodes.Files.Read)], principal, null);
        AuthorizationHandlerContext refused = new([new PermissionRequirement(TestCodes.Files.Write)], principal, null);
        await handler.HandleAsync(granted);
        await handler.HandleAsync(refused);

        Assert.True(granted.HasSucceeded);
        Assert.False(refused.HasSucceeded);
    }

    [Fact]
    public async Task AddBasePermissions_RegistersWhatAnEndpointNeeds_AndTheScopeOfTheProduct()
    {
        IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["Permissions:CacheSeconds"] = "5", ["Permissions:KeyPrefix"] = "p" }).Build();
        ServiceCollection services = new();
        services.AddLogging().AddDistributedMemoryCache().AddSingleton<ICurrentUser>(new FakeUser());

        services.AddBasePermissions(configuration).AddPermissionScope<FixedScopeOfTheTest>();
        await using ServiceProvider provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
        await using AsyncServiceScope scope = provider.CreateAsyncScope();

        Assert.IsType<PermissionPolicyProvider>(provider.GetRequiredService<IAuthorizationPolicyProvider>());
        Assert.Contains(scope.ServiceProvider.GetServices<IAuthorizationHandler>(), handler => handler is PermissionRequirementHandler);
        Assert.IsType<DistributedPermissionStore>(provider.GetRequiredService<IPermissionStore>());
        Assert.IsType<PermissionChecker>(scope.ServiceProvider.GetRequiredService<IPermissionChecker>());
        Assert.Equal("tenant-of-the-test", scope.ServiceProvider.GetRequiredService<IPermissionScopeProvider>().GetScope());
        Assert.Equal(5, provider.GetRequiredService<IOptions<PermissionOptions>>().Value.CacheSeconds);
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddBasePermissions(configuration));
        Assert.Throws<ArgumentNullException>(() => services.AddBasePermissions(null!));
        Assert.Throws<ArgumentNullException>(() => ((IServiceCollection)null!).AddPermissionScope<FixedScopeOfTheTest>());
    }

    private sealed class FixedScopeOfTheTest : IPermissionScopeProvider
    {
        public string? GetScope() => "tenant-of-the-test";
    }
}
