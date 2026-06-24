using Hhs.Shared.Helper;
using Hhs.EventManagerService.Domain.Configuration;
using Hhs.EventManagerService.Domain.InfraDomain.Entities;
using Hhs.EventManagerService.Domain.InfraDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using Microsoft.Extensions.Logging;

namespace Hhs.EventManagerService.Application.Services;

public sealed class EventOperationRetryWorkerService(
    IServiceProvider provider,
    IEventInboxMessageRepository inboxRepository,
    EventManagerRetrySettings retrySettings,
    ILogger<EventOperationRetryWorkerService> logger
) : ApplicationServiceBase(provider)
{
    public async Task RetryDueRequestsAsync(CancellationToken cancellationToken)
    {
        var failedInboxes = await inboxRepository.GetByStatusAsync(InboxStatuses.Failed, cancellationToken);

        var failedToRetry = failedInboxes
            .Where(x => x.RetryCount < 30)
            .Take(retrySettings.BatchSize)
            .ToList();

        foreach (var inbox in failedToRetry)
        {
            try
            {
                inbox.Status = InboxStatuses.Started;
                inbox.RetryCount = inbox.RetryCount + 1;
                inbox.ErrorMessage = null;

                await inboxRepository.UpdateAsync(inbox, cancellationToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error retrying inbox message {InboxId}", inbox.Id);
            }
        }
    }
}
