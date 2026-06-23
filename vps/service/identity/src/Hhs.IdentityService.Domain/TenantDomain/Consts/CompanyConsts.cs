using Hhs.IdentityService.Domain.TenantDomain.Entities;

namespace Hhs.IdentityService.Domain.TenantDomain.Consts;

public static class CompanyConsts
{
    private const string DefaultSorting = "{0}{1} asc";
    private const string DefaultSortingField = nameof(Company.Name);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty,DefaultSortingField);
    }

    public const string TableName = "Companies";
    public const int TitleMaxLength = 250;
    public const int NameMaxLength = 100;
}