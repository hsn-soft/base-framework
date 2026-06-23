using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.TextNormalizerService.Configuration.Providers.Outline;
using Hhs.TextNormalizerService.Entities;
using Hhs.TextNormalizerService.Mongo;
using Hhs.TextNormalizerService.Providers;
using Hhs.TextNormalizerService.Providers.Outline;
using MongoDB.Driver;

using Hhs.Shared.Configuration;

namespace Hhs.TextNormalizerService.Services;

public sealed class OutlineProviderPollingAppService(
    NormalizerMongoContext context,
    IOutlineProviderResolver outlineProviderResolver,
    IEventBus eventBus,
    ILogger<OutlineProviderPollingAppService> logger,
    OutlinePollingSettings pollingSettings)
{
    public async Task PollDueOutlineRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await PollCustomerRequestsAsync(now, cancellationToken);
        await PollAnalysisRequestsAsync(now, cancellationToken);
    }

    private async Task PollCustomerRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var requests = await context.CustomerContentNormalizedRequests
            .Find(x =>
                x.Status == StatusNames.OutlineProviderPolling &&
                x.NextOutlinePollAtUtc != null &&
                x.NextOutlinePollAtUtc <= now &&
                x.OutlineProviderTrackId != null)
            .Limit(50)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                var claimResult = await ClaimDueCustomerPollingAsync(
                    request.Id,
                    now,
                    cancellationToken);

                if (claimResult.ModifiedCount == 0)
                    continue;

                if (request.OutlinePollingCount >= request.MaxOutlinePollingCount)
                {
                    request.Status = StatusNames.Failed;
                    request.LastError = ErrorMessages.OutlineProviderPollingTimeout;
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceCustomerAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        CorrelationId = request.CorrelationId,
                        RefContentId = request.CustomerContentId,
                        RefContentType = ContentType.CustomerContent,
                        Step = EventNames.OutlineProviderPollingStarted,
                        ErrorMessage = request.LastError,
                        Retryable = false
                    }, cancellationToken);

                    continue;
                }

                var provider = outlineProviderResolver.Resolve(SubscriptionScopeRegistry.GetOutlineProviderKey(request.ScopeKey));

                var status = await provider.GetStatusAsync
                (
                    new OutlineStatusRequest
                    {
                        ProviderTrackId = request.OutlineProviderTrackId
                                          ?? throw new InvalidOperationException()
                    }, cancellationToken
                );

                if (status.IsProcessFailed)
                {
                    request.Status = StatusNames.Failed;
                    request.LastError = status.ErrorMessage ?? ErrorMessages.OutlineProviderFailed;
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceCustomerAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        CorrelationId = request.CorrelationId,
                        RefContentId = request.CustomerContentId,
                        RefContentType = ContentType.CustomerContent,
                        Step = EventNames.OutlineProviderPollingStarted,
                        ErrorMessage = request.LastError,
                        Retryable = false
                    }, cancellationToken);

                    continue;
                }

                if (!status.IsProcessed)
                {
                    request.OutlinePollingCount++;
                    request.NextOutlinePollAtUtc = DateTime.UtcNow.AddSeconds(pollingSettings.IntervalSeconds);
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceCustomerAsync(request, cancellationToken);
                    continue;
                }

                request.OutlinePollingCount++;
                request.NextOutlinePollAtUtc = null;
                request.Status = StatusNames.OutlineProviderCompleted;
                request.OutlineStatus = StatusNames.ProviderCompleted;
                request.CurrentStep = EventNames.OutlineProviderCompleted;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceCustomerAsync(request, cancellationToken);

                if (string.IsNullOrWhiteSpace(status.OutlinedData))
                    throw new InvalidOperationException("Outline provider completed but script is empty.");

                await eventBus.PublishAsync(new OutlineProviderCompletedEto
                {
                    RefContentType = ContentType.CustomerContent,
                    CorrelationId = request.CorrelationId,
                    NormalizedRequestId = request.Id,
                    Script = status.OutlinedData!
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                request.OutlinePollingCount++;
                request.LastError = ex.Message;
                request.UpdatedAtUtc = DateTime.UtcNow;

                if (request.OutlinePollingCount >= request.MaxOutlinePollingCount)
                {
                    request.Status = StatusNames.Failed;
                    request.OutlineStatus = StatusNames.Failed;
                    request.CurrentStep = EventNames.OutlineProviderPollingStarted;
                    request.NextOutlinePollAtUtc = null;

                    await ReplaceCustomerAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        CorrelationId = request.CorrelationId,
                        RefContentId = request.CustomerContentId,
                        RefContentType = ContentType.CustomerContent,
                        Step = EventNames.OutlineProviderPollingStarted,
                        ErrorMessage = ex.Message,
                        Retryable = false
                    }, cancellationToken);
                }
                else
                {
                    request.Status = StatusNames.OutlineProviderPolling;
                    request.OutlineStatus = StatusNames.Polling;
                    request.CurrentStep = EventNames.OutlineProviderPollingStarted;
                    request.NextOutlinePollAtUtc = DateTime.UtcNow.AddSeconds(pollingSettings.BackoffIntervalSeconds);

                    await ReplaceCustomerAsync(request, cancellationToken);
                }

                logger.LogError(
                    ex,
                    "Customer outline polling failed. CustomerContentId: {CustomerContentId}",
                    request.CustomerContentId);
            }
        }
    }

    private async Task PollAnalysisRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var filter = Builders<AnalysisContentNormalizedRequest>.Filter.ElemMatch(
            x => x.Items,
            i =>
                i.OutlineStatus == StatusNames.Polling &&
                i.NextOutlinePollAtUtc <= now &&
                i.OutlineProviderTrackId != null);

        var requests = await context.AnalysisContentNormalizedRequests
            .Find(filter)
            .Limit(50)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            var pollingItems = request.Items
                .Where(i =>
                    i.OutlineStatus == StatusNames.Polling &&
                    i.NextOutlinePollAtUtc <= now &&
                    i.OutlineProviderTrackId != null)
                .OrderBy(i => i.SortOrder)
                .ToList();

            foreach (var item in pollingItems)
            {
                try
                {
                    var claimUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", DateTime.UtcNow.AddSeconds(pollingSettings.IntervalSeconds))
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                        .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                    var claimResult = await UpdateDuePollingAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        now,
                        claimUpdate,
                        cancellationToken);

                    if (claimResult.ModifiedCount == 0)
                        continue;

                    if (item.OutlinePollingCount >= item.MaxOutlinePollingCount)
                    {
                        await FailAnalysisPollingItemAsync(
                            request,
                            item,
                            ErrorMessages.OutlineProviderPollingTimeout,
                            false,
                            cancellationToken);

                        continue;
                    }

                    var provider = outlineProviderResolver.Resolve(SubscriptionScopeRegistry.GetOutlineProviderKey(request.ScopeKey));

                    var status = await provider.GetStatusAsync
                    (
                        new OutlineStatusRequest
                        {
                            ProviderTrackId = item.OutlineProviderTrackId
                                              ?? throw new InvalidOperationException()
                        }, cancellationToken
                    );

                    if (status.IsProcessFailed)
                    {
                        await FailAnalysisPollingItemAsync(
                            request,
                            item,
                            status.ErrorMessage ?? "Outline provider failed.",
                            false,
                            cancellationToken);

                        continue;
                    }

                    if (!status.IsProcessed)
                    {
                        var update = Builders<AnalysisContentNormalizedRequest>.Update
                            .Inc($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlinePollingCount)}", 1)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", DateTime.UtcNow.AddSeconds(pollingSettings.ErrorRescheduleDelaySeconds))
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                        await UpdateAnalysisItemAsync(
                            request.Id,
                            item.CustomerContentId,
                            update,
                            cancellationToken);

                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(status.OutlinedData))
                        throw new InvalidOperationException("Outline provider completed but script is empty.");

                    var completedUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                        .Inc($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlinePollingCount)}", 1)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.OutlineProviderCompleted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.OutlineProviderCompleted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", (DateTime?)null)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.ProviderCompleted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                        .Set(x => x.CurrentStep, EventNames.OutlineProviderCompleted)
                        .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                    await UpdateAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        completedUpdate,
                        cancellationToken);

                    await eventBus.PublishAsync(new OutlineProviderCompletedEto
                    {
                        RefContentIdForItem = item.CustomerContentId,
                        RefContentType = ContentType.AnalysisContent,
                        CorrelationId = request.CorrelationId,
                        NormalizedRequestId = request.Id,
                        Script = status.OutlinedData!
                    }, cancellationToken);
                }
                catch (Exception ex)
                {
                    int nextCount = item.OutlinePollingCount + 1;

                    if (nextCount >= item.MaxOutlinePollingCount)
                    {
                        await FailAnalysisPollingItemAsync(
                            request,
                            item,
                            ex.Message,
                            false,
                            cancellationToken);

                        logger.LogError(
                            ex,
                            "Analysis outline item polling failed permanently. AnalysisContentId: {AnalysisContentId}, CustomerContentId: {CustomerContentId}",
                            request.AnalysisContentId,
                            item.CustomerContentId);

                        continue;
                    }

                    var update = Builders<AnalysisContentNormalizedRequest>.Update
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlinePollingCount)}", nextCount)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.OutlineProviderPolling)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.OutlineProviderPollingStarted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Polling)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", ex.Message)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", DateTime.UtcNow.AddSeconds(pollingSettings.ErrorRescheduleDelaySeconds))
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
                        .Set(x => x.Status, StatusNames.OutlineProviderPolling)
                        .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted)
                        .Set(x => x.LastError, ex.Message)
                        .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

                    await UpdateAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        update,
                        cancellationToken);

                    logger.LogError(
                        ex,
                        "Analysis outline item polling failed. AnalysisContentId: {AnalysisContentId}, CustomerContentId: {CustomerContentId}",
                        request.AnalysisContentId,
                        item.CustomerContentId);
                }
            }
        }
    }

    private Task<UpdateResult> UpdateDuePollingAnalysisItemAsync(
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
                    i.OutlineStatus == StatusNames.Polling &&
                    i.NextOutlinePollAtUtc != null &&
                    i.NextOutlinePollAtUtc <= now &&
                    i.OutlineProviderTrackId != null));

        return context.AnalysisContentNormalizedRequests.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }

    private async Task FailAnalysisPollingItemAsync(
        AnalysisContentNormalizedRequest request,
        AnalysisNormalizedItem item,
        string errorMessage,
        bool retryable,
        CancellationToken cancellationToken)
    {
        var update = Builders<AnalysisContentNormalizedRequest>.Update
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", StatusNames.Failed)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.OutlineProviderPollingStarted)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", StatusNames.Failed)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", errorMessage)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", (DateTime?)null)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.UpdatedAtUtc)}", DateTime.UtcNow)
            .Set(x => x.Status, StatusNames.Failed)
            .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted)
            .Set(x => x.LastError, errorMessage)
            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow);

        await UpdateAnalysisItemAsync(
            request.Id,
            item.CustomerContentId,
            update,
            cancellationToken);

        await eventBus.PublishAsync(new StepFailedEto
        {
            CorrelationId = request.CorrelationId,
            RefContentId = request.AnalysisContentId,
            RefContentType = ContentType.AnalysisContent,
            Step = EventNames.OutlineProviderPollingStarted,
            ErrorMessage = errorMessage,
            Retryable = retryable
        }, cancellationToken);
    }

    private Task UpdateAnalysisItemAsync(
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

        return context.AnalysisContentNormalizedRequests.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }

    private Task ReplaceCustomerAsync(
        CustomerContentNormalizedRequest request,
        CancellationToken cancellationToken)
    {
        return context.CustomerContentNormalizedRequests.ReplaceOneAsync(
            x => x.Id == request.Id,
            request,
            cancellationToken: cancellationToken);
    }

    private Task<UpdateResult> ClaimDueCustomerPollingAsync(
        Guid customerRequestId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return context.CustomerContentNormalizedRequests.UpdateOneAsync(
            x =>
                x.Id == customerRequestId &&
                x.Status == StatusNames.OutlineProviderPolling &&
                x.NextOutlinePollAtUtc != null &&
                x.NextOutlinePollAtUtc <= now &&
                x.OutlineProviderTrackId != null,
            Builders<CustomerContentNormalizedRequest>.Update
                .Set(x => x.NextOutlinePollAtUtc, DateTime.UtcNow.AddSeconds(pollingSettings.ErrorRescheduleDelaySeconds))
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
            cancellationToken: cancellationToken);
    }
}