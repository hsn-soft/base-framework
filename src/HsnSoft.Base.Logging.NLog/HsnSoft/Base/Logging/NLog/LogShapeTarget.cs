using System;
using System.Collections.Generic;
using NLog;
using NLog.Common;
using NLog.Targets;
using NLog.Targets.Wrappers;

namespace HsnSoft.Base.Logging.NLog;

/// <summary>
/// The target in front of the persistent sink (Graylog, file): gives every log the fields the sink sorts by and keeps their number and size bounded, as the Serilog provider does. It writes a log of its own to the wrapped target:
/// the message with its template filled in (secrets masked) and cut, and the properties of the log and of the scopes it was written in (the correlation id, ...), with
/// <list type="bullet">
/// <item><c>Solution</c>, <c>Assembly</c>, <c>Version</c>, <c>LoggerName</c> and <c>LogType</c> (<see cref="LogTypes"/>) and <c>MessageType</c> (<c>Structured</c> when an object was logged with <c>{@Object}</c>, <c>Args</c> otherwise);</item>
/// <item><c>Facility</c> from the name of the event when it is in capitals, unless the log has one;</item>
/// <item>no event id, no message details and no noise (<see cref="LogShape.NoiseFields"/>);</item>
/// <item>an object, a dictionary or a list as the one field holding its masked JSON, and no text longer than <c>MaxPersistentFieldLength</c>.</item>
/// </list>
/// A log that already carries a <c>LogType</c>, <c>MessageType</c> or <c>Facility</c> keeps it. The shaping is done on the thread that logs, where the scopes are, before any asynchronous target takes the log over.
/// </summary>
internal sealed class LogShapeTarget : WrapperTargetBase
{
    internal const string LoggerNameField = "LoggerName";

    private readonly string _appNamespace;
    private readonly string _hostName;
    private readonly string _solution;
    private readonly string _assembly;
    private readonly string? _version;
    private readonly int _maxLength;

    internal LogShapeTarget(Target wrapped, PersistentLoggerOptions options, string hostName)
    {
        WrappedTarget = wrapped;
        _hostName = hostName;
        _assembly = AppDomain.CurrentDomain.FriendlyName;
        _solution = _assembly.Split('.')[0];
        _appNamespace = string.IsNullOrWhiteSpace(options.AppNamespace) ? _solution : options.AppNamespace.Trim();
        _version = options.ResolveVersion();
        _maxLength = options.MaxPersistentFieldLength;
        Name = "base-shape";
    }

    internal string LogType(string category) => LogShape.LogType(category, _appNamespace);

    internal string LoggerName(string category) => LogRouting.IsConsoleOnly(category) ? category[LogCategories.ConsoleOnlyPrefix.Length..] : _hostName;

    protected override void Write(AsyncLogEventInfo logEvent)
        => WrappedTarget!.WriteAsyncLogEvent(Shape(logEvent.LogEvent).WithContinuation(logEvent.Continuation));

    internal LogEventInfo Shape(LogEventInfo source)
    {
        ArgumentNullException.ThrowIfNull(source);

        Dictionary<string, object?> properties = [];
        foreach (KeyValuePair<object, object?> property in source.Properties)
        {
            properties[property.Key.ToString() ?? string.Empty] = property.Value;
        }

        foreach (KeyValuePair<string, object?> scope in ScopeContext.GetAllProperties())
        {
            properties.TryAdd(scope.Key, scope.Value);
        }

        string category = source.LoggerName ?? string.Empty;
        string logType = properties.TryGetValue(LogFields.LogType, out object? existingType) && existingType is string text ? text : LogType(category);
        string? facility = properties.TryGetValue(LogShape.FacilityField, out object? existingFacility) ? existingFacility as string : LogShape.FacilityName(properties.GetValueOrDefault("EventName") as string);
        bool structured = false;
        foreach (KeyValuePair<string, object?> property in properties)
        {
            structured |= property.Key is not ("EventId" or "EventName") && NLogMessage.IsStructured(property.Value);
        }

        properties.TryAdd(LogFields.LogType, logType);
        properties.TryAdd(LogShape.MessageTypeField, structured ? LogShape.StructuredMessage : LogShape.ArgsMessage);
        if (facility is not null)
        {
            properties[LogShape.FacilityField] = facility;
        }

        properties["Solution"] = _solution;
        properties["Assembly"] = _assembly;
        if (_version is not null)
        {
            properties["Version"] = _version;
        }

        properties[LoggerNameField] = LoggerName(category);

        foreach (string noise in LogShape.NoiseFields((string)properties[LogFields.LogType]!))
        {
            properties.Remove(noise);
        }

        properties.Remove("EventId");
        properties.Remove("EventName");
        properties.Remove(LogFields.MessageDetails);

        LogEventInfo shaped = new(source.Level, category, LogFields.Text(NLogMessage.Render(source), _maxLength) ?? string.Empty)
        {
            TimeStamp = source.TimeStamp,
            Exception = source.Exception,
        };
        foreach (KeyValuePair<string, object?> property in properties)
        {
            shaped.Properties[property.Key] = NLogMessage.FieldValue(property.Value, _maxLength);
        }

        return shaped;
    }
}
