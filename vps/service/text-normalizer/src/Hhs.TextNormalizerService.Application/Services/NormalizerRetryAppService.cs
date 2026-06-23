using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.TextNormalizerService.Domain.Configuration;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.MongoDb.Context;
using HsnSoft.Base.EventBus;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class NormalizerRetryAppService(
    IServiceProvider provider,
    TextNormalizerServiceDbContext context,
    IEventBus eventBus,
    NormalizerRetrySettings retrySettings) : ApplicationServiceBase(provider)
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
        var requests = await context.CustomerContentNormalizedRequests
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
                    await context.CustomerContentNormalizedRequests.UpdateOneAsync(
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

                var claimResult = await context.CustomerContentNormalizedRequests.UpdateOneAsync(
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
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        eventMessage: new CustomerContentScrapingStartedEto { CustomerContentId = request.CustomerContentId, }
                    );
                }
                else if (request.CurrentStep == EventNames.CustomerContentOutlineStarted)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        eventMessage: new CustomerContentOutlineStartedEto { CustomerContentId = request.CustomerContentId }
                    );
                }
                else if (request.CurrentStep == EventNames.OutlineProviderRequestStarted)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        eventMessage: new OutlineProviderRequestStartedEto
                        {
                            RefContentType = ContentType.CustomerContent,
                            NormalizedRequestId = request.Id,
                            ScopeKey = request.ScopeKey,
                            InputText = request.ScrapingResult?.Text
                                        ?? throw new InvalidOperationException("ScrapingResult.Text is required.")
                        }
                    );
                }
                else
                {
                    await context.CustomerContentNormalizedRequests.UpdateOneAsync(
                        x => x.Id == request.Id,
                        Builders<CustomerContentNormalizedRequest>.Update
                            .Set(x => x.Status, StatusNames.Failed)
                            .Set(x => x.LastError, $"Unsupported customer retry step: {request.CurrentStep}")
                            .Set(x => x.NextRetryAtUtc, (DateTime?)null)
                            .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
                        cancellationToken: cancellationToken);

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = request.CustomerContentId,
                            RefContentType = ContentType.CustomerContent,
                            Step = request.CurrentStep,
                            ErrorMessage = $"Unsupported customer retry step: {request.CurrentStep}",
                            Retryable = false
                        }
                    );
                }
            }
            catch
            {
                await context.CustomerContentNormalizedRequests.UpdateOneAsync(
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

        var requests = await context.AnalysisContentNormalizedRequests
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
                        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                            eventMessage: new AnalysisItemScrapingStartedEto { AnalysisContentId = request.AnalysisContentId }
                        );
                    }
                    else if (item.CurrentStep == EventNames.AnalysisItemOutlineStarted)
                    {
                        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                            eventMessage: new AnalysisItemOutlineStartedEto { AnalysisContentId = request.AnalysisContentId }
                        );
                    }
                    else if (item.CurrentStep == EventNames.OutlineProviderRequestStarted)
                    {
                        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                            eventMessage: new OutlineProviderRequestStartedEto
                            {
                                CustomerContentIdForItem = item.CustomerContentId,
                                RefContentType = ContentType.AnalysisContent,
                                NormalizedRequestId = request.Id,
                                ScopeKey = request.ScopeKey,
                                InputText = item.ScrapingResult?.Text
                                            ?? throw new InvalidOperationException("ScrapingResult.Text is required.")
                            }
                        );
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

        return context.AnalysisContentNormalizedRequests.UpdateOneAsync(
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

        return context.AnalysisContentNormalizedRequests.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
    }
}