using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus;

namespace Hhs.VideoGeneratorService.Application.Contracts;

public interface IEventManagerAppService : IEventApplicationService
{
    Task EventReQueuedAsync(MessageEnvelope<ReQueuedEto> @event);
}