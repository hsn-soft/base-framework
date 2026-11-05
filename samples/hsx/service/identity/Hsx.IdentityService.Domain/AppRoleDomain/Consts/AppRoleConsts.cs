namespace Hsx.IdentityService.Domain.AppRoleDomain.Consts;

public static class AppRoleConsts
{
    public const string TableName = "AppRoles";

    public const int NameMaxLength = 64;

    /// <summary>The role of the people who run the system (the system tenant): it can do everything.</summary>
    public const string SystemAdmin = "system-admin";

    /// <summary>The system tenant's role for the day-to-day work (resellers, companies, customers, sales): a System role with a smaller set of grants.</summary>
    public const string SystemOperator = "system-operator";

    /// <summary>The system tenant's role of a person who only has a login: a System role that starts with no grants at all (what it may do is given to it on purpose).</summary>
    public const string SystemUser = "system-user";

    /// <summary>The role of the administrator of a reseller: it manages what is its tenant's and sees all of its sales.</summary>
    public const string TenantAdmin = "tenant-admin";
}
