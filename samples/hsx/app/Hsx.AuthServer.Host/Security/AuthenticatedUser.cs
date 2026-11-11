namespace Hsx.AuthServer.Security;

/// <summary>Who signed in: what goes into the claims of the token. <paramref name="AllowedTenantIds"/> are the tenants under the tenant of the user; <paramref name="ScopeKeys"/> are the keys of the subscriptions of its roles; <paramref name="PasswordChangeRequired"/> is a user that still has the temporary password it was given.</summary>
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
    IReadOnlyList<string> ScopeKeys,
    bool PasswordChangeRequired);
