using System;
using System.Collections.Generic;
using System.Text.Json;
using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus;

/// <summary>Wraps a message in the envelope it is published with: ids, time, who produced it, and the chain it belongs to.</summary>
public sealed class EventEnvelopeFactory(IOptions<EventBusOptions> options, IEnumerable<IEventHeaderProvider> headerProviders)
{
    private readonly EventBusOptions _options = options.Value;
    private readonly IEventHeaderProvider[] _headerProviders = [.. headerProviders];

    public MessageEnvelope<T> Create<T>(T message, ParentMessageEnvelope? parentMessage, string? correlationId, bool isReQueuePublish)
        where T : IIntegrationEventMessage
    {
        ArgumentNullException.ThrowIfNull(message);

        // the message being handled is the parent unless the caller says otherwise
        ParentMessageEnvelope? parent = parentMessage ?? EventContext.Current;

        MessageEnvelope<T> envelope = new()
        {
            ParentMessageId = parent?.MessageId,
            MessageId = Guid.CreateVersion7(),
            MessageTime = DateTime.UtcNow,
            Message = message,
            Producer = _options.ConsumerClientInfo,
            CorrelationId = correlationId ?? parent?.CorrelationId ?? FirstCorrelationId(),
            HopLevel = parent is null ? (ushort)1 : (ushort)(parent.HopLevel + 1),
            ReQueuedCount = (ushort)((parent?.ReQueuedCount ?? 0) + (isReQueuePublish ? 1 : 0)),
        };

        // what the product knows about the caller first, then what the parent carries (the parent wins)
        Dictionary<string, string> provided = [];
        foreach (IEventHeaderProvider provider in _headerProviders)
        {
            provider.AddHeaders(provided);
        }

        foreach (KeyValuePair<string, string> header in provided)
        {
            envelope.SetHeader(header.Key, header.Value);
        }

        if (parent is not null)
        {
            foreach (KeyValuePair<string, JsonElement> header in parent.HeadersToInherit())
            {
                envelope.Headers ??= [];
                envelope.Headers[header.Key] = header.Value;
            }
        }

        return envelope;
    }

    private string? FirstCorrelationId()
    {
        foreach (IEventHeaderProvider provider in _headerProviders)
        {
            string? correlationId = provider.GetCorrelationId();
            if (!string.IsNullOrEmpty(correlationId))
            {
                return correlationId;
            }
        }

        return null;
    }
}
