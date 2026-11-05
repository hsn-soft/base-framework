namespace Hsx.IdentityService.Domain.TenantDomain.Consts;

public static class TenantConsts
{
    public const string TableName = "Tenants";

    public const int TitleMaxLength = 200;

    public const int NameMaxLength = 128;

    /// <summary>The path of a tenant in the tree (<c>TenantAccessPath</c>): thirty levels of 33 characters.</summary>
    public const int NormalizedAccessPathMaxLength = 1000;
}
