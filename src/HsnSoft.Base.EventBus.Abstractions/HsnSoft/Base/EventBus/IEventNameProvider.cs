using System;

namespace HsnSoft.Base.EventBus;

public interface IEventNameProvider
{
    string GetName(Type eventType);
}