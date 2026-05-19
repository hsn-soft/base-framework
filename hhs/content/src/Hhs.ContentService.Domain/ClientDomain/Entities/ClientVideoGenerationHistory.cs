using Hhs.ContentService.Domain.Enums;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using JetBrains.Annotations;

namespace Hhs.ContentService.Domain.ClientDomain.Entities;

public sealed class ClientVideoGenerationHistory : Entity<Guid>
{
    public Guid ClientId { get; private set; }

    public DateTime VideoGenerationDate { get; private set; }

    public VideoGenerationTypes VideoGenerationType { get; private set; }

    [NotNull]
    public string ContentReferenceIds { get; private set; }

    private ClientVideoGenerationHistory()
    {
        ContentReferenceIds = string.Empty;
    }

    internal ClientVideoGenerationHistory(Guid id, Guid clientId, DateTime videoGenerationDate,VideoGenerationTypes videoGenerationType, [NotNull] string contentReferenceIds) : this()
    {
        Id = id;
        SetClientId(clientId);
        VideoGenerationDate = videoGenerationDate;
        VideoGenerationType = videoGenerationType;
        SetContentReferenceIds(contentReferenceIds);
    }

    internal void SetClientId(Guid clientId)
    {
        if (clientId == Guid.Empty)
        {
            throw new ArgumentException($"{nameof(ClientId)} is invalid", nameof(clientId));
        }

        ClientId = clientId;
    }

    internal void SetContentReferenceIds(string contentReferenceIds)
    {
        ContentReferenceIds = Check.NotNull(contentReferenceIds, nameof(contentReferenceIds));
    }
}