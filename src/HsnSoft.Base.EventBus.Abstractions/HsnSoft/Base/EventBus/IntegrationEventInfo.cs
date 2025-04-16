using System;

namespace HsnSoft.Base.EventBus;

public class IntegrationEventInfo
{
    public Type EventType { get; }
    public ushort FetchCount { get; }

    private IntegrationEventInfo(Type integrationEventType)
    {
        EventType = integrationEventType ?? throw new ArgumentNullException(nameof(integrationEventType));
        FetchCount = 1;
    }

    private IntegrationEventInfo(Type integrationEventType, ushort fetchCount) : this(integrationEventType)
    {
        FetchCount = fetchCount;
    }

    public static IntegrationEventInfo Typed(Type handlerType, ushort fetchCount = 1) => new(handlerType, fetchCount);
}