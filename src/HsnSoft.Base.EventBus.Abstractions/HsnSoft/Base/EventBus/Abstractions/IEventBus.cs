using System;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus.Abstractions;

public interface IEventBus
{
    Task PublishAsync(IntegrationEvent @event);

    void Subscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;

    void Subscribe(Type eventType, Type eventHandlerType);

    void Unsubscribe<TEvent, THandler>()
        where TEvent : IntegrationEvent
        where THandler : IIntegrationEventHandler<TEvent>;
}