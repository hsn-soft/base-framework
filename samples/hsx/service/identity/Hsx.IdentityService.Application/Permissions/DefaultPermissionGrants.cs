using System.Collections.ObjectModel;
using Hsx.IdentityService.Domain.AppRoleDomain.Consts;
using Hsx.IdentityService.Domain.Enums;
using Hsx.Shared.Contracts.Permissions;

namespace Hsx.IdentityService.Application.Permissions;

/// <summary>
/// What a role starts with when it is made (the seeders, the provisioning of a reseller) or when a permission is first added (the permissions seeder): a starting point, not a rule. After it every grant is changed through its
/// endpoints (the administrator of a tenant changes the grants of the roles of its own tenant). It follows the **type** of the role: a <c>System</c> role does all of it; a <c>Tenant</c> role (the administrator of a reseller) manages what is its tenant's (users, roles,
/// grants, the tenants under it) and sees its sales and all the data of its products, but writes no tenant, company, customer or sale and has no access to the catalogue; a <c>Company</c> or <c>Customer</c> role only reads the data of its products.
/// A constraint is granted with a value (<see cref="ConstraintsFor"/>): a role with none is not allowed what the constraint limits.
/// </summary>
public static class DefaultPermissionGrants
{
    // what only the system tenant does: the tenants, the catalogue of the companies and the customers, and the sales
    private static readonly HashSet<string> s_systemOnly =
    [
        IdentityPermissions.Endpoints.Tenants.Create,
        IdentityPermissions.Endpoints.Tenants.Update,
        IdentityPermissions.Endpoints.Tenants.Delete,
        IdentityPermissions.Endpoints.Tenants.Move,
        IdentityPermissions.Endpoints.TenantManagement.CreateReseller,
        IdentityPermissions.Endpoints.Companies.Read,
        IdentityPermissions.Endpoints.Companies.Create,
        IdentityPermissions.Endpoints.Companies.Update,
        IdentityPermissions.Endpoints.Companies.Delete,
        IdentityPermissions.Endpoints.Customers.Read,
        IdentityPermissions.Endpoints.Customers.Create,
        IdentityPermissions.Endpoints.Customers.Update,
        IdentityPermissions.Endpoints.Customers.Delete,
        IdentityPermissions.Endpoints.Subscriptions.Create,
        IdentityPermissions.Endpoints.Subscriptions.Update,
        IdentityPermissions.Endpoints.Subscriptions.Delete,
        IdentityPermissions.Endpoints.Subscriptions.Transfer,
        IdentityPermissions.Pages.Companies.View,
        IdentityPermissions.Pages.Customers.View,
    ];

    // what the users of a company or a customer of a reseller do: they read the data of their products
    private static readonly string[] s_dataReads =
    [
        ContentPermissions.Pages.CustomerContents.View,
        ContentPermissions.Pages.AnalysisContents.View,
        ContentPermissions.Pages.CustomerContentVisits.View,
        ContentPermissions.Pages.CustomerContentVpSettings.View,
        TextNormalizerPermissions.Pages.NormalizeRequests.View,
        VideoGeneratorPermissions.Pages.VideoRequests.View,
        ContentPermissions.Endpoints.CustomerContents.Read,
        ContentPermissions.Endpoints.AnalysisContents.Read,
        ContentPermissions.Endpoints.AnalysisContentItems.Read,
        ContentPermissions.Endpoints.CustomerContentVisits.Read,
        ContentPermissions.Endpoints.CustomerContentVpSettings.Read,
        TextNormalizerPermissions.Endpoints.CustomerContentNormalizedRequests.Read,
        VideoGeneratorPermissions.Endpoints.VideoRequests.Read,
    ];

    // the system operator: the work of selling (resellers, the catalogue, the sales) and the demo content, with what is needed to see the results; no deletes, no grants, no roles of its own
    private static readonly string[] s_systemOperator =
    [
        IdentityPermissions.Pages.Tenants.View,
        IdentityPermissions.Pages.Users.View,
        IdentityPermissions.Pages.Companies.View,
        IdentityPermissions.Pages.Customers.View,
        IdentityPermissions.Pages.Subscriptions.View,
        ContentPermissions.Pages.CustomerContents.View,
        IdentityPermissions.Endpoints.Tenants.Read,
        IdentityPermissions.Endpoints.TenantManagement.CreateReseller,
        IdentityPermissions.Endpoints.AppUsers.Read,
        IdentityPermissions.Endpoints.AppRoles.Read,
        IdentityPermissions.Endpoints.Companies.Read,
        IdentityPermissions.Endpoints.Companies.Create,
        IdentityPermissions.Endpoints.Customers.Read,
        IdentityPermissions.Endpoints.Customers.Create,
        IdentityPermissions.Endpoints.Subscriptions.Read,
        IdentityPermissions.Endpoints.Subscriptions.Create,
        IdentityPermissions.Endpoints.Subscriptions.Transfer,
        ContentPermissions.Endpoints.CustomerContents.Read,
        ContentPermissions.Endpoints.CustomerContents.Create,
    ];

    // the limits (value of the constraint) a role of this type starts with: the days of the audit log it may read
    private static readonly Dictionary<string, string> s_systemConstraints = new() { [IdentityPermissions.Constraints.AuditLogs.MaxDays] = "365" };

    private static readonly Dictionary<string, string> s_tenantConstraints = new() { [IdentityPermissions.Constraints.AuditLogs.MaxDays] = "90" };

    /// <summary>The codes (of the permissions that carry no value) the role of this name and type is granted: the system operator has a set of its own, the system user has none, any other role the set of its type.</summary>
    public static IReadOnlyList<string> ForRole(AppRoleType roleType, string roleName)
        => roleType == AppRoleType.System
            ? roleName switch
            {
                AppRoleConsts.SystemOperator => s_systemOperator,
                AppRoleConsts.SystemUser => [],
                _ => For(roleType),
            }
            : For(roleType);

    /// <summary>The codes (of the permissions that carry no value) a role of this type is granted.</summary>
    public static IReadOnlyList<string> For(AppRoleType roleType)
        => roleType switch
        {
            AppRoleType.System => [.. HsxPermissionCatalog.Codes.Where(code => !IsConstraint(code))],
            AppRoleType.Tenant => [.. HsxPermissionCatalog.Codes.Where(code => !IsConstraint(code) && !s_systemOnly.Contains(code))],
            AppRoleType.Company or AppRoleType.Customer => s_dataReads,
            _ => [],
        };

    /// <summary>The constraints (code and value) the role of this name and type starts with. The system operator and the system user have none: they do not read the audit log.</summary>
    public static IReadOnlyDictionary<string, string> ConstraintsFor(AppRoleType roleType, string roleName)
        => roleType switch
        {
            AppRoleType.System when roleName is not (AppRoleConsts.SystemOperator or AppRoleConsts.SystemUser) => s_systemConstraints,
            AppRoleType.Tenant => s_tenantConstraints,
            _ => ReadOnlyDictionary<string, string>.Empty,
        };

    private static bool IsConstraint(string code) => code.StartsWith("constraint.", StringComparison.Ordinal);
}
