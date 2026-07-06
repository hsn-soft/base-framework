using Hhs.ContentService.Domain.Configuration;
using Hhs.Shared.Contracts.EventInbox;
using Microsoft.Extensions.Logging;

namespace Hhs.ContentService.Application.Services;

public sealed class ContentOperationRetryWorkerService(
    IServiceProvider provider,
    IEventInboxMessageManager inboxManager,
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

        var updated = await inboxManager.ResetStaleStartedMessagesAsync(staleThreshold, cancellationToken);

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
