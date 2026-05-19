namespace Hhs.AdministrationService.Domain.PermissionDomain.Consts;

public static class PermissionGrantConsts
{
    private const string DefaultSorting = "{0}Name asc";

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty);
    }

    public const string TableName = "PermissionGrants";
    public const int NameMaxLength = 128;
    public const int ProviderNameMaxLength = 64;
    public const int ProviderKeyMaxLength = 64;
}