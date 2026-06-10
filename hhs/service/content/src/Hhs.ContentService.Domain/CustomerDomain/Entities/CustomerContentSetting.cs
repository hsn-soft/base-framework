using Hhs.ContentService.Domain.CustomerDomain.Consts;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.CustomerDomain.Entities;

public sealed class CustomerContentSetting : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; internal set; }

    public Guid TenantId { get; private set; }

    public Guid CustomerId { get; private set; }

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

    // Navigation fields
    public ICollection<CustomerContentSettingPathFilter> PathFilters { get; set; }
    public ICollection<CustomerVideoGenerationHistory> VideoGenerationHistories { get; set; }

    private CustomerContentSetting()
    {
        // Not-Null string fields
        DomainName = string.Empty;

        // include arrays
        PathFilters = [];
        VideoGenerationHistories = [];
    }

    internal CustomerContentSetting(Guid tenantId,
        [NotNull] string domainName
    ) : this(Guid.CreateVersion7(), tenantId, domainName)
    {
    }

    internal CustomerContentSetting(Guid id, Guid tenantId,
        [NotNull] string domainName
    ) : this()
    {
        Id = id;
        TenantId = tenantId;
        SetDomainName(domainName);
    }

    internal void SetDomainName(string domainName)
    {
        string checkDomainName = LocalizedModelValidator.NotNullOrWhiteSpace(domainName, $"{nameof(CustomerContentSetting)}:{nameof(DomainName)}", CustomerContentSettingConsts.DomainNameMaxLength);
        DomainName = StringHelper.Minimize(StringHelper.ReplaceInvalidChars(checkDomainName));
    }
}