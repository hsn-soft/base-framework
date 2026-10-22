using System;
using System.Text.Json;

namespace HsnSoft.Base.EventBus;

/// <summary>The one wire format of events: UTF-8 JSON with web defaults (camelCase). Every broker project uses it.</summary>
public static class EventSerializer
{
    public static byte[] Serialize<TEvent>(TEvent integrationEvent)
        where TEvent : IntegrationEvent
        => JsonSerializer.SerializeToUtf8Bytes(integrationEvent, JsonSerializerOptions.Web);

    /// <exception cref="JsonException">The message is not a valid <paramref name="eventType"/>.</exception>
    public static IntegrationEvent Deserialize(ReadOnlySpan<byte> body, Type eventType)
        => (IntegrationEvent?)JsonSerializer.Deserialize(body, eventType, JsonSerializerOptions.Web)
           ?? throw new JsonException("The message body is empty.");
}
