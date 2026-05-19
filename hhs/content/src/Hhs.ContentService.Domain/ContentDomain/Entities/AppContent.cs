using System.Globalization;
using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class AppContent : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; set; }

    public Guid TenantId { get; private set; }

    public Guid ClientId { get; private set; }

    [CanBeNull]
    public Client Client { get; } = null;

    [NotNull]
    public string SlugKey { get; private set; }

    public AppContentOperationStates OperationStatus { get; internal set; }

    [CanBeNull]
    public string OperationStatusDescription { get; internal set; }

    public Guid? NormalizedRequestId { get; private set; }

    public DateTime? ReleaseTime { get; private set; }

    public Guid? VideoRequestId { get; private set; }

    [CanBeNull]
    public string StorageVideoUrl { get; internal set; }

    [CanBeNull]
    public string CorrelationId { get; internal set; }

    private AppContent()
    {
        SlugKey = string.Empty;
    }

    internal AppContent(Guid id, Guid tenantId, Guid clientId, [NotNull] string slugKey,
        AppContentOperationStates operationStatus, [CanBeNull] string operationStatusDescription = null,
        Guid? normalizedRequestId = null, DateTime? releaseTime = null,
        Guid? videoRequestId = null, [CanBeNull] string storageVideoUrl = null, [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        SetTenantId(tenantId);
        SetClientId(clientId);
        SetSlugKey(slugKey);

        OperationStatus = operationStatus;
        OperationStatusDescription = operationStatusDescription;

        SetNormalizedRequestId(normalizedRequestId);
        SetReleaseTime(releaseTime);

        SetVideoRequestId(videoRequestId);
        StorageVideoUrl = storageVideoUrl;
        CorrelationId = correlationId;
    }

    internal void SetTenantId(Guid tenantId)
    {
        if (tenantId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(TenantId)} is invalid", nameof(tenantId));
        }

        TenantId = tenantId;
    }

    internal void SetClientId(Guid clientId)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(ClientId)} is invalid", nameof(clientId));
        }

        ClientId = clientId;
    }

    internal void SetSlugKey(string slugKey)
    {
        SlugKey = Check.NotNull(slugKey, nameof(slugKey), AppContentConsts.SlugKeyMaxLength).ToLower(new CultureInfo("en-US"));
    }

    internal void SetReleaseTime(DateTime? releaseTime)
    {
        if (releaseTime.HasValue && releaseTime.Value == default)
        {
            throw new ArgumentException($"{nameof(ReleaseTime)} is invalid", nameof(releaseTime));
        }

        ReleaseTime = releaseTime?.ToUniversalTime();
    }

    internal void SetNormalizedRequestId(Guid? normalizedRequestId)
    {
        if (normalizedRequestId.HasValue && normalizedRequestId.Value == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(NormalizedRequestId)} is invalid", nameof(normalizedRequestId));
        }

        NormalizedRequestId = normalizedRequestId;
    }

    internal void SetVideoRequestId(Guid? videoRequestId)
    {
        if (videoRequestId.HasValue && videoRequestId.Value == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(VideoRequestId)} is invalid", nameof(videoRequestId));
        }

        VideoRequestId = videoRequestId;
    }
}