using HsnSoft.Base.Domain.Sorting;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Entities;

namespace Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Consts;

/// <summary>The fields a list of AuthRefreshToken entities may be sorted by and the sorting when it sends none.</summary>
public static class AuthRefreshTokenSorting
{
    public const string Default = "createdAt desc";

    public static readonly SortFields<AuthRefreshToken> Fields = new SortFields<AuthRefreshToken>()
        .Add("createdAt", entity => entity.CreatedAt)
        .Add("expiresAt", entity => entity.ExpiresAt);
}
