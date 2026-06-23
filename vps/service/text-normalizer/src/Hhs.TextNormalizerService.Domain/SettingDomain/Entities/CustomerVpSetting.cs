using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using Hhs.TextNormalizerService.Domain.Enums;
using Hhs.TextNormalizerService.Domain.SettingDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Subscribe;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.TextNormalizerService.Domain.SettingDomain.Entities;

public class CustomerVpSetting : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    public bool IsDeleted { get; internal set; }

    [NotNull] public string ScopeKey { get; private set; }

    [NotNull] public string DomainName { get; private set; }

    public bool IsScrapingOperationActive { get; set; }
    public bool IsOutlineOperationActive { get; set; }
    public TextNormalizeProviderTypes ContentOutlineProvider { get; set; }
    public string ContentOutlinePrompt { get; set; }
    public TextNormalizeProviderTypes AnalysisOutlineProvider { get; set; }
    public string AnalysisOutlineContentPrompt { get; set; }
    public string AnalysisOutlineIntroPrompt { get; set; }
    public string AnalysisOutlineOutroPrompt { get; set; }
    public bool IsForceContentDetailInAnalyseActive { get; set; }

    private CustomerVpSetting()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        DomainName = string.Empty;
    }

    internal CustomerVpSetting(Guid customerId, [NotNull] string domainName) : this(Guid.CreateVersion7(), customerId, domainName)
    {
    }

    internal CustomerVpSetting(Guid id, Guid customerId, [NotNull] string domainName) : this()
    {
        Id = id;

        SetScopeKey(customerId);
        SetDomainName(domainName);
    }

    private void SetScopeKey(Guid customerId)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            ScopeKeyHelper.Generate(customerId, ProductTypes.VideoPlatform),
            $"{nameof(CustomerVpSetting)}:{nameof(ScopeKey)}",
            CustomerVpSettingConsts.ScopeKeyMaxLength
        );

    internal void SetDomainName(string domainName)
    {
        string checkDomainName = LocalizedModelValidator.NotNullOrWhiteSpace(domainName, $"{nameof(CustomerVpSetting)}:{nameof(DomainName)}", CustomerVpSettingConsts.DomainNameMaxLength);
        DomainName = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkDomainName));
    }
}