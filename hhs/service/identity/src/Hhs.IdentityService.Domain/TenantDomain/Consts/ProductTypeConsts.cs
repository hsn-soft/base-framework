using Hhs.IdentityService.Domain.TenantDomain.Entities;

namespace Hhs.IdentityService.Domain.TenantDomain.Consts;

public static class ProductTypeConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(ProductType.Name);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty,DefaultSortingField);
    }

    public const string TableName = "ProductTypes";
    public const int CodeMaxLength = 32;
    public const int NameMaxLength = 128;
}