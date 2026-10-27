using Edx.Shared.Helper;

namespace Edx.IdentityService.EntityFrameworkCore;

/// <summary>
/// What the roles of the sample tenants are granted the first time the database is made: a starting point, not a rule. After it every grant is the identity service's data and is changed through its endpoints (a tenant's own
/// administrator changes the grants of its roles); this is never applied again to a database that has grants.
/// </summary>
public static class DefaultPermissionGrants
{
    private const string Megabyte100 = "104857600";
    private const string Megabyte20 = "20971520";

    private static readonly string[] s_identityReads =
    [
        EdxEndpointPermissions.Identity.Users.Get,
        EdxEndpointPermissions.Identity.Users.PagedList,
        EdxEndpointPermissions.Identity.Roles.FilterList,
        EdxEndpointPermissions.Identity.Tenants.FilterList,
        EdxEndpointPermissions.Identity.Permissions.FilterList,
        EdxEndpointPermissions.Identity.RolePermissions.Read,
        EdxEndpointPermissions.Identity.UserPermissions.Read,
    ];

    private static readonly string[] s_identityWrites =
    [
        EdxEndpointPermissions.Identity.RolePermissions.Update,
        EdxEndpointPermissions.Identity.UserPermissions.Update,
    ];

    private static readonly string[] s_intakeReads =
    [
        EdxEndpointPermissions.Intake.Files.Get,
        EdxEndpointPermissions.Intake.Files.PagedList,
        EdxEndpointPermissions.Intake.Files.Download,
    ];

    private static readonly string[] s_intakeUploads =
    [
        EdxEndpointPermissions.Intake.Archives.UploadXml,
        EdxEndpointPermissions.Intake.Archives.UploadTabular,
        EdxEndpointPermissions.Intake.Models.SubmitInvoice,
    ];

    private static readonly string[] s_invoiceReads =
    [
        EdxEndpointPermissions.Invoice.ReceivedFiles.Get,
        EdxEndpointPermissions.Invoice.ReceivedFiles.PagedList,
    ];

    private static readonly string[] s_operations =
    [
        EdxOperationPermissions.Identity.UserEmailView,
        EdxOperationPermissions.Intake.FileStorageLocationView,
    ];

    /// <summary>The grants of a role of a tenant (by the code of the tenant and the name of the role), each as the code and its value (a constraint's; none for the others); nothing for a role that is not named here.</summary>
    public static IReadOnlyList<(string Code, string? Value)> For(string tenantCode, string roleName)
        => (tenantCode, roleName) switch
        {
            (EdxTenants.SystemCode, "admin") => Make(Megabyte100, s_identityReads, s_identityWrites, s_intakeReads, s_intakeUploads, s_invoiceReads, s_operations),
            (EdxTenants.SystemCode, "user") => Make(null, s_identityReads, s_intakeReads, s_invoiceReads, s_operations),
            (EdxTenants.VeribanCode or EdxTenants.BilgeraCode, "admin") => Make(Megabyte100, s_identityReads, s_identityWrites, s_intakeReads, s_intakeUploads, s_invoiceReads, s_operations),
            (EdxTenants.VeribanCode, "operator") => Make(Megabyte20, s_intakeReads, s_intakeUploads, s_invoiceReads, [EdxOperationPermissions.Intake.FileStorageLocationView]),
            (EdxTenants.BilgeraCode, "manager") => Make(null, s_intakeReads, s_invoiceReads, [EdxEndpointPermissions.Identity.Users.Get, EdxEndpointPermissions.Identity.Users.PagedList], s_operations),
            _ => [],
        };

    // the codes of the groups, and the most an upload may weigh when the role is limited
    private static List<(string Code, string? Value)> Make(string? uploadMaxBytes, params string[][] groups)
    {
        List<(string Code, string? Value)> grants = [.. groups.SelectMany(group => group).Select(code => (code, (string?)null))];
        if (uploadMaxBytes is not null)
        {
            grants.Add((EdxConstraintPermissions.Intake.UploadMaxBytes, uploadMaxBytes));
        }

        return grants;
    }
}
