using System;
using System.Linq;
using System.Text.Json;
using HsnSoft.Base.Domain.Entities.Events;

namespace HsnSoft.Base.EventBus;

/// <summary>
/// The one wire format of messages: UTF-8 JSON, envelope and message in one flat object, property names exactly as the C# properties
/// (PascalCase) because that is what the services in production write. Reading ignores the case of names.
/// </summary>
public static class EventSerializer
{
    private static readonly JsonSerializerOptions s_options = new() { PropertyNameCaseInsensitive = true };

    public static byte[] Serialize<T>(MessageEnvelope<T> envelope)
        where T : IIntegrationEventMessage
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return JsonSerializer.SerializeToUtf8Bytes(envelope, s_options);
    }

    /// <summary>Reads a message as a <c>MessageEnvelope&lt;eventType&gt;</c>.</summary>
    /// <exception cref="JsonException">The body is not a valid message of that type.</exception>
    public static object DeserializeEnvelope(ReadOnlySpan<byte> body, Type eventType)
    {
        ArgumentNullException.ThrowIfNull(eventType);

        Type envelopeType = typeof(MessageEnvelope<>).MakeGenericType(eventType);
        return JsonSerializer.Deserialize(body, envelopeType, s_options)
               ?? throw new JsonException("The message body is empty.");
    }

    /// <summary>Reads only the envelope of a message, whatever the type of the message inside.</summary>
    /// <exception cref="JsonException">The body is not a valid message.</exception>
    public static ParentMessageEnvelope DeserializeParent(ReadOnlySpan<byte> body)
    {
        ParentMessageEnvelope envelope = JsonSerializer.Deserialize<ParentMessageEnvelope>(body, s_options)
                                         ?? throw new JsonException("The message body is empty.");
        if (envelope.Headers is not null)
        {
            // the message and the id of the message that caused it are not headers
            foreach (string name in envelope.Headers.Keys.Where(ParentMessageEnvelope.IsEnvelopeProperty).ToList())
            {
                envelope.Headers.Remove(name);
            }
        }

        return envelope;
    }
}
