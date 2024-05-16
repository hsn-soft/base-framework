using System;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using JetBrains.Annotations;

namespace HsnSoft.Base.EventBus;

public interface IEventBus
{
    Task PublishAsync<TEventMessage>(
        [NotNull] TEventMessage eventMessage,
        [CanBeNull] ParentMessageEnvelope parentMessage = null,
        bool isExchangeEvent = true,
        bool isReQueuePublish = false
    ) where TEventMessage : IIntegrationEventMessage;

    void Subscribe<TEvent, THandler>()
        where TEvent : IIntegrationEventMessage
        where THandler : IIntegrationEventHandler<TEvent>;

    void Subscribe(Type eventType, Type eventHandlerType);
}