using Hhs.AdministrationService.Domain.PermissionDomain.Entities;

namespace Hhs.AdministrationService.Domain.PermissionDomain.Consts;

public static class PermissionGrantConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(PermissionGrant.Id);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty,DefaultSortingField);
    }

    public const string TableName = "PermissionGrants";
    public const int NameMaxLength = 128;
    public const int ProviderNameMaxLength = 64;
    public const int ProviderKeyMaxLength = 64;
}