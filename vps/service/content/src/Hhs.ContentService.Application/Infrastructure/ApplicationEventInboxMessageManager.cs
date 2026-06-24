using System.Text.Json;
using Hhs.Shared.Helper;
using Hhs.ContentService.Domain.InfraDomain.Entities;
using Hhs.ContentService.Domain.InfraDomain.Repositories;
using Hhs.ContentService.Domain.Configuration;
using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.ContentService.Application.Infrastructure;

public sealed class ApplicationEventInboxMessageManager(IEventInboxMessageRepository repository, ContentRetrySettings settings)
{
    public async Task<bool> IsProcessedAsync(Guid eventId, CancellationToken cancellationToken)
    {
        return await repository.ExistsAsync(
            x => x.Id == eventId && x.Status == InboxStatuses.Completed,
            cancellationToken: cancellationToken);
    }

    public async Task<bool> StartAsync<TEvent>(MessageEnvelope<TEvent> @event, CancellationToken cancellationToken)
        where TEvent : IIntegrationEventMessage
    {
        if (await IsProcessedAsync(@event.MessageId, cancellationToken))
            return false;

        var existing = await repository.GetByIdAsync(@event.MessageId, cancellationToken: cancellationToken);

        if (existing is not null)
        {
            if (existing.Status == InboxStatuses.Completed)
                return false;

            if (existing.Status == InboxStatuses.Failed)
            {
                if (existing.RetryCount >= settings.MaxRetryCount)
                    return false;

                await repository.UpdateByExpressionAsync(
                    x => x.Id == @event.MessageId && x.Status == InboxStatuses.Failed,
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

        try
        {
            var newMessage = new EventInboxMessage(
                @event.MessageId,
                typeof(TEvent).Name,
                JsonSerializer.Serialize(@event.Message, @event.Message.GetType()));

            await repository.InsertAsync(newMessage, cancellationToken);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task CompleteAsync(Guid eventId, CancellationToken cancellationToken)
    {
        await repository.UpdateByExpressionAsync(
            x => x.Id == eventId,
            s => s
                .SetProperty(a => a.Status, InboxStatuses.Completed)
                .SetProperty(a => a.ProcessedAtUtc, DateTime.UtcNow)
                .SetProperty(a => a.ErrorMessage, (string?)null),
            cancellationToken: cancellationToken);
    }

    public async Task FailAsync(Guid eventId, Exception ex, CancellationToken cancellationToken)
    {
        var inbox = await repository.GetByIdAsync(eventId, cancellationToken: cancellationToken);

        if (inbox != null)
        {
            inbox.Status = InboxStatuses.Failed;
            inbox.ErrorMessage = ex.Message;
            inbox.RetryCount = Math.Max(inbox.RetryCount, 0);

            await repository.UpdateAsync(inbox, cancellationToken);
        }
    }
}