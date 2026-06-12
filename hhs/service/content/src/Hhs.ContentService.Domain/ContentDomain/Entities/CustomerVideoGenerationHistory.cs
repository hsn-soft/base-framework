using Hhs.ContentService.Domain.ContentDomain.Consts;
using Hhs.ContentService.Domain.Enums;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ContentDomain.Entities;

public sealed class CustomerVideoGenerationHistory : Entity<Guid>, IMultiTenant
{
    public Guid TenantId { get; private set; }

    public Guid CustomerId { get; set; }

    public DateTime VideoGenerationDate { get; private set; }

    public VideoGenerationTypes VideoGenerationType { get; private set; }

    [NotNull] public string ContentReferenceIds { get; private set; }


    private CustomerVideoGenerationHistory()
    {
        // Not-Null string fields
        ContentReferenceIds = string.Empty;
    }

    internal CustomerVideoGenerationHistory(Guid tenantId, Guid customerId, DateTime videoGenerationDate, VideoGenerationTypes videoGenerationType, [NotNull] string contentReferenceIds)
        : this(Guid.CreateVersion7(), tenantId, customerId, videoGenerationDate, videoGenerationType, contentReferenceIds)
    {
    }

    internal CustomerVideoGenerationHistory(Guid id, Guid tenantId,
        Guid customerId,
        DateTime videoGenerationDate,
        VideoGenerationTypes videoGenerationType,
        [NotNull] string contentReferenceIds
    ) : this()
    {
        Id = id;
        TenantId = tenantId;
        CustomerId = customerId;

        VideoGenerationDate = videoGenerationDate;
        VideoGenerationType = videoGenerationType;
        SetContentReferenceIds(contentReferenceIds);
    }

    internal void SetContentReferenceIds(string contentReferenceIds)
    {
        string checkReferences = LocalizedModelValidator.NotNullOrWhiteSpace(contentReferenceIds, $"{nameof(CustomerVideoGenerationHistory)}:{nameof(ContentReferenceIds)}", CustomerVideoGenerationHistoryConsts.ContentReferenceIdsNameMaxLength);
        ContentReferenceIds = StringHelper.Minimize(checkReferences);
    }
}