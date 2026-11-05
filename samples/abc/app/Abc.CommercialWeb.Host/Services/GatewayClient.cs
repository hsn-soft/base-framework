using Abc.CommercialWeb.Host.Models;
using HsnSoft.Base;
using HsnSoft.Base.Http;

namespace Abc.CommercialWeb.Host.Services;

/// <summary>The calls of the commercial web application to the commercial gateway: <c>/auth-server/v1/...</c> and <c>/identity-service/v1/...</c>; the gateway maps them to the services. A call that the gateway refuses is a <see cref="BaseHttpException"/> with its status (401: the token is no longer good).</summary>
public sealed class GatewayClient(HttpClient http)
{
    /// <summary>The access token for a user name and a password, and how long it is valid; <see langword="null"/> when they are wrong.</summary>
    public async Task<TokenPayload?> LoginAsync(string userName, string password, CancellationToken cancellationToken)
    {
        try
        {
            return await http.PostPayloadAsync<TokenPayload>("/auth-server/v1/auth/login", new { username = userName, password }, cancellationToken).ConfigureAwait(false);
        }
        catch (BaseHttpException exception) when (exception.HttpStatusCode < 500)
        {
            return null;
        }
    }

    public async Task<PagedUsers> ListUsersAsync(string? userName, CancellationToken cancellationToken)
        => await http.PostPayloadAsync<PagedUsers>("/identity-service/v1/users/paged-list", new { maxResultCount = 50, userName }, cancellationToken).ConfigureAwait(false) ?? new PagedUsers();
}
