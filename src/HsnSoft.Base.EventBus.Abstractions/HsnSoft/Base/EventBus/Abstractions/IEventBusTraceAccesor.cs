using System;

namespace HsnSoft.Base.EventBus.Abstractions;

public interface IEventBusTraceAccesor
{
    string? GetCorrelationId();

    string? GetUserId();

    string[] GetRoles();
}

public class DefaultEventBusTraceAccesor : IEventBusTraceAccesor
{
    public string GetCorrelationId() => Guid.NewGuid().ToString("N");

    public string GetUserId() => null;

    public string[] GetRoles() => Array.Empty<string>();
}