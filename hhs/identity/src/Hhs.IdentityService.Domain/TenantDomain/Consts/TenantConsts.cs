using Hhs.IdentityService.Domain.TenantDomain.Entities;

namespace Hhs.IdentityService.Domain.TenantDomain.Consts;

public static class TenantConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(Tenant.Name);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty,DefaultSortingField);
    }

    public const string TableName = "Tenants";
    public const int TitleMaxLength = 200;
    public const int NameMaxLength = 100;
    public const int NormalizedAccessPathMaxLength = 1000;
}
