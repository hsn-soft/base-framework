namespace Hsx.Shared.Helper;

/// <summary>The names of the roles the sample makes: the grants of a role are found by its name (in the scope of its tenant).</summary>
public static class HsxRoles
{
    /// <summary>The role of the people who run the system (the system tenant): it can do everything.</summary>
    public const string SystemAdmin = "SystemAdmin";

    /// <summary>The role of the administrator of a reseller: it manages what is its tenant's and sees all of its sales.</summary>
    /// <summary>The system tenant's role for the person who runs the day-to-day work: makes resellers, companies, customers and sales and puts demo content in, and nothing else (a System role with a smaller set of grants than <see cref="SystemAdmin"/>).</summary>
    public const string SystemOperator = "SystemOperator";

    public const string Admin = "Admin";
}
