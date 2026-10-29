namespace Edx.Shared.Helper;

/// <summary>
/// The scope the grants are kept in (<c>IPermissionScopeProvider</c>): the roles of a tenant are its own, so a role is looked for in its tenant. The identity service writes the grants under this scope and every other service reads them under it, so
/// both name it here.
/// </summary>
public static class EdxPermissionScopes
{
    public static string ForTenant(Guid tenantId) => tenantId.ToString("N");
}
