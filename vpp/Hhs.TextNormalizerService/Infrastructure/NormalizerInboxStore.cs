using System.Text.Json;
using Hhs.Shared.Events;
using Hhs.TextNormalizerService.Entities;
using Hhs.TextNormalizerService.Mongo;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Infrastructure;

public sealed class NormalizerInboxStore(NormalizerMongoContext context)
{
    public async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await context.InboxMessages
            .Find(x => x.EventId == eventId && x.Status == "PROCESSED")
            .AnyAsync(cancellationToken);
    }

    public async Task StartAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        var existing = await context.InboxMessages
            .Find(x => x.EventId == @event.EventId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
            return;

        await context.InboxMessages.InsertOneAsync(new NormalizerInboxMessage
        {
            EventId = @event.EventId,
            EventName = @event.EventName,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            Status = "STARTED",
            CreatedAtUtc = DateTime.UtcNow
        }, cancellationToken: cancellationToken);
    }

    public async Task CompleteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await context.InboxMessages.UpdateOneAsync(
            x => x.EventId == eventId,
            Builders<NormalizerInboxMessage>.Update
                .Set(x => x.Status, "PROCESSED")
                .Set(x => x.ProcessedAtUtc, DateTime.UtcNow)
                .Set(x => x.ErrorMessage, null),
            cancellationToken: cancellationToken);
    }

    public async Task FailAsync(Guid eventId, Exception ex, CancellationToken cancellationToken)
    {
        await context.InboxMessages.UpdateOneAsync(
            x => x.EventId == eventId,
            Builders<NormalizerInboxMessage>.Update
                .Set(x => x.Status, "FAILED")
                .Set(x => x.ErrorMessage, ex.Message),
            cancellationToken: cancellationToken);
    }
}