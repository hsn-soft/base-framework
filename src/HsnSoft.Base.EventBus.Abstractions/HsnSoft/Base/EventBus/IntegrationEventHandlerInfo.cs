using System;

namespace HsnSoft.Base.EventBus;

public class IntegrationEventHandlerInfo
{
    public Type HandlerType { get; }

    private IntegrationEventHandlerInfo(Type handlerType)
    {
        HandlerType = handlerType ?? throw new ArgumentNullException(nameof(handlerType));
    }

    public static IntegrationEventHandlerInfo Typed(Type handlerType) => new(handlerType);
}