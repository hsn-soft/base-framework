using Hhs.Shared.Helper;
using Hhs.FeedRService.Domain.Configuration;
using Hhs.FeedRService.Domain.InfraDomain.Repositories;
using Microsoft.Extensions.Logging;

namespace Hhs.FeedRService.Application.Services;

public sealed class FeedROperationRetryWorkerService(
    IServiceProvider provider,
    IEventInboxMessageRepository inboxRepository,
    FeedRRetrySettings retrySettings,
    ILogger<FeedROperationRetryWorkerService> logger
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
