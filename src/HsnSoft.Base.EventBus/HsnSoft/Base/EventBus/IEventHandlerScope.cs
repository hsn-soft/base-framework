using System;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus;

/// <summary>
/// What a package or a product opens around the handling of a message, in the scope the handler runs in: the tenant of the message (<c>HsnSoft.Base.MultiTenancy.EventBus</c>) is the tenant of the handler.
/// Register it as a scoped service; the dispatcher begins every one before the handler runs and disposes it after, each attempt.
/// </summary>
public interface IEventHandlerScope
{
    /// <summary>Starts the scope for the message with <paramref name="envelope"/>; disposing the result ends it.</summary>
    IDisposable Begin(ParentMessageEnvelope envelope);
}
