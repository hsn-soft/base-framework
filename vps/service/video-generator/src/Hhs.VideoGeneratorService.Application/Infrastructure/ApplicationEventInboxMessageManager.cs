using System.Text.Json;
using Hhs.Shared.Helper;
using Hhs.VideoGeneratorService.Domain.InfraDomain.Entities;
using Hhs.VideoGeneratorService.Domain.InfraDomain.Repositories;
using Hhs.VideoGeneratorService.Domain.Configuration;
using HsnSoft.Base.Domain.Entities.Events;

namespace Hhs.VideoGeneratorService.Application.Infrastructure;

public sealed class ApplicationEventInboxMessageManager(IEventInboxMessageRepository repository, VideoRetrySettings settings)
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

                existing.Status = InboxStatuses.Started;
                existing.RetryCount = existing.RetryCount + 1;
                existing.ErrorMessage = null;

                await repository.UpdateAsync(existing, cancellationToken);

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
        var inbox = await repository.GetByIdAsync(eventId, cancellationToken: cancellationToken);

        if (inbox != null)
        {
            inbox.Status = InboxStatuses.Completed;
            inbox.ProcessedAtUtc = DateTime.UtcNow;
            inbox.ErrorMessage = null;

            await repository.UpdateAsync(inbox, cancellationToken);
        }
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
