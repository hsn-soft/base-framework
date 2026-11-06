using Hsx.AuthServer.Host.Contracts.AuthDomain.Dtos;
using Hsx.AuthServer.Host.Contracts.AuthDomain.Dtos.Submits;

namespace Hsx.AuthServer.Host.Contracts.AuthDomain.Services;

/// <summary>What the auth server does: signs a user in, gives a new access token for a refresh token, signs it out, and says who the token is of. The refresh token is a cookie of the request and the response, which the service reads and sets.</summary>
public interface IAuthAppService
{
    /// <summary>A tenant, an account (an email or a user name) and a password for an access token; with <see cref="LoginDto.OfflineAccess"/> a refresh token comes too, in a cookie.</summary>
    /// <exception cref="HsnSoft.Base.BaseHttpException">The client is not known or does not prove itself (401 <c>invalid_client</c>), may not sign a user in (403 <c>grant_not_allowed</c>), asks for an audience it has not (400 <c>audience_not_allowed</c>) or for a refresh token it may not have (400 <c>offline_access_not_allowed</c>); the tenant, the account or the password is wrong, the user is not active or is locked out (401, and the caller cannot tell which).</exception>
    Task<TokenDto> LoginAsync(LoginDto input, CancellationToken cancellationToken = default);

    /// <summary>A new access token for the refresh token of the cookie; the token is used once and the cookie gets the one that takes its place.</summary>
    /// <exception cref="HsnSoft.Base.BaseHttpException">The client is not known or does not prove itself (401 <c>invalid_client</c>), there is no refresh token, or it is not valid any more (401, and the cookie is removed).</exception>
    Task<TokenDto> RefreshAsync(RefreshDto input, CancellationToken cancellationToken = default);

    /// <summary>A token for a client itself (a machine, no user): its audiences and nothing else.</summary>
    /// <exception cref="HsnSoft.Base.BaseHttpException">The client is not known or does not prove itself (401 <c>invalid_client</c>), may not ask for a token for itself (403 <c>grant_not_allowed</c>) or asks for an audience it has not (400 <c>audience_not_allowed</c>).</exception>
    Task<TokenDto> ClientTokenAsync(ClientTokenRequestDto input, CancellationToken cancellationToken = default);

    Task<LogoutResultDto> LogoutAsync(CancellationToken cancellationToken = default);

    Task<MeDto> GetMeAsync(CancellationToken cancellationToken = default);
}
