using System.Text.Json;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.ContentService.Domain.ContentDomain.Entities;
using Hhs.ContentService.EntityFrameworkCore.Context;
using HsnSoft.Base.Domain.Entities.Events;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.Application.Infrastructure;

public sealed class ContentInboxStore(ContentServiceDbContext context)
{
    public async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await context.InboxMessages
            .AnyAsync(x => x.EventId == eventId && x.Status == InboxStatuses.Completed,
                cancellationToken: cancellationToken);
    }

    public async Task<bool> StartAsync<TEvent>(MessageEnvelope<TEvent> @event, CancellationToken cancellationToken)
        where TEvent : IIntegrationEventMessage
    {
        if (await IsProcessedAsync(@event.MessageId, cancellationToken))
            return false;

        var existing = await context.InboxMessages
            .FirstOrDefaultAsync(x => x.EventId == @event.MessageId, cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == InboxStatuses.Completed)
                return false;

            if (existing.Status == InboxStatuses.Failed)
            {
                if (existing.RetryCount >= 30)
                    return false;

                await context.InboxMessages
                    .Where(x => x.EventId == @event.MessageId && x.Status == InboxStatuses.Failed)
                    .ExecuteUpdateAsync(
                        s => s
                            .SetProperty(a => a.Status, InboxStatuses.Started)
                            .SetProperty(a => a.RetryCount, existing.RetryCount + 1)
                            .SetProperty(a => a.ErrorMessage, (string?)null),
                        cancellationToken: cancellationToken);

                return true;
            }

            if (existing.Status == InboxStatuses.Started)
                return false;

            return false;
        }

        context.InboxMessages.Add(new ContentInboxMessage
        {
            EventId = @event.MessageId,
            EventName = typeof(TEvent).Name,
            Payload = JsonSerializer.Serialize(@event.Message, @event.Message.GetType()),
            Status = InboxStatuses.Started,
            CreatedAtUtc = DateTime.UtcNow,
            RetryCount = 0
        });

        try
        {
            await context.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException)
        {
            return false;
        }
    }

    public async Task CompleteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await context.InboxMessages
            .Where(x => x.EventId == eventId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(a => a.Status, InboxStatuses.Completed)
                    .SetProperty(a => a.ProcessedAtUtc, DateTime.UtcNow)
                    .SetProperty(a => a.ErrorMessage, (string?)null),
                cancellationToken: cancellationToken);
    }

    public async Task FailAsync(Guid eventId, Exception ex, CancellationToken cancellationToken)
    {
        var inbox = await context.InboxMessages
            .FirstOrDefaultAsync(x => x.EventId == eventId, cancellationToken);

        if (inbox != null)
        {
            inbox.Status = InboxStatuses.Failed;
            inbox.ErrorMessage = ex.Message;
            inbox.RetryCount = Math.Max(inbox.RetryCount, 0);

            await context.SaveChangesAsync(cancellationToken);
        }
    }
}