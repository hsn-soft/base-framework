using System.Text.Json;
using Hhs.Shared.Events;
using Hhs.Shared.Inbox;
using Hhs.TextNormalizerService.Entities;
using Hhs.TextNormalizerService.Mongo;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Infrastructure;

public sealed class NormalizerInboxStore(
    NormalizerMongoContext context)
{
    public async Task<bool> IsProcessedAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        return await context.NormalizerInboxMessages
            .Find(x =>
                x.EventId == eventId &&
                x.Status == InboxStatuses.Completed)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> StartAsync<TEvent>(
        TEvent @event,
        CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        if (await IsProcessedAsync(@event.EventId, cancellationToken))
            return false;

        var existing = await context.NormalizerInboxMessages
            .Find(x => x.EventId == @event.EventId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            if (existing.Status != InboxStatuses.Failed)
                return false;

            await context.NormalizerInboxMessages.UpdateOneAsync(
                x => x.EventId == @event.EventId && x.Status == InboxStatuses.Failed,
                Builders<NormalizerInboxMessage>.Update
                    .Set(x => x.Status, InboxStatuses.Started)
                    .Set(x => x.ErrorMessage, null)
                    .Set(x => x.CreatedAtUtc, DateTime.UtcNow),
                cancellationToken: cancellationToken);

            return true;
        }

        try
        {
            await context.NormalizerInboxMessages.InsertOneAsync(
                new NormalizerInboxMessage
                {
                    EventId = @event.EventId,
                    EventName = @event.EventName,
                    Payload = JsonSerializer.Serialize(
                        @event,
                        @event.GetType()),
                    Status = InboxStatuses.Started,
                    CreatedAtUtc = DateTime.UtcNow
                },
                cancellationToken: cancellationToken);

            return true;
        }
        catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async Task CompleteAsync(
        Guid eventId,
        CancellationToken cancellationToken)
    {
        await context.NormalizerInboxMessages.UpdateOneAsync(
            x => x.EventId == eventId,
            Builders<NormalizerInboxMessage>.Update
                .Set(x => x.Status, InboxStatuses.Completed)
                .Set(x => x.ProcessedAtUtc, DateTime.UtcNow)
                .Set(x => x.ErrorMessage, null),
            cancellationToken: cancellationToken);
    }

    public async Task FailAsync(
        Guid eventId,
        Exception ex,
        CancellationToken cancellationToken)
    {
        await context.NormalizerInboxMessages.UpdateOneAsync(
            x => x.EventId == eventId,
            Builders<NormalizerInboxMessage>.Update
                .Set(x => x.Status, InboxStatuses.Failed)
                .Set(x => x.ErrorMessage, ex.ToString()),
            cancellationToken: cancellationToken);
    }
}