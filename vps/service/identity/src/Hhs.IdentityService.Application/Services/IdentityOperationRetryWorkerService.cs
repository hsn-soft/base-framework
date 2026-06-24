using Hhs.IdentityService.Domain.Configuration;
using Microsoft.Extensions.Logging;

namespace Hhs.IdentityService.Application.Services;

public sealed class IdentityOperationRetryWorkerService(
    IServiceProvider provider,
    IdentityRetrySettings retrySettings,
    ILogger<IdentityOperationRetryWorkerService> logger
) : ApplicationServiceBase(provider)
{
    public async Task RetryDueRequestsAsync(CancellationToken cancellationToken)
    {
        try
        {
            // ContentService does not have separate request tracking entities like TextNormalizer.
            // Retry logic is handled through event processing and status updates on ContentOperation entities.
            // This method serves as a placeholder for future retry implementations if needed.

            logger.LogDebug("Content operation retry worker executed. Batch size: {BatchSize}", retrySettings.BatchSize);

            await Task.CompletedTask;
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
}
