using System.Text.Json;
using Hhs.Shared.Events;
using Hhs.TextNormalizerService.Entities;
using Hhs.TextNormalizerService.Mongo;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Infrastructure;

public sealed class NormalizerInboxStore
{
    private readonly NormalizerMongoContext _context;

    public NormalizerInboxStore(NormalizerMongoContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await _context.InboxMessages
            .Find(x => x.EventId == eventId)
            .AnyAsync(cancellationToken);
    }

    public async Task SaveAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        await _context.InboxMessages.InsertOneAsync(new NormalizerInboxMessage
        {
            EventId = @event.EventId,
            EventName = @event.EventName,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            ProcessedAtUtc = DateTime.UtcNow
        }, cancellationToken: cancellationToken);
    }
}