using Hhs.Shared.Configuration;
using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.TextNormalizerService.Configuration;
using Hhs.TextNormalizerService.Entities;
using Hhs.TextNormalizerService.Mongo;
using MongoDB.Driver;

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
                x.Status == "WAITING_RETRY" &&
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
                        x => x.Id == request.Id && x.Status == "WAITING_RETRY",
                        Builders<CustomerContentNormalizedRequest>.Update
                            .Set(x => x.Status, "OUTLINE_PROVIDER_POLLING")
                            .Set(x => x.OutlineStatus, "POLLING")
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
                        x.Status == "WAITING_RETRY" &&
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
                    await eventBus.PublishAsync(new CustomerContentScrapingStartedEto { CustomerContentId = request.CustomerContentId, ContentProcessType = ContentProcessTypes.CustomerContent, CorrelationId = request.CorrelationId }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.CustomerContentOutlineStarted)
                {
                    await eventBus.PublishAsync(new CustomerContentOutlineStartedEto { CustomerContentId = request.CustomerContentId, ContentProcessType = ContentProcessTypes.CustomerContent, CorrelationId = request.CorrelationId }, cancellationToken);
                }
                else if (request.CurrentStep == EventNames.OutlineProviderRequestStarted)
                {
                    await eventBus.PublishAsync(new OutlineProviderRequestStartedEto
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
                else
                {
                    await context.CustomerRequests.UpdateOneAsync(
                        x => x.Id == request.Id,
                        Builders<CustomerContentNormalizedRequest>.Update
                            .Set(x => x.Status, "FAILED")
                            .Set(x => x.LastError, $"Unsupported customer retry step: {request.CurrentStep}")
                            .Set(x => x.NextRetryAtUtc, (DateTime?)null)
                            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
                        cancellationToken: cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        CorrelationId = request.CorrelationId,
                        CustomerContentId = request.CustomerContentId,
                        ContentProcessType = ContentProcessTypes.CustomerContent,
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
                        .Set(x => x.Status, "WAITING_RETRY")
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
                i.Status == "WAITING_RETRY" &&
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
                    x.Status == "WAITING_RETRY" &&
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
                            .Set("Items.$.Status", "OUTLINE_PROVIDER_POLLING")
                            .Set("Items.$.OutlineStatus", "POLLING")
                            .Set("Items.$.NextOutlinePollAtUtc", DateTime.UtcNow)
                            .Set("Items.$.NextRetryAtUtc", (DateTime?)null)
                            .Set("Items.$.LastError", (string?)null)
                            .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
                            .Set(x => x.Status, "OUTLINE_PROVIDER_POLLING")
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
                        .Set("Items.$.NextRetryAtUtc", DateTime.UtcNow.AddSeconds(_retrySettings.ClaimFailRescheduleDelaySeconds))
                        .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
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
                            AnalysisContentId = request.AnalysisContentId,
                            CustomerContentId = item.CustomerContentId,
                            ContentProcessType = ContentProcessTypes.AnalysisContent,
                            CorrelationId = request.CorrelationId,
                            SortOrder = item.SortOrder
                        }, cancellationToken);
                    }
                    else if (item.CurrentStep == EventNames.AnalysisItemOutlineStarted)
                    {
                        await eventBus.PublishAsync(new AnalysisItemOutlineStartedEto
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
                        await eventBus.PublishAsync(new OutlineProviderRequestStartedEto
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
                    else
                    {
                        var failUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                            .Set("Items.$.Status", "FAILED")
                            .Set("Items.$.LastError", $"Unsupported analysis retry step: {item.CurrentStep}")
                            .Set("Items.$.NextRetryAtUtc", (DateTime?)null)
                            .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
                            .Set(x => x.Status, "FAILED")
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
                        .Set("Items.$.Status", "WAITING_RETRY")
                        .Set("Items.$.NextRetryAtUtc", DateTime.UtcNow.AddSeconds(_retrySettings.ClaimFailRescheduleDelaySeconds))
                        .Set("Items.$.UpdatedAtUtc", DateTime.UtcNow)
                        .Set(x => x.Status, "WAITING_RETRY")
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
                    i.Status == "WAITING_RETRY" &&
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