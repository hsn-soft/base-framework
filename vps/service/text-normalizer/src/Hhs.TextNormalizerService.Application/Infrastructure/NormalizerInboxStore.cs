using System.Text.Json;
using Hhs.Shared.Helper;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.Domain.Entities.Events;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Application.Infrastructure;

public sealed class NormalizerInboxStore(TextNormalizerServiceDbContext context)
{
    public async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await context.NormalizerInboxMessages
            .Find(x => x.EventId == eventId && x.Status == InboxStatuses.Completed)
            .AnyAsync(cancellationToken);
    }

    public async Task<bool> StartAsync<TEvent>(MessageEnvelope<TEvent> @event, CancellationToken cancellationToken)
        where TEvent : IIntegrationEventMessage
    {
        if (await IsProcessedAsync(@event.MessageId, cancellationToken))
            return false;

        var existing = await context.NormalizerInboxMessages
            .Find(x => x.EventId == @event.MessageId)
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == InboxStatuses.Completed)
                return false;

            if (existing.Status == InboxStatuses.Failed)
            {
                if (existing.RetryCount >= 30)
                    return false;

                await context.NormalizerInboxMessages.UpdateOneAsync(
                    x => x.EventId == @event.MessageId && x.Status == InboxStatuses.Failed,
                    Builders<NormalizerInboxMessage>.Update
                        .Set(x => x.Status, InboxStatuses.Started)
                        .Set(x => x.RetryCount, existing.RetryCount + 1)
                        .Set(x => x.ErrorMessage, null),
                    cancellationToken: cancellationToken);

                return true;
            }

            if (existing.Status == InboxStatuses.Started)
                return false;

            return false;
        }

        try
        {
            await context.NormalizerInboxMessages.InsertOneAsync(new NormalizerInboxMessage
            {
                EventId = @event.MessageId,
                EventName = typeof(TEvent).Name,
                Payload = JsonSerializer.Serialize(@event.Message, @event.Message.GetType()),
                Status = InboxStatuses.Started,
                CreatedAtUtc = DateTime.UtcNow,
                RetryCount = 0
            }, cancellationToken: cancellationToken);

            return true;
        }
        catch (MongoWriteException ex)
            when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
        {
            return false;
        }
    }

    public async Task CompleteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await context.NormalizerInboxMessages.UpdateOneAsync(
            x => x.EventId == eventId,
            Builders<NormalizerInboxMessage>.Update
                .Set(x => x.Status, InboxStatuses.Completed)
                .Set(x => x.ProcessedAtUtc, DateTime.UtcNow)
                .Set(x => x.ErrorMessage, null),
            cancellationToken: cancellationToken);
    }

    public async Task FailAsync(Guid eventId, Exception ex, CancellationToken cancellationToken)
    {
        var inbox = await context.NormalizerInboxMessages
            .Find(x => x.EventId == eventId)
            .FirstOrDefaultAsync(cancellationToken);

        if (inbox != null)
        {
            inbox.Status = InboxStatuses.Failed;
            inbox.ErrorMessage = ex.Message;
            inbox.RetryCount = Math.Max(inbox.RetryCount, 0);

            await context.NormalizerInboxMessages.ReplaceOneAsync(
                x => x.EventId == eventId,
                inbox,
                cancellationToken: cancellationToken);
        }
    }
}
