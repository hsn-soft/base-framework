using Hhs.ContentService.Domain.Configuration;
using Hhs.ContentService.Domain.InfraDomain.Repositories;
using Hhs.Shared.Helper;
using Microsoft.Extensions.Logging;

namespace Hhs.ContentService.Application.Services;

public sealed class ContentOperationRetryWorkerService(
    IServiceProvider provider,
    IEventInboxMessageRepository inboxRepository,
    ILogger<ContentOperationRetryWorkerService> logger,
    ContentRetrySettings retrySettings) : ApplicationServiceBase(provider)
{
    public async Task RetryDueRequestsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ResetStaleStartedMessagesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Content operation retry worker cancelled");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Content operation retry worker encountered an error");
            throw;
        }
    }

    /// <summary>
    /// Resets EventInboxMessage records that are stuck in 'Started' status beyond the stale
    /// threshold back to 'Failed', so the next broker re-delivery can attempt processing.
    /// This handles the case where a handler crashed after inserting the inbox record but
    /// before calling CompleteAsync.
    /// </summary>
    private async Task ResetStaleStartedMessagesAsync(CancellationToken cancellationToken)
    {
        var staleThreshold = DateTime.UtcNow.AddMinutes(-retrySettings.StaleInboxMessageThresholdMinutes);

        var updated = await inboxRepository.UpdateByExpressionAsync(
            x => x.Status == InboxStatuses.Started && x.CreationTime < staleThreshold,
            s => s
                .SetProperty(a => a.Status, InboxStatuses.Failed)
                .SetProperty(a => a.ErrorMessage, "Reset by retry worker: handler did not complete within the stale threshold."),
            cancellationToken);

        if (updated > 0)
        {
            logger.LogWarning(
                "Content retry worker reset {Count} stale inbox message(s) from 'Started' to 'Failed'. " +
                "These will be re-processed on next broker re-delivery.",
                updated);
        }
        else
        {
            logger.LogDebug("Content retry worker: no stale inbox messages found. Batch size: {BatchSize}", retrySettings.BatchSize);
        }
    }
}
