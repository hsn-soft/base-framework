using Hhs.IdentityService.Domain.Configuration;
using Hhs.Shared.Contracts.EventInbox;
using Microsoft.Extensions.Logging;

namespace Hhs.IdentityService.Application.Services;

public sealed class IdentityOperationRetryWorkerService(
    IServiceProvider provider,
    IEventInboxMessageManager inboxManager,
    IdentityRetrySettings retrySettings,
    ILogger<IdentityOperationRetryWorkerService> logger
) : ApplicationServiceBase(provider)
{
    public async Task RetryDueRequestsAsync(CancellationToken cancellationToken)
    {
        try
        {
            await ResetStaleStartedInboxMessagesAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Identity operation retry worker cancelled");
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Identity operation retry worker encountered an error");
            throw;
        }
    }

    /// <summary>
    /// Resets EventInboxMessage records that are stuck in 'Started' status beyond the stale
    /// threshold back to 'Failed', so the next broker re-delivery can attempt processing.
    /// This handles the case where a handler crashed after inserting the inbox record but
    /// before calling CompleteAsync.
    /// </summary>
    private async Task ResetStaleStartedInboxMessagesAsync(CancellationToken cancellationToken)
    {
        var staleThreshold = DateTime.UtcNow.AddMinutes(-retrySettings.StaleInboxMessageThresholdMinutes);
        var updated = await inboxManager.ResetStaleStartedMessagesAsync(staleThreshold, cancellationToken);

        if (updated > 0)
        {
            logger.LogWarning(
                "Identity retry worker reset {Count} stale inbox message(s) from 'Started' to 'Failed'. " +
                "These will be re-processed on next broker re-delivery.",
                updated);
        }
        else
        {
            logger.LogDebug("Identity retry worker: no stale inbox messages found. Batch size: {BatchSize}", retrySettings.BatchSize);
        }
    }
}
