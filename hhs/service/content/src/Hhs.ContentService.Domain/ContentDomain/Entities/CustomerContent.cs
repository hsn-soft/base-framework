using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.Enums;
using Hhs.Shared.Helper.Enums;
using Hhs.Shared.Helper.Utils;
using Hhs.Shared.Localization;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities.Auditing;
using HsnSoft.Base.Subscribe;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class CustomerContent : AuditedEntity<Guid>, ISoftDelete, IScopeSubscription
{
    public bool IsDeleted { get; internal set; }

    [NotNull] public string ScopeKey { get; private set; }

    [NotNull] public string SlugKey { get; private set; }

    public CustomerContentOperationStates OperationStatus { get; set; }

    [CanBeNull] public string OperationStatusDescription { get; set; }

    public Guid? NormalizedRequestId { get; set; }

    public DateTime? ReleaseTime { get; private set; }

    public Guid? VideoRequestId { get; set; }

    [CanBeNull] public string StorageVideoUrl { get; set; }

    [CanBeNull] public string CorrelationId { get; set; }


    private CustomerContent()
    {
        // Not-Null string fields
        ScopeKey = string.Empty;
        SlugKey = string.Empty;
    }

    internal CustomerContent(Guid customerId, ProductTypes productType, [NotNull] string slugKey,
        CustomerContentOperationStates operationStatus, [CanBeNull] string correlationId = null)
        : this(Guid.CreateVersion7(), customerId, productType, slugKey, operationStatus, correlationId)
    {
    }

    internal CustomerContent(Guid id, Guid customerId, ProductTypes productType, [NotNull] string slugKey,
        CustomerContentOperationStates operationStatus, [CanBeNull] string correlationId = null) : this()
    {
        Id = id;
        SetScopeKey(customerId, productType);
        SetSlugKey(slugKey);

        OperationStatus = operationStatus;
        CorrelationId = correlationId;
    }

    private void SetScopeKey(Guid customerId, ProductTypes productType)
        => ScopeKey = LocalizedModelValidator.NotNullOrWhiteSpace(
            ScopeKeyHelper.Generate(customerId, productType),
            $"{nameof(CustomerContent)}:{nameof(ScopeKey)}",
            CustomerContentConsts.ScopeKeyMaxLength
        );

    internal void SetSlugKey(string slugKey)
    {
        string checkSlugKey = LocalizedModelValidator.NotNullOrWhiteSpace(slugKey, $"{nameof(CustomerContent)}:{nameof(SlugKey)}", CustomerContentConsts.SlugKeyMaxLength);
        SlugKey = StringHelper.SlugKeyNormalize(checkSlugKey);
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