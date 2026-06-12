using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.Domain.SettingDomain.Consts;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Subscribe;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.SettingDomain.Entities;

public sealed class CustomerVpSetting : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    public bool IsDeleted { get; internal set; }

    [NotNull] public string ScopeKey { get; private set; }

    [NotNull] public string DomainName { get; private set; }

    public bool IsBlocked { get; set; }

    public ushort DailyDirectVideoGenerationStartedUtcHour { get; set; }
    public ushort DailyDirectVideoGenerationLimit { get; set; }

    public ushort DailyTrendVideoGenerationStartedUtcHour { get; set; }
    public ushort DailyTrendVideoGenerationLimit { get; set; }
    public ushort DailyTrendVideoWaitStatisticHour { get; set; }
    public ushort DailyTrendVideoMinVisitCount { get; set; }

    public ushort DailyAnalysisVideoGenerationStartedUtcHour { get; set; }
    public ushort DailyAnalysisVideoGenerationLimit { get; set; }

    public List<string> IncludePathFilters { get; set; }
    public List<string> ExcludePathFilters { get; set; }

    // Navigation fields
    public ICollection<ContentVideoGenerationLimit> VideoGenerationHistories { get; set; }

    private CustomerVpSetting()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        DomainName = string.Empty;

        // Json property fields
        IncludePathFilters = [];
        ExcludePathFilters = [];

        // Navigation fields
        VideoGenerationHistories = [];
    }

    internal CustomerVpSetting(Guid customerId,
        [NotNull] string domainName,
        [CanBeNull] List<string> includePathFilters = null,
        [CanBeNull] List<string> excludePathFilters = null
    ) : this(Guid.CreateVersion7(), customerId, domainName, includePathFilters, excludePathFilters)
    {
    }

    internal CustomerVpSetting(Guid id, Guid customerId,
        [NotNull] string domainName,
        [CanBeNull] List<string> includePathFilters = null,
        [CanBeNull] List<string> excludePathFilters = null
    ) : this()
    {
        Id = id;

        SetScopeKey(customerId);
        SetDomainName(domainName);

        IncludePathFilters = includePathFilters ?? [];
        ExcludePathFilters = excludePathFilters ?? [];
    }

    private void SetScopeKey(Guid customerId)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            ScopeKeyHelper.Generate(customerId, ProductTypes.VideoPlatform),
            $"{nameof(CustomerContent)}:{nameof(ScopeKey)}",
            CustomerVpSettingConsts.ScopeKeyMaxLength
        );

    internal void SetDomainName(string domainName)
    {
        string checkDomainName = LocalizedModelValidator.NotNullOrWhiteSpace(domainName, $"{nameof(CustomerVpSetting)}:{nameof(DomainName)}", CustomerVpSettingConsts.DomainNameMaxLength);
        DomainName = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkDomainName));
    }
}