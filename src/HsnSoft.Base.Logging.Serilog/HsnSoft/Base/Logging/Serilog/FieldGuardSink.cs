using System;
using System.IO;
using System.Linq;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting.Json;
using Serilog.Parsing;

namespace HsnSoft.Base.Logging.Serilog;

/// <summary>
/// Keeps the fields of the persistent sink bounded. Graylog turns every property of a log into a field, and an object logged with
/// <c>{@Object}</c> into one field per property (and per property of those): a body with two hundred properties would create two hundred
/// fields, for every shape a developer ever logs. Here an object (or a dictionary, or a list) becomes the one field of its name, holding its
/// JSON, and no text is longer than the limit, so the set of fields stays what the code defines. The console is not touched.
/// </summary>
internal sealed class FieldGuardSink(ILogEventSink inner, int maxLength) : ILogEventSink
{
    private static readonly JsonValueFormatter s_json = new(typeTagName: null);

    public void Emit(LogEvent logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        // the event id (EventId_Id, EventId_Name) is not a field: its name is the Facility, and an id the compiler makes up says nothing
        LogEventProperty[] properties = [.. logEvent.Properties.Where(property => property.Key != "EventId").Select(property => new LogEventProperty(property.Key, Guard(property.Value)))];
        inner.Emit(new LogEvent(logEvent.Timestamp, logEvent.Level, logEvent.Exception, PlainMessage(logEvent), properties, logEvent.TraceId ?? default, logEvent.SpanId ?? default));
    }

    private LogEventPropertyValue Guard(LogEventPropertyValue value)
        => value switch
        {
            StructureValue or DictionaryValue or SequenceValue => new ScalarValue(Cut(Json(value))),
            ScalarValue { Value: string text } => new ScalarValue(Cut(text)),
            _ => value,
        };

    // the message as people read it: {From} -> {To} is httpclient -> token-login, not "httpclient" -> "token-login"
    private MessageTemplate PlainMessage(LogEvent logEvent)
    {
        using StringWriter writer = new(System.Globalization.CultureInfo.InvariantCulture);
        foreach (MessageTemplateToken token in logEvent.MessageTemplate.Tokens)
        {
            if (token is PropertyToken property && logEvent.Properties.TryGetValue(property.PropertyName, out LogEventPropertyValue? value))
            {
                if (value is ScalarValue { Value: string text })
                {
                    writer.Write(text);
                }
                else
                {
                    value.Render(writer, property.Format, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            else
            {
                token.Render(logEvent.Properties, writer, System.Globalization.CultureInfo.InvariantCulture);
            }
        }

        string message = Cut(writer.ToString());
        return new MessageTemplate(message, [new TextToken(message)]);
    }

    private string Cut(string text) => LogFields.Text(text, maxLength)!;

    private static string Json(LogEventPropertyValue value)
    {
        using StringWriter writer = new(System.Globalization.CultureInfo.InvariantCulture);
        s_json.Format(value, writer);
        return writer.ToString();
    }
}
