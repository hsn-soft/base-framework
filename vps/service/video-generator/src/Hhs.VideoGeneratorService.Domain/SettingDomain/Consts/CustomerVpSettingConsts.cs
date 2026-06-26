using Hhs.VideoGeneratorService.Domain.SettingDomain.Entities;

namespace Hhs.VideoGeneratorService.Domain.SettingDomain.Consts;

public static class CustomerVpSettingConsts
{
    private const string DefaultSorting = "{0}{1} desc";
    private const string DefaultSortingField = nameof(CustomerVpSetting.CreationTime);

    public static string GetDefaultSorting(bool withEntityName = false)
    {
        return string.Format(DefaultSorting, withEntityName ? $"{TableName}." : string.Empty, DefaultSortingField);
    }

    public const string TableName = "CustomerVpSettings";
    public const int ScopeKeyMaxLength = 128;
    public const int AudioProviderKeyMaxLength = 50;
    public const int VideoProviderKeyMaxLength = 50;
    public const int CustomerZoneNameMaxLength = 256;
    public const int CustomerBucketKeyMaxLength = 256;
    public const int CustomerBucketSecretMaxLength = 512;
}