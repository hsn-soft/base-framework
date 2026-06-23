using Hhs.IdentityService.Domain.TenantDomain.Entities;

namespace Hhs.IdentityService.Domain.TenantDomain.Consts;

public static class CustomerConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(Customer.Domain);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty,DefaultSortingField);
    }

    public const string TableName = "Customers";
    public const int DomainMaxLength = 256;
}