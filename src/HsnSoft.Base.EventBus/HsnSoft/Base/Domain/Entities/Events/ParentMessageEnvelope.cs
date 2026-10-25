using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace HsnSoft.Base.Domain.Entities.Events;

/// <summary>
/// What travels with every message next to the message itself, and what a handler gets about the message that caused another one.
/// On the wire it is flat: these properties and the message sit side by side in one JSON object (PascalCase).
/// </summary>
public record ParentMessageEnvelope
{
    /// <summary>
    /// The header that says what was running when the message was published: the step of a request (<c>token-login</c>), the event
    /// a handler was handling (<c>ordercreated</c>), or <c>agent</c>. It is where the message "comes from" in the logs.
    /// </summary>
    public const string FlowHeader = "Flow";

    /// <summary>How many messages deep the chain is: 1 for a message nobody caused, the cause's level + 1 otherwise.</summary>
    public ushort HopLevel { get; set; }

    /// <summary>How often the message was put back on its queue by hand (see <c>isReQueuePublish</c>).</summary>
    public ushort ReQueuedCount { get; set; }

    public Guid MessageId { get; set; }

    public DateTime MessageTime { get; set; }

    /// <summary>Ties together every message that came out of one request.</summary>
    public string? CorrelationId { get; set; }

    /// <summary>Who produced it: the <c>ConsumerClientName</c> of the service (<c>shipment-service</c>).</summary>
    public string? Producer { get; set; }

    /// <summary>The tenant the chain acts in: the tenant of the user who started it. The handler of the message runs in it (<see cref="HsnSoft.Base.Security.ActingUser"/>).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? TenantId { get; set; }

    /// <summary>Whether the caller belongs to the system tenant, which sees every tenant.</summary>
    public bool IsSystemTenant { get; set; }

    /// <summary>The id of the user who started the chain (what the audit fields of a handler keep).</summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public Guid? UserId { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? UserName { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? UserRoles { get; set; }

    /// <summary>Whether the envelope says who the caller is.</summary>
    internal bool HasCaller => TenantId is not null || UserId is not null;

    /// <summary>
    /// Every other top-level property of the message: the information a product adds (user, roles, client details) and keeps on the
    /// wire under its own names. The framework carries it from a message to the ones it causes but does not interpret it.
    /// </summary>
    [IgnoreDataMember] // other serializers (the services copy an envelope through Newtonsoft) must leave it alone; it only exists on the wire
    [JsonExtensionData]
    public Dictionary<string, JsonElement>? Headers { get; set; }

    /// <summary>The value of a header as text, or <see langword="null"/> when it is absent or null.</summary>
    public string? GetHeader(string name)
    {
        if (Headers is null)
        {
            return null;
        }

        foreach (KeyValuePair<string, JsonElement> header in Headers)
        {
            if (string.Equals(header.Key, name, StringComparison.OrdinalIgnoreCase))
            {
                return header.Value.ValueKind switch
                {
                    JsonValueKind.String => header.Value.GetString(),
                    JsonValueKind.Null or JsonValueKind.Undefined => null,
                    _ => header.Value.GetRawText(),
                };
            }
        }

        return null;
    }

    /// <summary>Sets a header; a <see langword="null"/> value removes it.</summary>
    public void SetHeader(string name, string? value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        if (value is null)
        {
            Headers?.Remove(name);
            return;
        }

        Headers ??= [];
        Headers[name] = JsonSerializer.SerializeToElement(value);
    }

    /// <summary>
    /// Whether a top-level property name belongs to <see cref="MessageEnvelope{T}"/> itself. Read as a plain parent envelope, such a
    /// property lands in <see cref="Headers"/>, and written again from there it would appear twice in the JSON.
    /// </summary>
    internal static bool IsEnvelopeProperty(string name)
        => string.Equals(name, "Message", StringComparison.OrdinalIgnoreCase)
           || string.Equals(name, "ParentMessageId", StringComparison.OrdinalIgnoreCase);

    /// <summary>The headers worth passing on to a message this one causes (not <see cref="FlowHeader"/>: every message says where it comes from itself).</summary>
    internal IEnumerable<KeyValuePair<string, JsonElement>> HeadersToInherit()
        => Headers is null
            ? []
            : Headers.Where(header => !IsEnvelopeProperty(header.Key) && !string.Equals(header.Key, FlowHeader, StringComparison.OrdinalIgnoreCase));
}
