using System.Security.Claims;
using HsnSoft.Base.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.Authorization.Tests;

public static class TestCodes
{
    public const string Outside = "endpoint.x.outside";

    public static class Files
    {
        public const string Read = "endpoint.x.files.read";
        public const string Write = "endpoint.x.files.write";
        public static readonly string NotAConstant = "endpoint.x.files.static-readonly";
        public const int NotAString = 5;
    }

    public static class Limits
    {
        public const string MaxBytes = "constraint.x.max-bytes";

        public static class Deep
        {
            public const string Flag = "operation.x.deep.flag";
        }
    }
}

public sealed class FakeUser : ICurrentUser
{
    public bool IsAuthenticated { get; set; } = true;

    public Guid? Id { get; set; } = Guid.Parse("11111111-1111-1111-1111-111111111111");

    public string? UserName { get; set; } = "alice";

    public IReadOnlyList<string> Roles { get; set; } = ["operator"];
}

public sealed class FixedScope(string? scope) : IPermissionScopeProvider
{
    public string? GetScope() => scope;
}

public class PermissionTests
{
    private static PermissionGrants Grants(params (string Code, string? Value)[] entries)
        => new(entries.ToDictionary(entry => entry.Code, entry => entry.Value));

    private static DistributedPermissionStore NewStore(int cacheSeconds = 30, IDistributedCache? distributed = null, string prefix = "permission")
        => new(
            distributed ?? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            new MemoryCache(new MemoryCacheOptions()),
            Options.Create(new PermissionOptions { CacheSeconds = cacheSeconds, KeyPrefix = prefix }));

    private sealed class CountingCache(IDistributedCache inner) : IDistributedCache
    {
        public int Reads { get; private set; }

        public byte[]? Get(string key) => inner.Get(key);

        public Task<byte[]?> GetAsync(string key, CancellationToken token = default)
        {
            Reads++;
            return inner.GetAsync(key, token);
        }

        public void Set(string key, byte[] value, DistributedCacheEntryOptions options) => inner.Set(key, value, options);

        public Task SetAsync(string key, byte[] value, DistributedCacheEntryOptions options, CancellationToken token = default) => inner.SetAsync(key, value, options, token);

        public void Refresh(string key) => inner.Refresh(key);

        public Task RefreshAsync(string key, CancellationToken token = default) => inner.RefreshAsync(key, token);

        public void Remove(string key) => inner.Remove(key);

        public Task RemoveAsync(string key, CancellationToken token = default) => inner.RemoveAsync(key, token);
    }

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
    public async Task TheStore_KeepsWhatWasWritten_ForEachScopeAndSubject()
    {
        DistributedPermissionStore store = NewStore();
        PermissionSubject role = new(PermissionProviders.Role, "Operator");
        await store.SetAsync("t1", role, Grants((TestCodes.Files.Read, null), (TestCodes.Limits.MaxBytes, "100")));

        PermissionGrants found = await store.GetAsync("t1", role);

        Assert.True(found.Has(TestCodes.Files.Read));
        Assert.Equal("100", found.ValueOf(TestCodes.Limits.MaxBytes));
        Assert.Null(found.ValueOf(TestCodes.Files.Read));
        Assert.Null(found.ValueOf("nothing"));
        Assert.False((await store.GetAsync("t2", role)).Has(TestCodes.Files.Read));
        Assert.False((await store.GetAsync(null, role)).Has(TestCodes.Files.Read));
        Assert.False((await store.GetAsync("t1", new PermissionSubject(PermissionProviders.User, "Operator"))).Has(TestCodes.Files.Read));

        // the same role in any case, and a blank scope, are the same entry
        Assert.True((await store.GetAsync("T1", new PermissionSubject(PermissionProviders.Role, "operator"))).Has(TestCodes.Files.Read));
        await store.SetAsync(" ", role, Grants((TestCodes.Files.Write, null)));
        Assert.True((await store.GetAsync(null, role)).Has(TestCodes.Files.Write));
    }

    [Fact]
    public async Task TheStore_ReadsAnEntryOnceInTheTimeItKeepsIt_AndAgainWhenItWasWrittenOrRemoved()
    {
        CountingCache cache = new(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));
        DistributedPermissionStore store = NewStore(30, cache);
        PermissionSubject role = new(PermissionProviders.Role, "r");

        _ = await store.GetAsync("s", role);
        _ = await store.GetAsync("s", role);
        Assert.Equal(1, cache.Reads);

        await store.SetAsync("s", role, Grants((TestCodes.Files.Read, null)));
        Assert.True((await store.GetAsync("s", role)).Has(TestCodes.Files.Read));
        Assert.Equal(2, cache.Reads);

        await store.RemoveAsync("s", role);
        Assert.False((await store.GetAsync("s", role)).Has(TestCodes.Files.Read));
        Assert.Equal(3, cache.Reads);
    }

    [Fact]
    public async Task TheStore_WithNoTimeToKeepAnEntry_ReadsItEveryTime()
    {
        CountingCache cache = new(new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));
        DistributedPermissionStore store = NewStore(0, cache);
        PermissionSubject role = new(PermissionProviders.Role, "r");

        _ = await store.GetAsync("s", role);
        _ = await store.GetAsync("s", role);

        Assert.Equal(2, cache.Reads);
    }

    [Fact]
    public async Task TheStore_UsesItsPrefix_AndRefusesNoGrants()
    {
        MemoryDistributedCache cache = new(Options.Create(new MemoryDistributedCacheOptions()));
        DistributedPermissionStore store = NewStore(30, cache, "myperm");
        await store.SetAsync("s", new PermissionSubject(PermissionProviders.Role, "r"), Grants((TestCodes.Files.Read, null)));

        Assert.NotNull(await cache.GetAsync("myperm:s:role:r"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => store.SetAsync("s", new PermissionSubject(PermissionProviders.Role, "r"), null!));
        Assert.Throws<ArgumentNullException>(() => new PermissionGrants(null!));
        Assert.Empty(PermissionGrants.None.Entries);
    }

    [Fact]
    public async Task TheStore_ReadsAnEntryThatIsJsonNull_AsNothingGranted()
    {
        MemoryDistributedCache cache = new(Options.Create(new MemoryDistributedCacheOptions()));
        await cache.SetAsync("permission:s:role:r", "null"u8.ToArray());
        DistributedPermissionStore store = NewStore(30, cache);

        Assert.Empty((await store.GetAsync("s", new PermissionSubject(PermissionProviders.Role, "r"))).Entries);
    }

    private static PermissionChecker NewChecker(FakeUser user, DistributedPermissionStore store, params IPermissionScopeProvider[] scopes) => new(user, store, scopes);

    [Fact]
    public async Task TheChecker_GrantsWhatTheUserOrOneOfItsRolesWasGranted_InTheScopeOfTheRequest()
    {
        DistributedPermissionStore store = NewStore();
        FakeUser user = new() { Roles = ["operator", "Viewer", "viewer"] };
        await store.SetAsync("t1", new PermissionSubject(PermissionProviders.Role, "operator"), Grants((TestCodes.Files.Read, null)));
        await store.SetAsync("t1", new PermissionSubject(PermissionProviders.Role, "viewer"), Grants((TestCodes.Files.Write, null)));
        await store.SetAsync("t1", new PermissionSubject(PermissionProviders.User, user.Id!.Value.ToString()), Grants((TestCodes.Outside, null)));
        PermissionChecker inScope = NewChecker(user, store, new FixedScope(null), new FixedScope("t1"), new FixedScope("t2"));
        PermissionChecker otherScope = NewChecker(user, store, new FixedScope("t2"));
        PermissionChecker noScope = NewChecker(user, store);

        Assert.True(await inScope.IsGrantedAsync(TestCodes.Files.Read));
        Assert.True(await inScope.IsGrantedAsync(TestCodes.Files.Write));
        Assert.True(await inScope.IsGrantedAsync(TestCodes.Outside));
        Assert.False(await inScope.IsGrantedAsync("endpoint.x.never"));
        Assert.False(await otherScope.IsGrantedAsync(TestCodes.Files.Read));
        Assert.False(await noScope.IsGrantedAsync(TestCodes.Files.Read));
    }

    [Fact]
    public async Task TheChecker_GrantsNothingToACallerWhoIsNotSignedIn_AndHasNoUserGrantsWithoutAnId()
    {
        DistributedPermissionStore store = NewStore();
        await store.SetAsync(null, new PermissionSubject(PermissionProviders.Role, "operator"), Grants((TestCodes.Files.Read, null)));

        Assert.False(await NewChecker(new FakeUser { IsAuthenticated = false }, store).IsGrantedAsync(TestCodes.Files.Read));
        Assert.Null(await NewChecker(new FakeUser { IsAuthenticated = false }, store).GetValueAsync(TestCodes.Files.Read));
        Assert.True(await NewChecker(new FakeUser { Id = null }, store).IsGrantedAsync(TestCodes.Files.Read));
        await Assert.ThrowsAsync<ArgumentException>(() => NewChecker(new FakeUser(), store).IsGrantedAsync(" "));
        await Assert.ThrowsAsync<ArgumentException>(() => NewChecker(new FakeUser(), store).GetValueAsync(""));
    }

    [Fact]
    public async Task TheChecker_TakesTheValueOfAConstraint_TheUsersOwnFirst_ThenTheRolesInTheirOrder()
    {
        DistributedPermissionStore store = NewStore();
        FakeUser user = new() { Roles = ["a", "b"] };
        await store.SetAsync(null, new PermissionSubject(PermissionProviders.Role, "a"), Grants((TestCodes.Limits.MaxBytes, null)));
        await store.SetAsync(null, new PermissionSubject(PermissionProviders.Role, "b"), Grants((TestCodes.Limits.MaxBytes, "200"), (TestCodes.Limits.Deep.Flag, "true")));
        PermissionChecker checker = NewChecker(user, store);

        Assert.Equal("200", await checker.GetValueAsync(TestCodes.Limits.MaxBytes));
        Assert.Equal(200, await checker.GetIntAsync(TestCodes.Limits.MaxBytes));
        Assert.Equal(200L, await checker.GetLongAsync(TestCodes.Limits.MaxBytes));
        Assert.Equal(200m, await checker.GetDecimalAsync(TestCodes.Limits.MaxBytes));
        Assert.True(await checker.GetBooleanAsync(TestCodes.Limits.Deep.Flag));
        Assert.Null(await checker.GetValueAsync("constraint.x.nothing"));

        await store.SetAsync(null, new PermissionSubject(PermissionProviders.User, user.Id!.Value.ToString()), Grants((TestCodes.Limits.MaxBytes, "50")));
        Assert.Equal("50", await checker.GetValueAsync(TestCodes.Limits.MaxBytes));
    }

    [Fact]
    public async Task TheTypedValues_AreNothingWhenTheValueIsNotOfThatKind_OrNotThere()
    {
        DistributedPermissionStore store = NewStore();
        await store.SetAsync(null, new PermissionSubject(PermissionProviders.Role, "operator"), Grants((TestCodes.Limits.MaxBytes, "many"), (TestCodes.Files.Read, null)));
        PermissionChecker checker = NewChecker(new FakeUser(), store);

        Assert.Null(await checker.GetIntAsync(TestCodes.Limits.MaxBytes));
        Assert.Null(await checker.GetLongAsync(TestCodes.Limits.MaxBytes));
        Assert.Null(await checker.GetDecimalAsync(TestCodes.Limits.MaxBytes));
        Assert.Null(await checker.GetBooleanAsync(TestCodes.Limits.MaxBytes));
        Assert.Null(await checker.GetIntAsync(TestCodes.Files.Read));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((IPermissionChecker)null!).GetIntAsync("x"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((IPermissionChecker)null!).GetLongAsync("x"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((IPermissionChecker)null!).GetDecimalAsync("x"));
        await Assert.ThrowsAsync<ArgumentNullException>(() => ((IPermissionChecker)null!).GetBooleanAsync("x"));
    }

    [Fact]
    public async Task ARequiredConstraint_IsItsValue_Or403BeforeAnythingIsDone()
    {
        DistributedPermissionStore store = NewStore();
        await store.SetAsync(null, new PermissionSubject(PermissionProviders.Role, "operator"), Grants((TestCodes.Limits.MaxBytes, "200"), (TestCodes.Limits.Deep.Flag, "yes"), (TestCodes.Files.Read, null)));
        PermissionChecker checker = NewChecker(new FakeUser(), store);

        Assert.Equal("200", await checker.RequireValueAsync(TestCodes.Limits.MaxBytes));
        Assert.Equal(200L, await checker.RequireLongAsync(TestCodes.Limits.MaxBytes));
        Assert.Equal(200m, await checker.RequireDecimalAsync(TestCodes.Limits.MaxBytes));
        Assert.Equal(200, await checker.RequireIntAsync(TestCodes.Limits.MaxBytes));
        foreach (Func<Task> call in new Func<Task>[]
                 {
                     () => checker.RequireValueAsync("constraint.x.nothing"),
                     () => checker.RequireLongAsync("constraint.x.nothing"),
                     () => checker.RequireDecimalAsync(TestCodes.Limits.Deep.Flag),
                     () => checker.RequireIntAsync(TestCodes.Limits.Deep.Flag),
                     () => checker.RequireLongAsync(TestCodes.Files.Read),
                 })
        {
            BaseHttpException refused = await Assert.ThrowsAsync<BaseHttpException>(call);
            Assert.Equal(403, refused.HttpStatusCode);
            Assert.Equal("permission_constraint_not_granted", refused.MessageKey);
        }

        await Assert.ThrowsAsync<ArgumentNullException>(() => ((IPermissionChecker)null!).RequireValueAsync("x"));
    }

    [Fact]
    public void TheCatalog_ListsTheStringConstantsOfAClassAndOfTheClassesNestedInIt()
    {
        IReadOnlyList<string> codes = PermissionCatalog.CodesOf(typeof(TestCodes));

        Assert.Equal(
            [TestCodes.Outside, TestCodes.Files.Read, TestCodes.Files.Write, TestCodes.Limits.MaxBytes, TestCodes.Limits.Deep.Flag],
            codes);
        Assert.Throws<ArgumentNullException>(() => PermissionCatalog.CodesOf(null!));
    }

    [Fact]
    public async Task TheHandler_MeetsTheRequirementOnlyWhenThePermissionWasGranted()
    {
        DistributedPermissionStore store = NewStore();
        await store.SetAsync(null, new PermissionSubject(PermissionProviders.Role, "operator"), Grants((TestCodes.Files.Read, null)));
        PermissionRequirementHandler handler = new(NewChecker(new FakeUser(), store));
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
