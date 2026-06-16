using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.TextNormalizerService.Mongo;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Services;

public sealed class NormalizerRetryAppService(
    NormalizerMongoContext context,
    IEventBus eventBus)
{
    public async Task RetryDueRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await RetryCustomerRequestsAsync(now, cancellationToken);
        await RetryAnalysisRequestsAsync(now, cancellationToken);
    }

    private async Task RetryCustomerRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var requests = await context.CustomerRequests
            .Find(x =>
                x.Status == "WAITING_RETRY" &&
                x.NextRetryAtUtc != null &&
                x.NextRetryAtUtc <= now)
            .Limit(50)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                var retryHandledByPolling = false;

                if (request.CurrentStep == EventNames.CustomerScrapingStarted)
                {
                    await eventBus.PublishAsync(new CustomerScrapingStartedEvent { CustomerContentId = request.CustomerContentId, ContentProcessType = ContentProcessTypes.CustomerContent, CorrelationId = request.CorrelationId }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.CustomerOutlineStarted)
                {
                    await eventBus.PublishAsync(new CustomerOutlineStartedEvent { CustomerContentId = request.CustomerContentId, ContentProcessType = ContentProcessTypes.CustomerContent, CorrelationId = request.CorrelationId }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.OutlineProviderRequestStarted)
                {
                    await eventBus.PublishAsync(new OutlineProviderRequestStartedEvent
                    {
                        CustomerContentId = request.CustomerContentId,
                        ContentProcessType = ContentProcessTypes.CustomerContent,
                        CorrelationId = request.CorrelationId,
                        NormalizedRequestId = request.Id,
                        ProviderKey = request.OutlineProviderKey,
                        InputText = request.ScrapingResult?.Text
                                    ?? throw new InvalidOperationException("ScrapingResult.Text is required.")
                    }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.OutlineProviderPollingStarted)
                {
                    request.Status = "OUTLINE_PROVIDER_POLLING";
                    request.OutlineStatus = "POLLING";
                    request.NextOutlinePollAtUtc = DateTime.UtcNow;
                    retryHandledByPolling = true;
                }
                else
                {
                    throw new InvalidOperationException($"Unsupported customer retry step: {request.CurrentStep}");
                }

                if (!retryHandledByPolling)
                {
                    request.Status = "RETRY_PUBLISHED";
                }

                request.NextRetryAtUtc = null;
                request.LastError = null;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await context.CustomerRequests.ReplaceOneAsync(
                    x => x.Id == request.Id,
                    request,
                    cancellationToken: cancellationToken);
            }
            catch
            {
                request.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(1);
                request.UpdatedAtUtc = DateTime.UtcNow;

                await context.CustomerRequests.ReplaceOneAsync(
                    x => x.Id == request.Id,
                    request,
                    cancellationToken: cancellationToken);

                throw;
            }
        }
    }

    private async Task RetryAnalysisRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var requests = await context.AnalysisRequests
            .Find(x => x.Status == "WAITING_RETRY")
            .Limit(50)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            var items = request.Items
                .Where(x =>
                    x.Status == "WAITING_RETRY" &&
                    x.NextRetryAtUtc != null &&
                    x.NextRetryAtUtc <= now)
                .OrderBy(x => x.SortOrder)
                .ToList();

            foreach (var item in items)
            {
                try
                {
                    var retryHandledByPolling = false;

                    if (item.CurrentStep == EventNames.AnalysisItemScrapingStarted)
                    {
                        await eventBus.PublishAsync(new AnalysisItemScrapingStartedEvent
                        {
                            AnalysisContentId = request.AnalysisContentId,
                            CustomerContentId = item.CustomerContentId,
                            ContentProcessType = ContentProcessTypes.AnalysisContent,
                            CorrelationId = request.CorrelationId,
                            SortOrder = item.SortOrder
                        }, cancellationToken);
                    }
                    else if (item.CurrentStep == EventNames.AnalysisItemOutlineStarted)
                    {
                        await eventBus.PublishAsync(new AnalysisItemOutlineStartedEvent
                        {
                            AnalysisContentId = request.AnalysisContentId,
                            CustomerContentId = item.CustomerContentId,
                            ContentProcessType = ContentProcessTypes.AnalysisContent,
                            CorrelationId = request.CorrelationId,
                            SortOrder = item.SortOrder
                        }, cancellationToken);
                    }
                    else if (item.CurrentStep == EventNames.OutlineProviderRequestStarted)
                    {
                        await eventBus.PublishAsync(new OutlineProviderRequestStartedEvent
                        {
                            AnalysisContentId = request.AnalysisContentId,
                            CustomerContentId = item.CustomerContentId,
                            CustomerContentIdForItem = item.CustomerContentId,
                            ContentProcessType = ContentProcessTypes.AnalysisContent,
                            CorrelationId = request.CorrelationId,
                            NormalizedRequestId = request.Id,
                            ProviderKey = request.OutlineProviderKey,
                            SortOrder = item.SortOrder,
                            InputText = item.ScrapingResult?.Text
                                        ?? throw new InvalidOperationException("ScrapingResult.Text is required.")
                        }, cancellationToken);
                    }
                    else if (item.CurrentStep == EventNames.OutlineProviderPollingStarted)
                    {
                        item.Status = "OUTLINE_PROVIDER_POLLING";
                        item.OutlineStatus = "POLLING";
                        item.NextOutlinePollAtUtc = DateTime.UtcNow;

                        request.Status = "OUTLINE_PROVIDER_POLLING";
                        request.CurrentStep = EventNames.OutlineProviderPollingStarted;

                        retryHandledByPolling = true;
                    }
                    else
                    {
                        throw new InvalidOperationException($"Unsupported analysis retry step: {item.CurrentStep}");
                    }

                    if (!retryHandledByPolling)
                    {
                        item.Status = "RETRY_PUBLISHED";
                    }

                    item.NextRetryAtUtc = null;
                    item.LastError = null;
                    item.UpdatedAtUtc = DateTime.UtcNow;

                    if (request.Items.All(x =>
                            x.Status is "RETRY_PUBLISHED" or "OUTLINE_PROVIDER_POLLING" or "COMPLETED" or "OUTLINE_COMPLETED"))
                    {
                        request.Status = request.Items.Any(x => x.Status == "OUTLINE_PROVIDER_POLLING")
                            ? "OUTLINE_PROVIDER_POLLING"
                            : "RETRY_PUBLISHED";
                    }

                    request.LastError = null;
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await context.AnalysisRequests.ReplaceOneAsync(
                        x => x.Id == request.Id,
                        request,
                        cancellationToken: cancellationToken);
                }
                catch
                {
                    item.NextRetryAtUtc = DateTime.UtcNow.AddMinutes(1);
                    item.UpdatedAtUtc = DateTime.UtcNow;

                    await context.AnalysisRequests.ReplaceOneAsync(
                        x => x.Id == request.Id,
                        request,
                        cancellationToken: cancellationToken);

                    throw;
                }
            }
        }
    }
}