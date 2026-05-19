using System.Globalization;
using Hhs.ContentService.Domain.ClientDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ClientDomain.Entities;

public sealed class Client : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; set; }

    public Guid TenantId { get; private set; }

    [CanBeNull]
    public string SubdomainName { get; private set; }

    [NotNull]
    public string DomainName { get; private set; }

    public bool IsBlocked { get; internal set; }

    public ushort DailyDirectVideoGenerationStartedUtcHour { get; internal set; }
    public ushort DailyDirectVideoGenerationLimit { get; internal set; }

    public ushort DailyTrendVideoGenerationStartedUtcHour { get; internal set; }
    public ushort DailyTrendVideoGenerationLimit { get; internal set; }
    public ushort DailyTrendVideoWaitStatisticHour { get; internal set; }
    public ushort DailyTrendVideoMinVisitCount { get; internal set; }

    public ushort DailyAnalysisVideoGenerationStartedUtcHour { get; internal set; }
    public ushort DailyAnalysisVideoGenerationLimit { get; internal set; }

    private List<ClientPathFilter> _pathFilters = [];
    public IReadOnlyCollection<ClientPathFilter> PathFilters => _pathFilters;

    private List<ClientVideoGenerationHistory> _videoGenerationHistories = [];
    public IReadOnlyCollection<ClientVideoGenerationHistory> VideoGenerationHistories => _videoGenerationHistories;

    private Client()
    {
        DomainName = string.Empty;
    }

    internal Client(Guid id, Guid tenantId, [NotNull] string domainName, [CanBeNull] string subdomainName = null) : this()
    {
        Id = id;
        SetTenantId(tenantId);
        SetDomainName(domainName);
        SetSubdomainName(subdomainName);
        IsBlocked = false;
    }

    internal void SetTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(TenantId)} is invalid", nameof(tenantId));
        }

        TenantId = tenantId;
    }

    internal void SetDomainName(string domainName)
    {
        DomainName = Check.NotNull(domainName, nameof(domainName), ClientConsts.DomainNameMaxLength).ToLower(new CultureInfo("en-US"));
    }

    internal void SetSubdomainName(string subdomainName)
    {
        SubdomainName = null;
        if (subdomainName != null)
        {
            SubdomainName = Check.NotNull(subdomainName, nameof(subdomainName), ClientConsts.SubdomainNameMaxLength).ToLower(new CultureInfo("en-US"));
        }
    }

    internal void SetPathFilters(List<ClientPathFilter> pathFilters)
    {
        pathFilters ??= [];
        _pathFilters = pathFilters;
    }
}