using Abc.CommercialWeb.Host.Models;

namespace Abc.CommercialWeb.Host.Services;

/// <summary>The calls of the commercial web application to the commercial gateway: <c>/auth-server/v1/...</c> and <c>/identity-service/v1/...</c>; the gateway maps them to the services.</summary>
public sealed class GatewayClient(HttpClient http)
{
    /// <summary>The access token for a user name and a password, and how long it is valid; <see langword="null"/> when they are wrong.</summary>
    public async Task<TokenPayload?> LoginAsync(string userName, string password, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await http.PostAsJsonAsync("/auth-server/v1/auth/login", new { username = userName, password }, cancellationToken).ConfigureAwait(false);
        if (!response.IsSuccessStatusCode)
        {
            return null;
        }

        ApiResponse<TokenPayload>? body = await response.Content.ReadFromJsonAsync<ApiResponse<TokenPayload>>(cancellationToken).ConfigureAwait(false);
        return body?.Payload;
    }

    public async Task<PagedUsers> ListUsersAsync(string? userName, CancellationToken cancellationToken)
    {
        using HttpResponseMessage response = await http.PostAsJsonAsync("/identity-service/v1/users/paged-list", new { take = 50, userName }, cancellationToken).ConfigureAwait(false);
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            throw new GatewayUnauthorizedException();
        }

        response.EnsureSuccessStatusCode();
        ApiResponse<PagedUsers>? body = await response.Content.ReadFromJsonAsync<ApiResponse<PagedUsers>>(cancellationToken).ConfigureAwait(false);
        return body?.Payload ?? new PagedUsers();
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
