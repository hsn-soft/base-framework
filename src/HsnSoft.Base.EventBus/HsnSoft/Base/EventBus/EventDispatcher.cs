using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus;

/// <summary>The broker-independent half of consuming: find the handler, run it in its own scope, retry on failure, log how it went.</summary>
public interface IEventDispatcher
{
    /// <summary>
    /// Runs the handler of <paramref name="trimmedEventName"/> with an already deserialized envelope. A handler that cannot be resolved
    /// from the container, and a handler that still fails after all attempts, throw: the broker project reports the failure (see
    /// <see cref="FailedEto"/>) so the message is never lost.
    /// </summary>
    /// <param name="trimmedEventName">The event name without prefix and suffix (e.g. "OrderPlaced").</param>
    /// <param name="eventType">The type of the message inside the envelope.</param>
    /// <param name="messageEnvelope">A <c>MessageEnvelope&lt;eventType&gt;</c>.</param>
    /// <param name="cancellationToken">Cancels the handler.</param>
    Task DispatchAsync(string trimmedEventName, Type eventType, object messageEnvelope, CancellationToken cancellationToken = default);
}

public sealed partial class EventDispatcher(
    IServiceScopeFactory scopeFactory,
    IEventBusSubscriptionManager subscriptions,
    IOptions<EventBusOptions> options,
    ILogger<EventDispatcher> logger) : IEventDispatcher
{
    private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task>> s_invokers = new();

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IEventBusSubscriptionManager _subscriptions = subscriptions;
    private readonly EventBusOptions _options = options.Value;
    private readonly ILogger<EventDispatcher> _logger = logger;

    public async Task DispatchAsync(string trimmedEventName, Type eventType, object messageEnvelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(trimmedEventName);
        ArgumentNullException.ThrowIfNull(eventType);
        ArgumentNullException.ThrowIfNull(messageEnvelope);

        ParentMessageEnvelope envelope = (ParentMessageEnvelope)messageEnvelope;
        Dictionary<string, object?> scopeValues = new()
        {
            ["CorrelationId"] = envelope.CorrelationId,
            ["MessageId"] = envelope.MessageId,
            ["EventName"] = trimmedEventName,
            ["Producer"] = envelope.Producer,
            ["HopLevel"] = envelope.HopLevel,
        };

        using IDisposable? logScope = _logger.BeginScope(scopeValues);
        foreach (IntegrationEventHandlerInfo handler in _subscriptions.GetHandlersForEvent(trimmedEventName))
        {
            await RunHandlerAsync(handler, trimmedEventName, eventType, envelope, messageEnvelope, cancellationToken).ConfigureAwait(false);
        }
    }

    private async Task RunHandlerAsync(
        IntegrationEventHandlerInfo handler,
        string eventName,
        Type eventType,
        ParentMessageEnvelope envelope,
        object messageEnvelope,
        CancellationToken cancellationToken)
    {
        int maxAttempts = Math.Max(1, _options.MaxHandlerAttempts);
        Stopwatch watch = Stopwatch.StartNew();

        for (int attempt = 1; ; attempt++)
        {
            try
            {
                AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                await using (scope.ConfigureAwait(false))
                {
                    object instance = scope.ServiceProvider.GetService(handler.HandlerType)
                                      ?? throw new HandlerNotResolvedException(handler.HandlerType, eventName);

                    // do not run the handler on the thread that delivered the message
                    await Task.Yield();
                    using (EventContext.Use(envelope))
                    {
                        await s_invokers.GetOrAdd(eventType, CreateInvoker)(instance, messageEnvelope, cancellationToken).ConfigureAwait(false);
                    }
                }

                LogHandled(_logger, handler.HandlerType.Name, eventName, watch.ElapsedMilliseconds);
                return;
            }
            catch (Exception exception) when (attempt < maxAttempts && exception is not (OperationCanceledException or HandlerNotResolvedException))
            {
                LogAttemptFailed(_logger, exception, handler.HandlerType.Name, eventName, attempt, maxAttempts);
                await Task.Delay(_options.RetryDelay * attempt, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogFailed(_logger, exception, handler.HandlerType.Name, eventName, watch.ElapsedMilliseconds);
                throw;
            }
        }
    }

    private static Func<object, object, CancellationToken, Task> CreateInvoker(Type eventType)
        => typeof(EventDispatcher)
            .GetMethod(nameof(Invoke), BindingFlags.NonPublic | BindingFlags.Static)!
            .MakeGenericMethod(eventType)
            .CreateDelegate<Func<object, object, CancellationToken, Task>>();

    private static Task Invoke<TEvent>(object handler, object envelope, CancellationToken cancellationToken)
        where TEvent : IIntegrationEventMessage
        => ((IIntegrationEventHandler<TEvent>)handler).HandleAsync((MessageEnvelope<TEvent>)envelope, cancellationToken);

    [LoggerMessage(EventId = 2000, EventName = "CONSUME_EVENT_SUCCESS", Level = LogLevel.Information, Message = "{Handler} handled {EventName} in {ElapsedMs} ms")]
    private static partial void LogHandled(ILogger logger, string handler, string eventName, long elapsedMs);

    [LoggerMessage(EventId = 2001, EventName = "CONSUME_EVENT_RETRY", Level = LogLevel.Warning, Message = "{Handler} failed on {EventName}, attempt {Attempt}/{Max}")]
    private static partial void LogAttemptFailed(ILogger logger, Exception exception, string handler, string eventName, int attempt, int max);

    [LoggerMessage(EventId = 2002, EventName = "CONSUME_EVENT_ERROR", Level = LogLevel.Error, Message = "{Handler} failed on {EventName} after {ElapsedMs} ms")]
    private static partial void LogFailed(ILogger logger, Exception exception, string handler, string eventName, long elapsedMs);

    /// <summary>The container cannot create the handler: a problem of the service, not of the message, so it is not retried.</summary>
    private sealed class HandlerNotResolvedException(Type handlerType, string eventName)
        : InvalidOperationException($"No handler resolved for {handlerType.FullName} (event: {eventName}).");
}
