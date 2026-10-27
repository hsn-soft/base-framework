using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using NLog;
using NLog.Targets;

namespace HsnSoft.Base.Logging.NLog;

/// <summary>
/// Sends a log to Graylog as a GELF message over HTTP (<c>http://address:port/gelf</c>), in the background: a log is handed to a queue and sent by a task, so a slow or unreachable Graylog does not slow the application, and a
/// message that could not be sent is tried again. Every property of the log becomes a field of its name with <c>_</c> in front (a name Graylog cannot take is cleaned; <c>id</c> is reserved and is left out).
/// </summary>
internal sealed partial class GelfHttpTarget : AsyncTaskTarget
{
    private static readonly Regex s_invalidFieldCharacters = InvalidFieldCharacters();

    private readonly Uri _address;
    private readonly string _host;
    private readonly HttpClient _client;
    private readonly bool _disposeClient;

    internal GelfHttpTarget(string address, int port, HttpClient? client = null, string? host = null)
    {
        _address = new Uri($"http://{address}:{port.ToString(CultureInfo.InvariantCulture)}/gelf");
        _host = host ?? Environment.MachineName;
        _client = client ?? new HttpClient { Timeout = TimeSpan.FromSeconds(10) };
        _disposeClient = client is null;
        Name = "base-graylog";
        RetryCount = 2;
        RetryDelayMilliseconds = 500;
        TaskDelayMilliseconds = 200;
        BatchSize = 1;
    }

    protected override async Task WriteAsyncTask(LogEventInfo logEvent, CancellationToken cancellationToken)
    {
        using StringContent content = new(Message(logEvent), Encoding.UTF8, "application/json");
        using HttpResponseMessage response = await _client.PostAsync(_address, content, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _disposeClient)
        {
            _client.Dispose();
        }

        base.Dispose(disposing);
    }

    /// <summary>The GELF message of a log: <c>version</c>, <c>host</c>, <c>short_message</c> (the message), <c>full_message</c> (the exception), <c>timestamp</c>, <c>level</c> (syslog) and the fields.</summary>
    internal string Message(LogEventInfo logEvent)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            writer.WriteStartObject();
            writer.WriteString("version", "1.1");
            writer.WriteString("host", _host);
            writer.WriteString("short_message", string.IsNullOrEmpty(logEvent.FormattedMessage) ? "-" : logEvent.FormattedMessage);
            if (logEvent.Exception is not null)
            {
                writer.WriteString("full_message", logEvent.Exception.ToString());
            }

            writer.WriteNumber("timestamp", new DateTimeOffset(logEvent.TimeStamp.ToUniversalTime()).ToUnixTimeMilliseconds() / 1000.0);
            writer.WriteNumber("level", Syslog(logEvent.Level));
            foreach (KeyValuePair<object, object?> property in logEvent.Properties)
            {
                string name = s_invalidFieldCharacters.Replace(property.Key.ToString() ?? string.Empty, "_");
                if (name.Length == 0 || name == "id")
                {
                    continue;
                }

                writer.WritePropertyName("_" + name);
                WriteValue(writer, property.Value);
            }

            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static void WriteValue(Utf8JsonWriter writer, object? value)
    {
        switch (value)
        {
            case null:
                writer.WriteNullValue();
                break;
            case bool flag:
                writer.WriteBooleanValue(flag);
                break;
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                JsonSerializer.Serialize(writer, value, value.GetType());
                break;
            default:
                writer.WriteStringValue(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
        }
    }

    // the syslog levels GELF uses
    private static int Syslog(LogLevel level)
        => level.Ordinal switch
        {
            0 or 1 => 7,
            2 => 6,
            3 => 4,
            4 => 3,
            _ => 2,
        };

    [GeneratedRegex(@"[^\w.\-]", RegexOptions.CultureInvariant)]
    private static partial Regex InvalidFieldCharacters();
}
