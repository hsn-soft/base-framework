using Hhs.ContentService.Domain.ClientDomain.Consts;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ClientDomain.Entities;

public sealed class Client : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; internal set; }

    public Guid TenantId { get; private set; }

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
    public ICollection<ClientPathFilter> PathFilters { get; set; }
    public ICollection<ClientVideoGenerationHistory> VideoGenerationHistories { get; set; }

    private Client()
    {
        // Not-Null string fields
        DomainName = string.Empty;

        // include arrays
        PathFilters = [];
        VideoGenerationHistories = [];
    }

    internal Client(Guid tenantId,
        [NotNull] string domainName
    ) : this(Guid.CreateVersion7(), tenantId, domainName)
    {
    }

    internal Client(Guid id, Guid tenantId,
        [NotNull] string domainName
    ) : this()
    {
        Id = id;
        TenantId = tenantId;
        SetDomainName(domainName);
    }

    internal void SetDomainName(string domainName)
    {
        string checkDomainName = LocalizedModelValidator.NotNullOrWhiteSpace(domainName, $"{nameof(Client)}:{nameof(DomainName)}", ClientConsts.DomainNameMaxLength);
        DomainName = StringOperations.Minimize(StringOperations.ReplaceInvalidChars(checkDomainName));
    }
}