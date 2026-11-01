using HsnSoft.Base;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Repositories;
using Hsx.IdentityService.Domain.AppUserDomain.Security;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Repositories;
using Hsx.Shared.Helper;

namespace Hsx.AuthServer.Host.Security;

/// <summary>Who signed in: what goes into the claims of the token. <paramref name="AllowedTenantIds"/> are the tenants under the tenant of the user; <paramref name="ScopeKeys"/> are the keys of the subscriptions of its roles.</summary>
public sealed record AuthenticatedUser(
    Guid Id,
    string UserName,
    string DisplayName,
    string Email,
    IReadOnlyList<string> Roles,
    Guid TenantId,
    bool IsSystemTenant,
    string Language,
    IReadOnlyList<Guid> AllowedTenantIds,
    IReadOnlyList<string> ScopeKeys);

/// <summary>Checks an email and a password against the users of the identity service (the same database; the auth server only reads it) and works out what the user may see: the tenants under its own and the scopes of its subscriptions.</summary>
public sealed class UserAuthenticator(ISignInRepository signIn, ITenantRepository tenants)
{
    /// <summary>The most ids a claim list may hold: a token that carries more is too big for a header, so such a tenant is given scope keys instead.</summary>
    public const int MaxClaimValues = 50;

    // checked when the user does not exist, so a wrong email takes as long as a wrong password
    private static readonly string s_unknownUserHash = PasswordHasher.Hash("not-a-password-of-anyone");

    /// <summary>The user, or <see langword="null"/> when the email is unknown, the user or its tenant is not active or the password is wrong (the caller cannot tell which).</summary>
    public async Task<AuthenticatedUser?> AuthenticateAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        SignInInfo? info = await signIn.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        bool passwordMatches = PasswordHasher.Verify(info?.User.PasswordHash ?? s_unknownUserHash, password);
        if (info is null || !info.User.IsActive || !info.Tenant.IsActive || !passwordMatches)
        {
            return null;
        }

        AppUser user = info.User;
        Tenant tenant = info.Tenant;
        bool isSystem = tenant.Id == HsxTenants.System;

        // the system tenant is under no isolation, so it lists nothing; any other tenant sees its own and the tenants under it
        IReadOnlyList<Guid> descendants = isSystem ? [] : await tenants.GetDescendantIdsAsync(tenant, cancellationToken).ConfigureAwait(false);
        if (descendants.Count > MaxClaimValues || info.ScopeKeys.Count > MaxClaimValues)
        {
            throw new BaseHttpException(422, $"The token would list more than {MaxClaimValues} tenants or scopes.").WithKey("token_too_large");
        }

        return new AuthenticatedUser(user.Id, user.UserName, user.DisplayName, user.Email, info.Roles, tenant.Id, isSystem, user.LanguageCode ?? "en", descendants, info.ScopeKeys);
    }
}
