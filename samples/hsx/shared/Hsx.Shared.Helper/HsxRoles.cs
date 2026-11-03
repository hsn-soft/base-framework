namespace Hsx.Shared.Helper;

/// <summary>The names of the roles the sample makes: the grants of a role are found by its name (in the scope of its tenant).</summary>
public static class HsxRoles
{
    /// <summary>The role of the people who run the system (the system tenant): it can do everything.</summary>
    public const string SystemAdmin = "SystemAdmin";

    /// <summary>The role of the administrator of a reseller or of an account: it manages what is its tenant's, and sees the tree of tenants without changing it.</summary>
    public const string Admin = "Admin";
}
