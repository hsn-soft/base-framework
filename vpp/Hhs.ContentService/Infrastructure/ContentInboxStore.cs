using System.Text.Json;
using Hhs.ContentService.Data;
using Hhs.Shared.Events;
using Hhs.ContentService.Entities;
using Hhs.Shared.Inbox;
using Microsoft.EntityFrameworkCore;

namespace Hhs.ContentService.Infrastructure;

public sealed class ContentInboxStore(ContentDbContext context)
{
    public async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await context.InboxMessages
            .AnyAsync(x => x.EventId == eventId && x.Status == InboxStatuses.Completed,
                cancellationToken: cancellationToken);
    }

    public async Task StartAsync<TEvent>(TEvent @event, CancellationToken cancellationToken)
        where TEvent : IntegrationEvent
    {
        var existing = await context.InboxMessages
            .FirstOrDefaultAsync(x => x.EventId == @event.EventId, cancellationToken: cancellationToken);

        if (existing is not null)
            return;

        context.InboxMessages.Add(new ContentInboxMessage
        {
            EventId = @event.EventId,
            EventName = @event.EventName,
            Payload = JsonSerializer.Serialize(@event, @event.GetType()),
            Status = InboxStatuses.Started,
            CreatedAtUtc = DateTime.UtcNow
        });
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task CompleteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await context.InboxMessages
            .Where(x => x.EventId == eventId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(a => a.Status, InboxStatuses.Completed)
                    .SetProperty(a => a.ProcessedAtUtc, DateTime.UtcNow)
                    .SetProperty(a => a.ErrorMessage, string.Empty),
                cancellationToken: cancellationToken);
    }

    public async Task FailAsync(Guid eventId, Exception ex, CancellationToken cancellationToken)
    {
        await context.InboxMessages
            .Where(x => x.EventId == eventId)
            .ExecuteUpdateAsync(
                s => s
                    .SetProperty(a => a.Status, InboxStatuses.Failed)
                    .SetProperty(a => a.ErrorMessage, ex.Message),
                cancellationToken: cancellationToken);
    }
}