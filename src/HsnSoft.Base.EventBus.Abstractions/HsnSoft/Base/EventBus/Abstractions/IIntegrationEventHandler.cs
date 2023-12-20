using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus.Abstractions;

public interface IIntegrationEventHandler<in TEvent> : IIntegrationEventHandler
    where TEvent : IntegrationEvent
{
    Task HandleAsync(TEvent @event);
}

public interface IIntegrationEventHandler
{
}