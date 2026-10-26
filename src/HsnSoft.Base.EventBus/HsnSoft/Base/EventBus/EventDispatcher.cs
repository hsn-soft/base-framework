using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.Domain.Entities.Events;
using HsnSoft.Base.EventBus.Inbox;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Masking;
using HsnSoft.Base.Security;
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

/// <summary>How many times the handler that failed was run (the attempts of the last handler), for the log that ends the handling.</summary>
internal sealed class AttemptCounter
{
    public int Count { get; set; }
}

public sealed partial class EventDispatcher(
    IServiceScopeFactory scopeFactory,
    IEventBusSubscriptionManager subscriptions,
    IOptions<EventBusOptions> options,
    ILogger<EventDispatcher> logger,
    ILogMasker? masker = null,
    InboxEnabled? inbox = null) : IEventDispatcher
{
    /// <summary>Set in the <c>Data</c> of an exception whose Error the dispatcher has logged already (<c>CONSUME_EVENT_ERROR</c>).</summary>
    public const string ErrorLoggedKey = "HsnSoft.Base.EventBus.ErrorLogged";

    private static readonly string[] s_clientHeaders = ["ClientChannel", "ClientVersion", "ClientLat", "ClientLong"];

    private static readonly Dictionary<string, object> s_consumeEnd = new() { [LogFields.LogType] = LogTypes.ConsumeEnd };

    private static readonly ConcurrentDictionary<Type, Func<object, object, CancellationToken, Task>> s_invokers = new();

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IEventBusSubscriptionManager _subscriptions = subscriptions;
    private readonly EventBusOptions _options = options.Value;
    private readonly ILogger<EventDispatcher> _logger = logger;
    private readonly ILogMasker? _masker = masker;
    private readonly bool _inbox = inbox is not null;

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
        // who caused it: the caller of the chain, then what a product adds as headers (IEventHeaderProvider)
        if (envelope.UserId is not null)
        {
            scopeValues["UserId"] = envelope.UserId;
        }

        if (envelope.TenantId is not null)
        {
            scopeValues["TenantId"] = envelope.TenantId;
        }

        if (envelope.UserRoles is { Count: > 0 })
        {
            scopeValues["UserRoles"] = LogFields.Text(string.Join(',', envelope.UserRoles), 256);
        }

        foreach (string header in s_clientHeaders)
        {
            if (envelope.GetHeader(header) is { Length: > 0 } value)
            {
                scopeValues[header] = LogFields.Text(value, 256);
            }
        }

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

        using (_logger.BeginScope(ConsumeStartFields(messageEnvelope)))
        {
            LogStarted(_logger, from, to);
        }

        // a service with an inbox handles a message once: a delivery of one that was handled (or is being handled) is left alone
        if (_inbox && !await TryStartInboxAsync(trimmedEventName, envelope, from, to, cancellationToken).ConfigureAwait(false))
        {
            return;
        }

        AttemptCounter attempts = new();
        try
        {
            foreach (IntegrationEventHandlerInfo handler in handlers)
            {
                await RunHandlerAsync(handler, trimmedEventName, eventType, envelope, messageEnvelope, attempts, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException && KnownExceptions.IsKnown(exception))
        {
            // the message asked for something the application says no to (a rule, a missing value): that is how it is handled, not a failure.
            // It ends here as a Warning and is not retried, not reported to the error client; the message is done.
            Dictionary<string, object?> end = new()
            {
                [LogFields.LogType] = LogTypes.ConsumeEnd,
                ["ErrorCode"] = (exception as BusinessException)?.ErrorCode,
            };
            ExceptionLogFields.AddTo(end, exception);
            using (_logger.BeginScope(end))
            {
                LogRejected(_logger, from, to, handlerNames, watch.ElapsedMilliseconds, exception.Message);
            }

            await EndInboxAsync(envelope, error: null, cancellationToken).ConfigureAwait(false);
            return;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // a handling cut short by a shutdown has no end log on purpose: its start log without an end is how an interrupted message is found
            Dictionary<string, object?> failed = new() { [LogFields.LogType] = LogTypes.ConsumeEnd, ["Attempts"] = attempts.Count };
            ExceptionLogFields.AddTo(failed, exception);
            using (_logger.BeginScope(failed))
            {
                LogFailed(_logger, exception, from, to, handlerNames, watch.ElapsedMilliseconds);
            }

            // the broker project reports the failure to the error client; the Error is already written, it must not be written twice
            exception.Data[ErrorLoggedKey] = true;
            await EndInboxAsync(envelope, exception.Message, cancellationToken).ConfigureAwait(false);
            throw;
        }

        await EndInboxAsync(envelope, error: null, cancellationToken).ConfigureAwait(false);
        using (_logger.BeginScope(s_consumeEnd))
        {
            LogHandled(_logger, from, to, handlerNames, watch.ElapsedMilliseconds);
        }
    }

    // false when the message is not to be handled: it was handled before, or another handling holds it, or it failed too often
    private async Task<bool> TryStartInboxAsync(string eventName, ParentMessageEnvelope envelope, string from, string to, CancellationToken cancellationToken)
    {
        AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            InboxStart start = await scope.ServiceProvider.GetRequiredService<IInboxStore>().TryStartAsync(envelope.MessageId, eventName, envelope.CorrelationId, cancellationToken).ConfigureAwait(false);
            switch (start)
            {
                case InboxStart.Started:
                    return true;
                case InboxStart.AlreadyHandled:
                    LogInboxAlreadyHandled(_logger, from, to);
                    break;
                case InboxStart.InProgress:
                    LogInboxInProgress(_logger, from, to);
                    break;
                default:
                    LogInboxRetryLimit(_logger, from, to);
                    break;
            }

            return false;
        }
    }

    // the handling ended: handled (or the application said no) when there is no error, failed with it otherwise; a handling cut short by a shutdown never gets here and stays Started until its lease runs out
    private async Task EndInboxAsync(ParentMessageEnvelope envelope, string? error, CancellationToken cancellationToken)
    {
        if (!_inbox)
        {
            return;
        }

        AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            IInboxStore store = scope.ServiceProvider.GetRequiredService<IInboxStore>();
            await (error is null ? store.CompleteAsync(envelope.MessageId, cancellationToken) : store.FailAsync(envelope.MessageId, error, cancellationToken)).ConfigureAwait(false);
        }
    }

    // the message the handling starts with, with what is sensitive in it hidden by what its classes say
    private Dictionary<string, object?> ConsumeStartFields(object messageEnvelope)
    {
        Dictionary<string, object?> fields = new() { [LogFields.LogType] = LogTypes.ConsumeStart };
        if (_options.LogConsumedMessage && _masker is not null)
        {
            fields["MessageBody"] = LogFields.Text(_masker.MaskText(EventSerializer.ToJson(messageEnvelope), messageEnvelope.GetType()), _options.MaxLoggedMessageLength);
        }

        return fields;
    }

    private async Task RunHandlerAsync(
        IntegrationEventHandlerInfo handler,
        string eventName,
        Type eventType,
        ParentMessageEnvelope envelope,
        object messageEnvelope,
        AttemptCounter attempts,
        CancellationToken cancellationToken)
    {
        int maxAttempts = Math.Max(1, _options.MaxHandlerAttempts);

        for (int attempt = 1; ; attempt++)
        {
            attempts.Count = attempt;
            try
            {
                AsyncServiceScope scope = _scopeFactory.CreateAsyncScope();
                await using (scope.ConfigureAwait(false))
                {
                    object instance = scope.ServiceProvider.GetService(handler.HandlerType)
                                      ?? throw new HandlerNotResolvedException(handler.HandlerType, eventName);

                    // do not run the handler on the thread that delivered the message
                    await Task.Yield();

                    // the handler runs as the caller of the chain (the user and the tenant of whoever started it), so the audit fields and the query filters work as in a request
                    using (EventContext.Use(envelope))
                    using (ActingUser.Use(envelope.HasCaller ? new UserContext(envelope.UserId, envelope.UserName, envelope.UserRoles ?? [], envelope.TenantId, envelope.IsSystemTenant) : null))
                    {
                        await s_invokers.GetOrAdd(eventType, CreateInvoker)(instance, messageEnvelope, cancellationToken).ConfigureAwait(false);
                    }
                }

                return;
            }
            catch (Exception exception) when (attempt < maxAttempts && exception is not (OperationCanceledException or HandlerNotResolvedException) && !KnownExceptions.IsKnown(exception))
            {
                Dictionary<string, object?> retried = [];
                ExceptionLogFields.AddTo(retried, exception);
                using (_logger.BeginScope(retried))
                {
                    LogAttemptFailed(_logger, exception, handler.HandlerType.Name, eventName, attempt, maxAttempts);
                }

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

    [LoggerMessage(EventId = 2005, EventName = "CONSUME_EVENT_INBOX_ALREADY_HANDLED", Level = LogLevel.Information, Message = "{From} -> {To}: the message was handled before, it is left alone")]
    private static partial void LogInboxAlreadyHandled(ILogger logger, string from, string to);

    [LoggerMessage(EventId = 2006, EventName = "CONSUME_EVENT_INBOX_IN_PROGRESS", Level = LogLevel.Warning, Message = "{From} -> {To}: another handling holds the message and its lease has not run out, this delivery is left alone")]
    private static partial void LogInboxInProgress(ILogger logger, string from, string to);

    [LoggerMessage(EventId = 2007, EventName = "CONSUME_EVENT_INBOX_RETRY_LIMIT", Level = LogLevel.Warning, Message = "{From} -> {To}: the message failed more often than the inbox allows, it is left to the error queue")]
    private static partial void LogInboxRetryLimit(ILogger logger, string from, string to);

    [LoggerMessage(EventId = 2003, EventName = "CONSUME_EVENT_RETRY", Level = LogLevel.Warning, Message = "{Handler} failed on {EventName}, attempt {Attempt}/{Max}")]
    private static partial void LogAttemptFailed(ILogger logger, Exception exception, string handler, string eventName, int attempt, int max);

    /// <summary>The container cannot create the handler: a problem of the service, not of the message, so it is not retried.</summary>
    private sealed class HandlerNotResolvedException(Type handlerType, string eventName)
        : InvalidOperationException($"No handler resolved for {handlerType.FullName} (event: {eventName}).");
}
