using System;
using System.Collections.Generic;
using System.Diagnostics.Tracing;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace HsnSoft.Base.EventBus.RabbitMQ;

/// <summary>
/// Passes what the RabbitMQ client library reports about itself (it only writes to an <see cref="EventSource"/>, never to
/// <see cref="ILogger"/>) on to the logging of the application: its information as Debug (so it stays in the console), its warnings
/// and errors as they are. Without it, a connection the client gives up on would leave no trace in Graylog.
/// </summary>
internal sealed partial class RabbitMqClientLogBridge : EventListener
{
    internal const string ClientEventSourceName = "rabbitmq-client";

    private const int WarnEventId = 2;
    private const int ErrorEventId = 3;

    private readonly List<EventSource> _foundBeforeReady = [];
    private readonly Func<EventSource, bool> _isClientSource;
    private readonly ILogger? _logger;

    public RabbitMqClientLogBridge(ILogger logger)
        : this(logger, source => source.Name == ClientEventSourceName)
    {
    }

    internal RabbitMqClientLogBridge(ILogger logger, Func<EventSource, bool> isClientSource)
    {
        _isClientSource = isClientSource;
        List<EventSource> found;
        lock (_foundBeforeReady)
        {
            // the base constructor already announced the sources that exist: they could not be enabled without the logger
            _logger = logger;
            found = [.. _foundBeforeReady];
            _foundBeforeReady.Clear();
        }

        foreach (EventSource source in found)
        {
            Enable(source);
        }
    }

    protected override void OnEventSourceCreated(EventSource eventSource)
    {
        lock (_foundBeforeReady)
        {
            if (_logger is null)
            {
                _foundBeforeReady.Add(eventSource);
                return;
            }
        }

        Enable(eventSource);
    }

    protected override void OnEventWritten(EventWrittenEventArgs eventData)
    {
        // only the sources enabled in Enable arrive here, and that happens after the logger is set
        ILogger logger = _logger!;
        string message = eventData.Payload is { Count: > 0 } payload ? Convert.ToString(payload[0], CultureInfo.InvariantCulture) ?? string.Empty : string.Empty;
        switch (eventData.EventId)
        {
            case WarnEventId:
                LogWarning(logger, message);
                break;
            case ErrorEventId:
                LogError(logger, message, Describe(eventData.Payload is { Count: > 1 } details ? details[1] : null));
                break;
            default:
                LogInformation(logger, message);
                break;
        }
    }

    private void Enable(EventSource source)
    {
        if (_isClientSource(source))
        {
            EnableEvents(source, EventLevel.Informational);
        }
    }

    // the library hands its exception over as a dictionary (Type, Message, StackTrace, InnerException)
    internal static string Describe(object? exception)
    {
        if (exception is not IDictionary<string, object?> detail)
        {
            return string.Empty;
        }

        detail.TryGetValue("Type", out object? type);
        detail.TryGetValue("Message", out object? message);
        detail.TryGetValue("StackTrace", out object? stackTrace);
        return string.Create(CultureInfo.InvariantCulture, $"{type}: {message}{Environment.NewLine}{stackTrace}");
    }

    [LoggerMessage(EventId = 4301, EventName = "RABBITMQ_CLIENT_INFORMATION", Level = LogLevel.Debug, Message = "RabbitMQ client: {Message}")]
    private static partial void LogInformation(ILogger logger, string message);

    [LoggerMessage(EventId = 4302, EventName = "RABBITMQ_CLIENT_WARNING", Level = LogLevel.Warning, Message = "RabbitMQ client: {Message}")]
    private static partial void LogWarning(ILogger logger, string message);

    [LoggerMessage(EventId = 4303, EventName = "RABBITMQ_CLIENT_ERROR", Level = LogLevel.Error, Message = "RabbitMQ client: {Message} {Exception}")]
    private static partial void LogError(ILogger logger, string message, string exception);
}
