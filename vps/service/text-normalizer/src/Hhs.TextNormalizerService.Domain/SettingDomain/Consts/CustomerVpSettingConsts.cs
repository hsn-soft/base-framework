using Hhs.TextNormalizerService.Domain.SettingDomain.Entities;

namespace Hhs.TextNormalizerService.Domain.SettingDomain.Consts;

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
    public const int DomainNameMaxLength = 100;
    public const int ContentOutlinePromptMaxLength = 5000;
    public const int AnalysisOutlineContentPromptMaxLength = 5000;
    public const int AnalysisOutlineIntroPromptMaxLength = 5000;
    public const int AnalysisOutlineOutroPromptMaxLength = 5000;
}