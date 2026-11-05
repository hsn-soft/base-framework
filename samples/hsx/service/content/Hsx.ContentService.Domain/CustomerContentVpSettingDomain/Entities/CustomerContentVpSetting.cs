using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Hsx.ContentService.Domain.CustomerContentVpSettingDomain.Consts;

namespace Hsx.ContentService.Domain.CustomerContentVpSettingDomain.Entities;

/// <summary>
/// The settings of the video platform of a customer: its domain, whether it is blocked and the limits of its daily videos. It is seen by its scope key.
/// </summary>
public sealed class CustomerContentVpSetting : FullAuditedEntity<Guid>, IScopeSubscription
{
    // for EF Core
    private CustomerContentVpSetting()
    {
    }

    public string ScopeKey { get; private set; } = string.Empty;

    public string DomainName { get; private set; } = string.Empty;
    public string NormalizedDomainName { get; private set; } = string.Empty;

    public bool IsBlocked { get; private set; }

    public int DailyDirectVideoGenerationLimit { get; private set; }

    public int DailyTrendVideoGenerationLimit { get; private set; }

    public int DailyAnalysisVideoGenerationLimit { get; private set; }

    public bool IsAnalysisSourcesEnabled { get; private set; }

    public static CustomerContentVpSetting Create(string scopeKey, string domainName, bool isBlocked, int dailyDirectVideoGenerationLimit, int dailyTrendVideoGenerationLimit, int dailyAnalysisVideoGenerationLimit, bool isAnalysisSourcesEnabled)
    {
        CustomerContentVpSetting entity = new()
        {
            Id = Guid.CreateVersion7(),
            ScopeKey = Guard.NotNullOrWhiteSpace(scopeKey, nameof(ScopeKey), CustomerContentVpSettingConsts.ScopeKeyMaxLength).Trim(),
            DomainName = Guard.NotNullOrWhiteSpace(domainName, nameof(DomainName), CustomerContentVpSettingConsts.DomainNameMaxLength).Trim()
        };
        entity.NormalizedDomainName = Guard.NotNullOrWhiteSpace(StringHelper.Normalize(entity.DomainName), nameof(NormalizedDomainName));
        entity.Update(isBlocked, dailyDirectVideoGenerationLimit, dailyTrendVideoGenerationLimit, dailyAnalysisVideoGenerationLimit, isAnalysisSourcesEnabled);
        return entity;
    }

    public void Update(bool isBlocked, int dailyDirectVideoGenerationLimit, int dailyTrendVideoGenerationLimit, int dailyAnalysisVideoGenerationLimit, bool isAnalysisSourcesEnabled)
    {
        IsBlocked = isBlocked;
        DailyDirectVideoGenerationLimit = dailyDirectVideoGenerationLimit;
        DailyTrendVideoGenerationLimit = dailyTrendVideoGenerationLimit;
        DailyAnalysisVideoGenerationLimit = dailyAnalysisVideoGenerationLimit;
        IsAnalysisSourcesEnabled = isAnalysisSourcesEnabled;
    }
}
