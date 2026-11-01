using System.Net;
using Hsx.CommercialWeb.Host.Models;

namespace Hsx.CommercialWeb.Host.Services;

/// <summary>The calls of the commercial web application to the commercial gateway: <c>/auth-server/v1/...</c>, <c>/identity-service/v1/...</c> and <c>/content-service/v1/...</c>; the gateway maps them to the services.</summary>
public sealed class GatewayClient(HttpClient http)
{
    /// <summary>The access token for an email and a password, and how long it is valid; <see langword="null"/> when they are wrong.</summary>
    public async Task<TokenPayload?> LoginAsync(string email, string password, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await http.PostAsJsonAsync("/auth-server/v1/auth/login", new { email, password }, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        ApiResponse<TokenPayload>? body = await response.Content.ReadFromJsonAsync<ApiResponse<TokenPayload>>(cancellationToken).ConfigureAwait(false);
        return body?.Payload;
    }

    public Task<Paged<TenantItem>> ListTenantsAsync(CancellationToken cancellationToken) => ListAsync<TenantItem>("/identity-service/v1/tenants/paged-list", new { maxResultCount = 100 }, cancellationToken);

    public Task<Paged<UserItem>> ListUsersAsync(string? email, CancellationToken cancellationToken) => ListAsync<UserItem>("/identity-service/v1/app-users/paged-list", new { maxResultCount = 100, email }, cancellationToken);

    public Task<Paged<ContentItem>> ListContentsAsync(string? scopeKey, CancellationToken cancellationToken) => ListAsync<ContentItem>("/content-service/v1/customer-contents/paged-list", new { maxResultCount = 100, scopeKey }, cancellationToken);

    private async Task<Paged<T>> ListAsync<T>(string path, object request, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await http.PostAsJsonAsync(path, request, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new GatewayUnauthorizedException();
        }

        response.EnsureSuccessStatusCode();
        ApiResponse<Paged<T>>? body = await response.Content.ReadFromJsonAsync<ApiResponse<Paged<T>>>(cancellationToken).ConfigureAwait(false);
        return body?.Payload ?? new Paged<T>();
    }
}

/// <summary>The gateway (the service behind it) did not accept the token of the signed-in user: it expired, or it is not one of ours. The page signs the user out.</summary>
public sealed class GatewayUnauthorizedException : Exception
{
    public GatewayUnauthorizedException()
        : base("The gateway did not accept the token.")
    {
    }
}
