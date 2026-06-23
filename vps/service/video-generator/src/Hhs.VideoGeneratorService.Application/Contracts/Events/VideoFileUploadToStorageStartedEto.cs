using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.VideoGeneratorService.Application.Contracts.Events;

public sealed record VideoFileUploadToStorageStartedEto(Guid VideoRequestId) : IIntegrationEventMessage
{
    public Guid VideoRequestId { get; } = VideoRequestId;
}