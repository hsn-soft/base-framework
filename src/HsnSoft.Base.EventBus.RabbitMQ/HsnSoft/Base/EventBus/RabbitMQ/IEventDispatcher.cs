using System;
using System.Threading.Tasks;

namespace HsnSoft.Base.EventBus.RabbitMQ;

public interface IEventDispatcher
{
    /// <summary>
    /// Resolves and invokes every handler registered (in THIS microservice's own subscription manager)
    /// for <paramref name="trimmedEventName"/>. A handler that cannot be resolved via DI is treated as a
    /// framework-level dispatch failure and throws. A handler that resolves but throws during its own
    /// execution is logged and then rethrown too — any exception reaching this point represents a
    /// genuinely unhandled failure (a microservice's own intentional retry/swallow logic never throws
    /// in the first place), so the caller can report it via FailedEto and the event is never lost.
    /// </summary>
    /// <param name="trimmedEventName">Event name with prefix/suffix already trimmed (e.g. "CustomerContentCreated").</param>
    /// <param name="eventType">The bare event message type (e.g. typeof(CustomerContentCreatedEto)).</param>
    /// <param name="messageEnvelope">An already-constructed MessageEnvelope&lt;TEventType&gt; instance.</param>
    Task DispatchAsync(string trimmedEventName, Type eventType, object messageEnvelope);
}
