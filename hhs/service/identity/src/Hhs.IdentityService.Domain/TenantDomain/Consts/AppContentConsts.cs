namespace Hhs.IdentityService.Domain.TenantDomain.Consts;

public static class AppContentConsts
{
    private const string DefaultSorting = "{0}CreationTime desc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "AppContents";
    public const int SlugKeyMaxLength = 500;
}