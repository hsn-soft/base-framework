using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;

namespace Def.Signer.Host.Security;

public static class ApiKeyDefaults
{
    public const string Scheme = "ApiKey";
    public const string HeaderName = "X-Api-Key";
}

/// <summary>
/// API keys live in Redis as <c>apikey:{SHA-256 of the key}</c> -> owner name, so the keys themselves are never stored.
/// </summary>
public sealed class ApiKeyStore(IDistributedCache cache)
{
    public Task<string?> FindOwnerAsync(string apiKey, CancellationToken cancellationToken)
        => cache.GetStringAsync(CacheKey(apiKey), cancellationToken);

    public Task AddAsync(string owner, string apiKey, CancellationToken cancellationToken)
        => cache.SetStringAsync(CacheKey(apiKey), owner, cancellationToken);

    private static string CacheKey(string apiKey) => "apikey:" + Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(apiKey)));
}

public sealed class ApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    ApiKeyStore store) : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(ApiKeyDefaults.HeaderName, out StringValues apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            return AuthenticateResult.NoResult();
        }

        string? owner = await store.FindOwnerAsync(apiKey.ToString(), Context.RequestAborted);
        if (owner is null)
        {
            return AuthenticateResult.Fail("Unknown API key.");
        }

        var identity = new ClaimsIdentity([new Claim(ClaimTypes.Name, owner)], Scheme.Name);
        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}

public static class ApiKeyExtensions
{
    public static IServiceCollection AddApiKeyAuthentication(this IServiceCollection services)
    {
        services.AddSingleton<ApiKeyStore>();
        services.AddAuthentication(ApiKeyDefaults.Scheme).AddScheme<AuthenticationSchemeOptions, ApiKeyAuthenticationHandler>(ApiKeyDefaults.Scheme, null);
        services.AddAuthorization();
        return services;
    }

    /// <summary>Puts the keys of the <c>ApiKeys:Seed</c> section (owner -> key) into Redis. A sample convenience: a real product issues keys through its own admin flow.</summary>
    public static async Task SeedApiKeysAsync(this IServiceProvider services, IConfiguration configuration)
    {
        ApiKeyStore store = services.GetRequiredService<ApiKeyStore>();
        foreach (IConfigurationSection entry in configuration.GetSection("ApiKeys:Seed").GetChildren())
        {
            if (entry.Value is { Length: > 0 } apiKey)
            {
                await store.AddAsync(entry.Key, apiKey, CancellationToken.None);
            }
        }
    }
}
