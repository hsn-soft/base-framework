using HsnSoft.Base;
using HsnSoft.Base.Domain.Repositories;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Repositories;
using Hsx.IdentityService.Domain.AppUserDomain.Security;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Consts;
using Hsx.IdentityService.Domain.AuthLoginAuditDomain.Entities;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Repositories;
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

/// <summary>Where a sign-in came from, for the audit.</summary>
public sealed record SignInOrigin(string? IpAddress, string? UserAgent);

/// <summary>
/// Checks an email and a password against the users of the identity service (the same database; the auth server writes only what a sign-in changes: the count of failures, the lockout, the audit) and works out what the user may see: the
/// tenants under its own and the scopes of its subscriptions. Every attempt is written to the audit, with why it failed; the caller is only told that it did.
/// </summary>
public sealed class UserAuthenticator(
    ISignInRepository signIn,
    ITenantRepository tenants,
    IPasswordPolicyProvider policies,
    IRepository<AuthLoginAudit, Guid> audits)
{
    /// <summary>The most ids a claim list may hold: a token that carries more is too big for a header, so such a tenant is given scope keys instead.</summary>
    public const int MaxClaimValues = 50;

    // checked when the user does not exist, so a wrong email takes as long as a wrong password
    private static readonly string s_unknownUserHash = PasswordHasher.Hash("not-a-password-of-anyone");

    /// <summary>The user, or <see langword="null"/> when the email is unknown, the user or its tenant is not active, the user is locked out or the password is wrong (the caller cannot tell which).</summary>
    public async Task<AuthenticatedUser?> AuthenticateAsync(string email, string password, SignInOrigin origin, CancellationToken cancellationToken = default)
    {
        SignInInfo? info = await signIn.FindByEmailAsync(email, cancellationToken).ConfigureAwait(false);
        bool passwordMatches = PasswordHasher.Verify(info?.User.PasswordHash ?? s_unknownUserHash, password);
        if (info is null)
        {
            await AuditAsync(email, null, null, AuthLoginAuditConsts.Reasons.UnknownUser, origin, cancellationToken).ConfigureAwait(false);
            return null;
        }

        AppUser user = info.User;
        if (!user.IsActive || !info.Tenant.IsActive)
        {
            await AuditAsync(email, info.Tenant.Id, user.Id, AuthLoginAuditConsts.Reasons.NotActive, origin, cancellationToken).ConfigureAwait(false);
            return null;
        }

        // a locked-out user is refused whatever the password is, and a refused attempt does not lengthen the lockout
        if (user.IsLockedOut(DateTimeOffset.UtcNow))
        {
            await AuditAsync(email, info.Tenant.Id, user.Id, AuthLoginAuditConsts.Reasons.LockedOut, origin, cancellationToken).ConfigureAwait(false);
            return null;
        }

        if (!passwordMatches)
        {
            PasswordRules rules = await policies.GetRulesAsync(info.Tenant.Id, cancellationToken).ConfigureAwait(false);
            await signIn.RecordFailureAsync(user.Id, rules.MaxFailedLoginCount, rules.LockoutMinutes, cancellationToken).ConfigureAwait(false);
            await AuditAsync(email, info.Tenant.Id, user.Id, AuthLoginAuditConsts.Reasons.WrongPassword, origin, cancellationToken).ConfigureAwait(false);
            return null;
        }

        await signIn.RecordSuccessAsync(user.Id, cancellationToken).ConfigureAwait(false);
        await AuditAsync(email, info.Tenant.Id, user.Id, null, origin, cancellationToken).ConfigureAwait(false);
        return await BuildAsync(info, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>The user of a refresh token, with what its token says now (its roles and subscriptions may have changed since it signed in); <see langword="null"/> when it is gone, not active or locked out.</summary>
    public async Task<AuthenticatedUser?> FindForRefreshAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        SignInInfo? info = await signIn.FindByIdAsync(userId, cancellationToken).ConfigureAwait(false);
        return info is null || !info.User.IsActive || !info.Tenant.IsActive || info.User.IsLockedOut(DateTimeOffset.UtcNow)
            ? null
            : await BuildAsync(info, cancellationToken).ConfigureAwait(false);
    }

    private async Task<AuthenticatedUser> BuildAsync(SignInInfo info, CancellationToken cancellationToken)
    {
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

    private Task AuditAsync(string email, Guid? tenantId, Guid? userId, string? failureReason, SignInOrigin origin, CancellationToken cancellationToken)
        => audits.AddAsync(AuthLoginAudit.Record(email, tenantId, userId, failureReason is null, failureReason, origin.IpAddress, origin.UserAgent, DateTimeOffset.UtcNow), cancellationToken);
}
