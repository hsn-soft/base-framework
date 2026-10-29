namespace Edx.Shared.Helper;

/// <summary>
/// The roles every tenant of the sample has, and a user of the same name in each (the sample's four kinds of people): an <c>admin</c> manages the tenant and does all of it, a <c>manager</c> approves and sees, an <c>operator</c> makes and uploads invoices, a <c>user</c> only reads.
/// What each is granted is the starting grants of the identity service (<c>DefaultPermissionGrants</c>); a tenant's administrator changes them.
/// </summary>
public static class EdxRoles
{
    public const string Admin = "admin";
    public const string Manager = "manager";
    public const string Operator = "operator";
    public const string User = "user";

    public static readonly string[] All = [Admin, Manager, Operator, User];
}
