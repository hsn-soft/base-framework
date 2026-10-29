using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using HsnSoft.Base.Logging.Masking;
using NLog;
using NLog.MessageTemplates;

namespace HsnSoft.Base.Logging.NLog;

/// <summary>
/// The text of a log as people read it, and the values of its properties as the fields of a sink keep them. NLog writes an object logged with <c>{@Object}</c> into the message itself, with every property in it: here the object
/// goes through <see cref="SensitiveDataTree"/> first, so what is secret is hidden in the message as it is in the fields.
/// </summary>
internal static class NLogMessage
{
    /// <summary>The message of a log with its template filled in; an object captured with <c>@</c> is written as the JSON of its masked tree.</summary>
    internal static string Render(LogEventInfo logEvent)
    {
        ArgumentNullException.ThrowIfNull(logEvent);

        MessageTemplateParameters parameters = logEvent.MessageTemplateParameters;
        if (parameters.Count == 0)
        {
            return logEvent.FormattedMessage;
        }

        string template = logEvent.Message;
        StringBuilder text = new(template.Length + 32);
        for (int index = 0; index < template.Length; index++)
        {
            char character = template[index];
            if (character is '{' or '}' && index + 1 < template.Length && template[index + 1] == character)
            {
                text.Append(character);
                index++;
                continue;
            }

            int close = character == '{' ? template.IndexOf('}', index + 1) : -1;
            if (close < 0)
            {
                text.Append(character);
                continue;
            }

            text.Append(Placeholder(template[(index + 1)..close], parameters, template[index..(close + 1)]));
            index = close;
        }

        return text.ToString();
    }

    /// <summary>The value of a property as a field keeps it: a plain value as it is, text cut to <paramref name="maxLength"/>, anything else (an object, a dictionary, a list) as the one field holding its masked JSON.</summary>
    internal static object? FieldValue(object? value, int maxLength)
        => value switch
        {
            null => null,
            string text => LogFields.Text(text, maxLength),
            Uri or IPAddress or Type => LogFields.Text(value.ToString(), maxLength),
            _ when IsScalar(value.GetType()) => value,
            _ => LogFields.Text(Json(value), maxLength),
        };

    internal static bool IsScalar(Type type) => SensitiveDataTree.IsSimple(type) || type == typeof(Uri) || type == typeof(IPAddress) || typeof(Type).IsAssignableFrom(type);

    /// <summary>Whether a value is taken apart when it is logged (an object, a dictionary, a list) and not written as it is.</summary>
    internal static bool IsStructured(object? value) => value is not null && !IsScalar(value.GetType());

    internal static string Json(object value)
    {
        using MemoryStream stream = new();
        using (Utf8JsonWriter writer = new(stream))
        {
            Write(writer, SensitiveDataTree.Build(value));
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string Placeholder(string body, MessageTemplateParameters parameters, string original)
    {
        int format = body.IndexOf(':', StringComparison.Ordinal);
        string head = format < 0 ? body : body[..format];
        int align = head.IndexOf(',', StringComparison.Ordinal);
        string name = (align < 0 ? head : head[..align]).TrimStart('@', '$');

        MessageTemplateParameter? found = parameters.Cast<MessageTemplateParameter?>().FirstOrDefault(parameter => parameter!.Value.Name == name);
        return found is not { } match
            ? original
            : match.CaptureType == CaptureType.Serialize && match.Value is { } serialized
                ? IsScalar(serialized.GetType()) ? Format(serialized, null) : Json(serialized)
                : Format(match.Value, format < 0 ? null : body[(format + 1)..]);
    }

    private static string Format(object? value, string? format)
        => value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(format, CultureInfo.InvariantCulture),
            _ => value.ToString() ?? string.Empty,
        };

    private static void Write(Utf8JsonWriter writer, MaskedNode node)
    {
        switch (node)
        {
            case MaskedStructure structure:
                writer.WriteStartObject();
                foreach (KeyValuePair<string, MaskedNode> property in structure.Properties)
                {
                    writer.WritePropertyName(property.Key);
                    Write(writer, property.Value);
                }

                writer.WriteEndObject();
                break;
            case MaskedDictionary dictionary:
                writer.WriteStartObject();
                foreach (KeyValuePair<string, MaskedNode> entry in dictionary.Entries)
                {
                    writer.WritePropertyName(entry.Key);
                    Write(writer, entry.Value);
                }

                writer.WriteEndObject();
                break;
            case MaskedSequence sequence:
                writer.WriteStartArray();
                foreach (MaskedNode item in sequence.Items)
                {
                    Write(writer, item);
                }

                writer.WriteEndArray();
                break;
            case MaskedScalar { Value: null }:
                writer.WriteNullValue();
                break;
            case MaskedScalar { Value: { } value }:
                JsonSerializer.Serialize(writer, value, value.GetType());
                break;
        }
    }
}
