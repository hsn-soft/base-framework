using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.TextNormalizerService.Configuration;
using Hhs.TextNormalizerService.Entities;
using Hhs.TextNormalizerService.Mongo;
using MongoDB.Driver;

using Hhs.Shared.Configuration;

namespace Hhs.TextNormalizerService.Services;

public sealed class NormalizerRetryAppService(
    NormalizerMongoContext context,
    IEventBus eventBus,
    NormalizerRetrySettings retrySettings)
{
    private readonly NormalizerRetrySettings _retrySettings = retrySettings;

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
                x.Status == StatusNames.WaitingRetry &&
                x.NextRetryAtUtc != null &&
                x.NextRetryAtUtc <= now)
            .Limit(_retrySettings.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                if (request.CurrentStep == EventNames.OutlineProviderPollingStarted)
                {
                    await context.CustomerRequests.UpdateOneAsync(
                        x => x.Id == request.Id && x.Status == StatusNames.WaitingRetry,
                        Builders<CustomerContentNormalizedRequest>.Update
                            .Set(x => x.Status, StatusNames.OutlineProviderPolling)
                            .Set(x => x.OutlineStatus, StatusNames.Polling)
                            .Set(x => x.NextOutlinePollAtUtc, DateTime.UtcNow)
                            .Set(x => x.NextRetryAtUtc, (DateTime?)null)
                            .Set(x => x.LastError, null)
                            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
                        cancellationToken: cancellationToken);

                    continue;
                }

                var claimResult = await context.CustomerRequests.UpdateOneAsync(
                    x =>
                        x.Id == request.Id &&
                        x.Status == StatusNames.WaitingRetry &&
                        x.NextRetryAtUtc != null &&
                        x.NextRetryAtUtc <= now,
                    Builders<CustomerContentNormalizedRequest>.Update
                        .Set(x => x.NextRetryAtUtc, DateTime.UtcNow.AddSeconds(_retrySettings.ClaimFailRescheduleDelaySeconds))
                        .Set(x => x.LastError, null)
                        .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
                    cancellationToken: cancellationToken);

                if (claimResult.ModifiedCount == 0)
                    continue;

                if (request.CurrentStep == EventNames.CustomerContentScrapingStarted)
                {
                    await eventBus.PublishAsync(new CustomerContentScrapingStartedEto { RefContentId = request.CustomerContentId, RefContentType = ContentType.CustomerContent, CorrelationId = request.CorrelationId }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.CustomerContentOutlineStarted)
                {
                    await eventBus.PublishAsync(new CustomerContentOutlineStartedEto { RefContentId = request.CustomerContentId, RefContentType = ContentType.CustomerContent, CorrelationId = request.CorrelationId }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.OutlineProviderRequestStarted)
                {
                    await eventBus.PublishAsync(new OutlineProviderRequestStartedEto
                    {
                        RefContentId = request.CustomerContentId,
                        RefContentType = ContentType.CustomerContent,
                        CorrelationId = request.CorrelationId,
                        NormalizedRequestId = request.Id,
                        ProviderKey = SubscriptionScopeRegistry.GetOutlineProviderKey(request.ScopeKey),
                        InputText = request.ScrapingResult?.Text
                                    ?? throw new InvalidOperationException("ScrapingResult.Text is required.")
                    }, cancellationToken);
                }
                else
                {
                    await context.CustomerRequests.UpdateOneAsync(
                        x => x.Id == request.Id,
                        Builders<CustomerContentNormalizedRequest>.Update
                            .Set(x => x.Status, StatusNames.Failed)
                            .Set(x => x.LastError, $"Unsupported customer retry step: {request.CurrentStep}")
                            .Set(x => x.NextRetryAtUtc, (DateTime?)null)
                            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
                        cancellationToken: cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        CorrelationId = request.CorrelationId,
                        RefContentId = request.CustomerContentId,
                        RefContentType = ContentType.CustomerContent,
                        Step = request.CurrentStep,
                        ErrorMessage = $"Unsupported customer retry step: {request.CurrentStep}",
                        Retryable = false
                    }, cancellationToken);
                }
            }
            catch
            {
                await context.CustomerRequests.UpdateOneAsync(
                    x => x.Id == request.Id,
                    Builders<CustomerContentNormalizedRequest>.Update
                        .Set(x => x.Status, StatusNames.WaitingRetry)
                        .Set(x => x.NextRetryAtUtc, DateTime.UtcNow.AddSeconds(_retrySettings.ClaimFailRescheduleDelaySeconds))
                        .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
                    cancellationToken: cancellationToken);

                throw;
            }
        }
    }

    private async Task RetryAnalysisRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var filter = Builders<AnalysisContentNormalizedRequest>.Filter.ElemMatch(
            x => x.Items,
            i =>
                i.Status == StatusNames.WaitingRetry &&
                i.NextRetryAtUtc != null &&
                i.NextRetryAtUtc <= now);

        var requests = await context.AnalysisRequests
            .Find(filter)
            .Limit(_retrySettings.BatchSize)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            var items = request.Items
                .Where(x =>
                    x.Status == StatusNames.WaitingRetry &&
                    x.NextRetryAtUtc != null &&
                    x.NextRetryAtUtc <= now)
                .OrderBy(x => x.SortOrder)
                .ToList();

            foreach (var item in items)
            {
                try
                {
                    if (item.CurrentStep == EventNames.OutlineProviderPollingStarted)
                    {
                        var pollingClaim = Builders<AnalysisContentNormalizedRequest>.Update
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.OutlineProviderPolling)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Polling)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", DateTime.UtcNow)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", (DateTime?)null)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", (string?)null)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                            .Set(x => x.Status, StatusNames.OutlineProviderPolling)
                            .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted)
                            .Set(x => x.LastError, null)
                            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                        var result = await UpdateDueRetryAnalysisItemAsync(
                            request.Id,
                            item.CustomerContentId,
                            now,
                            pollingClaim,
                            cancellationToken);

                        if (result.ModifiedCount == 0)
                            continue;

                        continue;
                    }

                    var claimUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", DateTime.UtcNow.AddSeconds(_retrySettings.ClaimFailRescheduleDelaySeconds))
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                        .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                    var claimResult = await UpdateDueRetryAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        now,
                        claimUpdate,
                        cancellationToken);

                    if (claimResult.ModifiedCount == 0)
                        continue;

                    if (item.CurrentStep == EventNames.AnalysisItemScrapingStarted)
                    {
                        await eventBus.PublishAsync(new AnalysisItemScrapingStartedEto
                        {
                            RefContentId = request.AnalysisContentId,
                            RefContentIdForItem = item.CustomerContentId,
                            RefContentType = ContentType.AnalysisContent,
                            CorrelationId = request.CorrelationId,
                            SortOrder = item.SortOrder
                        }, cancellationToken);
                    }
                    else if (item.CurrentStep == EventNames.AnalysisItemOutlineStarted)
                    {
                        await eventBus.PublishAsync(new AnalysisItemOutlineStartedEto
                        {
                            RefContentId = request.AnalysisContentId,
                            RefContentIdForItem = item.CustomerContentId,
                            RefContentType = ContentType.AnalysisContent,
                            CorrelationId = request.CorrelationId,
                            SortOrder = item.SortOrder
                        }, cancellationToken);
                    }
                    else if (item.CurrentStep == EventNames.OutlineProviderRequestStarted)
                    {
                        await eventBus.PublishAsync(new OutlineProviderRequestStartedEto
                        {
                            RefContentId = request.AnalysisContentId,
                            RefContentIdForItem = item.CustomerContentId,
                            RefContentType = ContentType.AnalysisContent,
                            CorrelationId = request.CorrelationId,
                            NormalizedRequestId = request.Id,
                            ProviderKey = SubscriptionScopeRegistry.GetOutlineProviderKey(request.ScopeKey),
                            SortOrder = item.SortOrder,
                            InputText = item.ScrapingResult?.Text
                                        ?? throw new InvalidOperationException("ScrapingResult.Text is required.")
                        }, cancellationToken);
                    }
                    else
                    {
                        var failUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.Failed)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", $"Unsupported analysis retry step: {item.CurrentStep}")
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", (DateTime?)null)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                            .Set(x => x.Status, StatusNames.Failed)
                            .Set(x => x.LastError, $"Unsupported analysis retry step: {item.CurrentStep}")
                            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                        await UpdateAnalysisItemAsync(
                            request.Id,
                            item.CustomerContentId,
                            failUpdate,
                            cancellationToken);
                    }
                }
                catch
                {
                    var retryAgainUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.WaitingRetry)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", DateTime.UtcNow.AddSeconds(_retrySettings.ClaimFailRescheduleDelaySeconds))
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                        .Set(x => x.Status, StatusNames.WaitingRetry)
                        .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                    await UpdateAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        retryAgainUpdate,
                        cancellationToken);

                    throw;
                }
            }
        }
    }

    private Task<UpdateResult> UpdateDueRetryAnalysisItemAsync(
        Guid analysisRequestId,
        Guid customerContentId,
        DateTime now,
        UpdateDefinition<AnalysisContentNormalizedRequest> update,
        CancellationToken cancellationToken)
    {
        var filter = Builders<AnalysisContentNormalizedRequest>.Filter.And(
            Builders<AnalysisContentNormalizedRequest>.Filter.Eq(x => x.Id, analysisRequestId),
            Builders<AnalysisContentNormalizedRequest>.Filter.ElemMatch(
                x => x.Items,
                i =>
                    i.CustomerContentId == customerContentId &&
                    i.Status == StatusNames.WaitingRetry &&
                    i.NextRetryAtUtc != null &&
                    i.NextRetryAtUtc <= now));

        return context.AnalysisRequests.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }

    private Task<UpdateResult> UpdateAnalysisItemAsync(
        Guid analysisRequestId,
        Guid customerContentId,
        UpdateDefinition<AnalysisContentNormalizedRequest> update,
        CancellationToken cancellationToken)
    {
        var filter = Builders<AnalysisContentNormalizedRequest>.Filter.And(
            Builders<AnalysisContentNormalizedRequest>.Filter.Eq(x => x.Id, analysisRequestId),
            Builders<AnalysisContentNormalizedRequest>.Filter.ElemMatch(
                x => x.Items,
                i => i.CustomerContentId == customerContentId));

        return context.AnalysisRequests.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }
}