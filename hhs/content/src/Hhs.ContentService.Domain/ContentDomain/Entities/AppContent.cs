using Hhs.ContentService.Domain.ClientDomain.Entities;
using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.MultiTenancy;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class AppContent : AuditedEntity<Guid>, ISoftDelete, IMultiTenant
{
    public bool IsDeleted { get; internal set; }

    public Guid TenantId { get; private set; }

    public Guid ClientId { get; set; }
    [CanBeNull] public Client Client { get; set; }

    [NotNull] public string SlugKey { get; private set; }

    public AppContentOperationStates OperationStatus { get; set; }

    [CanBeNull] public string OperationStatusDescription { get; set; }

    public Guid? NormalizedRequestId { get; set; }

    public DateTime? ReleaseTime { get; private set; }

    public Guid? VideoRequestId { get; set; }

    [CanBeNull] public string StorageVideoUrl { get; set; }

    [CanBeNull] public string CorrelationId { get; set; }


    private AppContent()
    {
        // Not-Null string fields
        SlugKey = string.Empty;

        // Navigation fields
        Client = null;
    }

    internal AppContent(Guid tenantId, Guid clientId, [NotNull] string slugKey,
        AppContentOperationStates operationStatus, [CanBeNull] string correlationId = null)
        : this(Guid.CreateVersion7(), tenantId, clientId, slugKey, operationStatus, correlationId)
    {
    }

    internal AppContent(Guid id, Guid tenantId, Guid clientId, [NotNull] string slugKey,
        AppContentOperationStates operationStatus, [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        TenantId = tenantId;
        ClientId = clientId;

        SetSlugKey(slugKey);

        OperationStatus = operationStatus;
        CorrelationId = correlationId;
    }

    internal void SetSlugKey(string slugKey)
    {
        string checkSlugKey = LocalizedModelValidator.NotNullOrWhiteSpace(slugKey, $"{nameof(AppContent)}:{nameof(SlugKey)}", AppContentConsts.SlugKeyMaxLength);
        SlugKey = StringOperations.SlugKeyNormalize(checkSlugKey);
    }

    internal void SetReleaseTime(DateTime? releaseTime)
    {
        if (releaseTime.HasValue && releaseTime.Value == default)
        {
            throw new ArgumentException($"{nameof(ReleaseTime)} is invalid", nameof(releaseTime));
        }

        ReleaseTime = releaseTime?.ToUniversalTime();
    }
}