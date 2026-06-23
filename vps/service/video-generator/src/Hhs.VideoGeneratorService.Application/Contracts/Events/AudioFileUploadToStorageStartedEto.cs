using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.VideoGeneratorService.Application.Contracts.Events;

public sealed record AudioFileUploadToStorageStartedEto(Guid VideoRequestId) : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; } = VideoRequestId;
}