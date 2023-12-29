using System;

namespace HsnSoft.Base.EventBus.Attributes;

public interface IEventNameProvider
{
    string GetName(Type eventType);
}