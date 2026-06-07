using Hhs.ContentService.Domain.ClientDomain.Consts;
using Hhs.ContentService.Domain.Enums;
using Hhs.Shared.Localization;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ClientDomain.Entities;

public sealed class ClientVideoGenerationHistory : Entity<Guid>, IMultiTenant
{
    public Guid TenantId { get; private set; }

    public Guid ClientId { get; set; }
    [CanBeNull] public Client Client { get; set; }

    public DateTime VideoGenerationDate { get; private set; }

    public VideoGenerationTypes VideoGenerationType { get; private set; }

    [NotNull] public string ContentReferenceIds { get; private set; }


    private ClientVideoGenerationHistory()
    {
        // Not-Null string fields
        ContentReferenceIds = string.Empty;

        // Navigation fields
        Client = null;
    }

    internal ClientVideoGenerationHistory(Guid tenantId, Guid clientId, DateTime videoGenerationDate, VideoGenerationTypes videoGenerationType, [NotNull] string contentReferenceIds)
        : this(Guid.CreateVersion7(), tenantId, clientId, videoGenerationDate, videoGenerationType, contentReferenceIds)
    {
    }

    internal ClientVideoGenerationHistory(Guid id, Guid tenantId,
        Guid clientId,
        DateTime videoGenerationDate,
        VideoGenerationTypes videoGenerationType,
        [NotNull] string contentReferenceIds
    ) : this()
    {
        Id = id;
        TenantId = tenantId;
        ClientId = clientId;

        VideoGenerationDate = videoGenerationDate;
        VideoGenerationType = videoGenerationType;
        SetContentReferenceIds(contentReferenceIds);
    }

    internal void SetContentReferenceIds(string contentReferenceIds)
    {
        string checkReferences = LocalizedModelValidator.NotNullOrWhiteSpace(contentReferenceIds, $"{nameof(ClientVideoGenerationHistory)}:{nameof(ContentReferenceIds)}", ClientVideoGenerationHistoryConsts.ContentReferenceIdsNameMaxLength);
        ContentReferenceIds = StringHelper.Minimize(checkReferences);
    }
}