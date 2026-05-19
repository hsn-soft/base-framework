namespace Hhs.IdentityService.Domain.AppRoleDomain.Consts;

public static class AppRoleConsts
{
    private const string DefaultSorting = "{0}Name asc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "AppRoles";
    public const int TenantDomainMaxLength = 20;
    public const int NameMaxLength = 256;
    public const int NormalizedNameMaxLength = 256;
}