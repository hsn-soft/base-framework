using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.Logging;
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
    /// <summary>Set in the <c>Data</c> of an exception whose Error the dispatcher has logged already (<c>CONSUME_EVENT_ERROR</c>).</summary>
    public const string ErrorLoggedKey = "HsnSoft.Base.EventBus.ErrorLogged";

    private static readonly Dictionary<string, object> s_consumeStart = new() { [LogFields.LogType] = LogTypes.ConsumeStart };
    private static readonly Dictionary<string, object> s_consumeEnd = new() { [LogFields.LogType] = LogTypes.ConsumeEnd };

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
        string scopeId = LogFields.NewId();
        string to = trimmedEventName.ToLowerInvariant();
        string from = (envelope.GetHeader(ParentMessageEnvelope.FlowHeader) ?? envelope.Producer ?? "unknown").ToLowerInvariant();

        // every log of the handling carries these: the application logs of the handler too
        Dictionary<string, object?> scopeValues = new()
        {
            [LogFields.CorrelationId] = envelope.CorrelationId,
            [LogFields.ScopeId] = scopeId,
            ["MessageId"] = envelope.MessageId,
            ["ParentMessageId"] = (messageEnvelope as IHasParentMessageId)?.ParentMessageId,
            ["EventName"] = trimmedEventName,
            ["Producer"] = envelope.Producer,
            ["HopLevel"] = envelope.HopLevel,
        };
        if (LogFields.IsTraceId(envelope.CorrelationId))
        {
            // a chain that started as a W3C trace keeps its TraceId across the messages, where no header carries it
            scopeValues["TraceId"] = envelope.CorrelationId;
        }

        using IDisposable? logScope = _logger.BeginScope(scopeValues);
        // what the handler publishes is caused by this step and comes from it
        using IDisposable flow = LogFlow.Begin(envelope.CorrelationId, scopeId, to);
        IReadOnlyList<IntegrationEventHandlerInfo> handlers = [.. _subscriptions.GetHandlersForEvent(trimmedEventName)];
        string handlerNames = string.Join(", ", handlers.Select(handler => handler.HandlerType.Name));
        Stopwatch watch = Stopwatch.StartNew();

        using (_logger.BeginScope(s_consumeStart))
        {
            LogStarted(_logger, from, to);
        }

        try
        {
            foreach (IntegrationEventHandlerInfo handler in handlers)
            {
                await RunHandlerAsync(handler, trimmedEventName, eventType, envelope, messageEnvelope, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException && KnownExceptions.IsKnown(exception))
        {
            // the message asked for something the application says no to (a rule, a missing value): that is how it is handled, not a failure.
            // It ends here as a Warning and is not retried, not reported to the error client; the message is done.
            Dictionary<string, object?> end = new()
            {
                [LogFields.LogType] = LogTypes.ConsumeEnd,
                ["ExceptionType"] = exception.GetType().FullName,
                ["ErrorCode"] = (exception as BusinessException)?.ErrorCode,
            };
            using (_logger.BeginScope(end))
            {
                LogRejected(_logger, from, to, handlerNames, watch.ElapsedMilliseconds, exception.Message);
            }

            return;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // a handling cut short by a shutdown has no end log on purpose: its start log without an end is how an interrupted message is found
            using (_logger.BeginScope(s_consumeEnd))
            {
                LogFailed(_logger, exception, from, to, handlerNames, watch.ElapsedMilliseconds);
            }

            // the broker project reports the failure to the error client; the Error is already written, it must not be written twice
            exception.Data[ErrorLoggedKey] = true;
            throw;
        }

        using (_logger.BeginScope(s_consumeEnd))
        {
            LogHandled(_logger, from, to, handlerNames, watch.ElapsedMilliseconds);
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

                return;
            }
            catch (Exception exception) when (attempt < maxAttempts && exception is not (OperationCanceledException or HandlerNotResolvedException) && !KnownExceptions.IsKnown(exception))
            {
                LogAttemptFailed(_logger, exception, handler.HandlerType.Name, eventName, attempt, maxAttempts);
                await Task.Delay(_options.RetryDelay * attempt, cancellationToken).ConfigureAwait(false);
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

    [LoggerMessage(EventId = 2000, EventName = "CONSUME_EVENT_START", Level = LogLevel.Information, Message = "{From} -> {To}: started")]
    private static partial void LogStarted(ILogger logger, string from, string to);

    [LoggerMessage(EventId = 2001, EventName = "CONSUME_EVENT_SUCCESS", Level = LogLevel.Information, Message = "{From} -> {To}: handled by {Handler} in {ElapsedMs} ms")]
    private static partial void LogHandled(ILogger logger, string from, string to, string handler, long elapsedMs);

    [LoggerMessage(EventId = 2002, EventName = "CONSUME_EVENT_ERROR", Level = LogLevel.Error, Message = "{From} -> {To}: {Handler} failed after {ElapsedMs} ms")]
    private static partial void LogFailed(ILogger logger, Exception exception, string from, string to, string handler, long elapsedMs);

    [LoggerMessage(EventId = 2004, EventName = "CONSUME_EVENT_REJECTED", Level = LogLevel.Warning, Message = "{From} -> {To}: {Handler} rejected the message after {ElapsedMs} ms: {Reason}")]
    private static partial void LogRejected(ILogger logger, string from, string to, string handler, long elapsedMs, string reason);

    [LoggerMessage(EventId = 2003, EventName = "CONSUME_EVENT_RETRY", Level = LogLevel.Warning, Message = "{Handler} failed on {EventName}, attempt {Attempt}/{Max}")]
    private static partial void LogAttemptFailed(ILogger logger, Exception exception, string handler, string eventName, int attempt, int max);

    /// <summary>The container cannot create the handler: a problem of the service, not of the message, so it is not retried.</summary>
    private sealed class HandlerNotResolvedException(Type handlerType, string eventName)
        : InvalidOperationException($"No handler resolved for {handlerType.FullName} (event: {eventName}).");
}
