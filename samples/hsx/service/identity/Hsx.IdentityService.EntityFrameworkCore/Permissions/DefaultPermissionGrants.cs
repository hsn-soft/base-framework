using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.EntityFrameworkCore.Permissions;

/// <summary>
/// What a role starts with when it is made (<c>SampleIdentitySeeder</c>, <c>tenant-management</c>, the roles of a reseller) or when a permission is first added (<c>PermissionDatabase.EnsurePermissionsAsync</c>): a starting point, not a rule. After it every grant is changed through its endpoints
/// (the administrator of a tenant changes the grants of the roles of its own tenant). It follows the **type** of the role: a <c>System</c> role does all of it; a <c>Tenant</c> role (the administrator of a reseller) manages what is its tenant's (users, roles, grants, the tenants under it) and sees
/// its sales and all the data of its products, but writes no tenant, company, customer or sale and has no access to the catalogue; a <c>Company</c> or <c>Customer</c> role only reads the data of its products.
/// </summary>
public static class DefaultPermissionGrants
{
    // what only the system tenant does: the tenants, the catalogue of the companies and the customers, and the sales
    private static readonly HashSet<string> s_systemOnly =
    [
        HsxEndpointPermissions.Identity.Tenants.Create,
        HsxEndpointPermissions.Identity.Tenants.Update,
        HsxEndpointPermissions.Identity.Tenants.Delete,
        HsxEndpointPermissions.Identity.Tenants.Move,
        HsxEndpointPermissions.Identity.TenantManagement.CreateReseller,
        HsxEndpointPermissions.Identity.Companies.Read,
        HsxEndpointPermissions.Identity.Companies.Create,
        HsxEndpointPermissions.Identity.Companies.Update,
        HsxEndpointPermissions.Identity.Companies.Delete,
        HsxEndpointPermissions.Identity.Customers.Read,
        HsxEndpointPermissions.Identity.Customers.Create,
        HsxEndpointPermissions.Identity.Customers.Update,
        HsxEndpointPermissions.Identity.Customers.Delete,
        HsxEndpointPermissions.Identity.Subscriptions.Create,
        HsxEndpointPermissions.Identity.Subscriptions.Update,
        HsxEndpointPermissions.Identity.Subscriptions.Delete,
        HsxEndpointPermissions.Identity.Subscriptions.Transfer,
        HsxPagePermissions.Identity.Companies.View,
        HsxPagePermissions.Identity.Customers.View,
    ];

    // what the users of a company or a customer of a reseller do: they read the data of their products
    private static readonly string[] s_dataReads =
    [
        HsxPagePermissions.Content.CustomerContents.View,
        HsxPagePermissions.Content.AnalysisContents.View,
        HsxPagePermissions.Content.CustomerContentVisits.View,
        HsxPagePermissions.Content.CustomerContentVpSettings.View,
        HsxPagePermissions.TextNormalizer.NormalizeRequests.View,
        HsxPagePermissions.VideoGenerator.VideoRequests.View,
        HsxEndpointPermissions.Content.CustomerContents.Read,
        HsxEndpointPermissions.Content.AnalysisContents.Read,
        HsxEndpointPermissions.Content.AnalysisContentItems.Read,
        HsxEndpointPermissions.Content.CustomerContentVisits.Read,
        HsxEndpointPermissions.Content.CustomerContentVpSettings.Read,
        HsxEndpointPermissions.TextNormalizer.CustomerContentNormalizedRequests.Read,
        HsxEndpointPermissions.VideoGenerator.VideoRequests.Read,
    ];

    // the system operator: the work of selling (resellers, the catalogue, the sales) and the demo content, with what is needed to see the results; no deletes, no grants, no roles of its own
    private static readonly string[] s_systemOperator =
    [
        HsxPagePermissions.Identity.Tenants.View,
        HsxPagePermissions.Identity.Users.View,
        HsxPagePermissions.Identity.Companies.View,
        HsxPagePermissions.Identity.Customers.View,
        HsxPagePermissions.Identity.Subscriptions.View,
        HsxPagePermissions.Content.CustomerContents.View,
        HsxEndpointPermissions.Identity.Tenants.Read,
        HsxEndpointPermissions.Identity.TenantManagement.CreateReseller,
        HsxEndpointPermissions.Identity.AppUsers.Read,
        HsxEndpointPermissions.Identity.AppRoles.Read,
        HsxEndpointPermissions.Identity.Companies.Read,
        HsxEndpointPermissions.Identity.Companies.Create,
        HsxEndpointPermissions.Identity.Customers.Read,
        HsxEndpointPermissions.Identity.Customers.Create,
        HsxEndpointPermissions.Identity.Subscriptions.Read,
        HsxEndpointPermissions.Identity.Subscriptions.Create,
        HsxEndpointPermissions.Identity.Subscriptions.Transfer,
        HsxEndpointPermissions.Content.CustomerContents.Read,
        HsxEndpointPermissions.Content.CustomerContents.Create,
    ];

    /// <summary>The codes the role of this name and type is granted: the system operator has a set of its own, any other role the set of its type.</summary>
    public static IReadOnlyList<string> ForRole(AppRoleType roleType, string roleName)
        => roleType == AppRoleType.System && roleName == HsxRoles.SystemOperator ? s_systemOperator : For(roleType);

    /// <summary>The codes a role of this type is granted.</summary>
    public static IReadOnlyList<string> For(AppRoleType roleType)
        => roleType switch
        {
            AppRoleType.System => PermissionDatabase.CatalogCodes,
            AppRoleType.Tenant => [.. PermissionDatabase.CatalogCodes.Where(code => !s_systemOnly.Contains(code))],
            AppRoleType.Company or AppRoleType.Customer => s_dataReads,
            _ => [],
        };
}
