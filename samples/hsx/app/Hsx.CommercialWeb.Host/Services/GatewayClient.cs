using HsnSoft.Base;
using HsnSoft.Base.Http;
using Hsx.CommercialWeb.Host.Models;

namespace Hsx.CommercialWeb.Host.Services;

/// <summary>The calls of the commercial web application to the commercial gateway: <c>/auth-server/v1/...</c>, <c>/identity-service/v1/...</c> and <c>/content-service/v1/...</c>; the gateway maps them to the services. A call that the gateway refuses is a <see cref="BaseHttpException"/> with its status (401: the token is no longer good).</summary>
public sealed class GatewayClient(HttpClient http)
{
    // the client of the auth server this application signs its users in through (Auth:Clients of the auth server: a public client, no secret)
    private const string ClientId = "hsx-web";

    /// <summary>The access token for a tenant, an account (an email or a user name) and a password, and how long it is valid; <see langword="null"/> when they are wrong.</summary>
    public async Task<TokenPayload?> LoginAsync(string tenant, string account, string password, CancellationToken cancellationToken)
    {
        try
        {
            return await http.PostPayloadAsync<TokenPayload>("/auth-server/v1/auth/login", new { clientId = ClientId, tenant, account, password }, cancellationToken).ConfigureAwait(false);
        }
        catch (BaseHttpException exception) when (exception.HttpStatusCode < 500)
        {
            return null;
        }
    }

    public Task<Paged<TenantItem>> ListTenantsAsync(CancellationToken cancellationToken) => ListAsync<TenantItem>("/identity-service/v1/tenants/paged-list", new { maxResultCount = 100 }, cancellationToken);

    public Task<Paged<UserItem>> ListUsersAsync(string? email, CancellationToken cancellationToken) => ListAsync<UserItem>("/identity-service/v1/app-users/paged-list", new { maxResultCount = 100, email }, cancellationToken);

    public Task<Paged<ContentItem>> ListContentsAsync(string? scopeKey, CancellationToken cancellationToken) => ListAsync<ContentItem>("/content-service/v1/customer-contents/paged-list", new { maxResultCount = 100, scopeKey }, cancellationToken);

    private async Task<Paged<T>> ListAsync<T>(string path, object request, CancellationToken cancellationToken)
        => await http.PostPayloadAsync<Paged<T>>(path, request, cancellationToken).ConfigureAwait(false) ?? new Paged<T>();
}
