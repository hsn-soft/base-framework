using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Exceptions;

/// <summary>A refresh token is not there (or the caller may not see it): answered as 404.</summary>
public static class AuthRefreshTokenNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"A refresh token {id} was not found.").WithKey("auth_refresh_token_not_found", id);
}
