using Hhs.ContentService.Domain.CustomerDomain.Entities;

namespace Hhs.ContentService.Domain.CustomerDomain.Consts;

public static class CustomerContentSettingConsts
{
    private const string DefaultSorting = "{0}{1} desc";
    private const string DefaultSortingField = nameof(CustomerContentSetting.CreationTime);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "CustomerContentSettings";
    public const int DomainNameMaxLength = 100;
}