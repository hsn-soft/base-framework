using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.IdentityService.Domain.IdentityDomain.Security;
using Edx.Shared.Helper;
using HsnSoft.Base.Text;
using Microsoft.Extensions.Options;

namespace Edx.AuthServer.Host.Security;

/// <summary>Who signed in: what goes into the claims of the token.</summary>
public sealed record AuthenticatedUser(Guid Id, string UserName, string DisplayName, string Email, IReadOnlyList<string> Roles, Guid TenantId, bool IsSystemTenant, IReadOnlyList<Guid> AllowedTenantIds);

/// <summary>The settings of the tenants in a token (section <c>Tenants</c>).</summary>
public sealed class TenantTokenOptions
{
    public const string SectionName = "Tenants";

    /// <summary>
    /// How many tenants a token may name besides the tenant of the user (<c>allowed_tenant_id</c>, the tenants under it). A tenant with more under it cannot be signed in to: a token that names a thousand tenants is thirty-six thousand characters, past the size
    /// of the headers a host accepts (<c>Hosting:Kestrel:MaxRequestHeadersTotalSize</c>), and a tenant that has so many is seen by the scope of its data (the scope key), not by a list of ids. Default 50.
    /// </summary>
    public int MaxAllowedPerToken { get; set; } = 50;
}

/// <summary>Checks a tenant, a user name and a password against the users of the identity service (the same database; the auth server only reads it).</summary>
public sealed class UserAuthenticator(IUserRepository users, ITenantRepository tenants, IOptions<TenantTokenOptions> tenantOptions)
{
    // checked when the user does not exist, so a wrong name takes as long as a wrong password
    private static readonly string s_unknownUserHash = PasswordHasher.Hash("not-a-password-of-anyone");

    /// <summary>The user, or <see langword="null"/> when the tenant or the name is unknown, the tenant or the user is not active or the password is wrong (the caller cannot tell which).</summary>
    public async Task<AuthenticatedUser?> AuthenticateAsync(string tenantCode, string userName, string password, CancellationToken cancellationToken = default)
    {
        // the repository compares the normalized columns: what was typed is normalized here (the code of a tenant is the normalized text in small letters)
        SignInInfo? found = await users.FindForSignInAsync(StringHelper.Normalize(tenantCode).ToLowerInvariant(), StringHelper.Normalize(userName), cancellationToken).ConfigureAwait(false);
        bool passwordMatches = PasswordHasher.Verify(found?.User.PasswordHash ?? s_unknownUserHash, password);
        if (found is not { Tenant.IsActive: true, User.IsActive: true } || !passwordMatches)
        {
            return null;
        }

        // the tenants under the tenant of the user are what its users see besides their own; the system tenant sees every tenant, so it has no list
        bool isSystem = found.Tenant.Id == EdxTenants.System;
        IReadOnlyList<Guid> allowed = isSystem ? [] : await tenants.GetDescendantIdsAsync(found.Tenant, cancellationToken).ConfigureAwait(false);
        int limit = tenantOptions.Value.MaxAllowedPerToken;
        if (allowed.Count > limit)
        {
            throw new InvalidOperationException($"The tenant '{found.Tenant.Code}' has {allowed.Count} tenants under it and a token may name {limit} (Tenants:MaxAllowedPerToken): they cannot be listed in a token.");
        }

        return new AuthenticatedUser(found.User.Id, found.User.UserName, found.User.DisplayName, found.User.Email, found.Roles, found.Tenant.Id, isSystem, allowed);
    }
}
