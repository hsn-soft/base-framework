using System.Linq.Expressions;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Domain.Configuration;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.EventBus;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class NormalizerRetryAppService(
    IServiceProvider provider,
    IAnalysisContentNormalizedRequestRepository analysisRepository,
    ICustomerContentNormalizedRequestRepository customerRepository,
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
        var options = new ListQueryOptions<CustomerContentNormalizedRequest>
        {
            Filter = x => x.Status == StatusNames.WaitingRetry &&
                         x.NextRetryAtUtc != null &&
                         x.NextRetryAtUtc <= now,
            MaxResultCount = _retrySettings.BatchSize
        };

        var requests = await customerRepository
            .GetListAsync(options, cancellationToken)
            .ConfigureAwait(false);

        foreach (var request in requests)
        {
            try
            {
                if (request.CurrentStep == EventNames.OutlineProviderPollingStarted)
                {
                    var outlinePollingPredicate = (Expression<Func<CustomerContentNormalizedRequest, bool>>)(x =>
                        x.Id == request.Id &&
                        x.Status == StatusNames.WaitingRetry);

                    var outlinePollingUpdate = Builders<CustomerContentNormalizedRequest>.Update
                        .Set(x => x.Status, StatusNames.OutlineProviderPolling)
                        .Set(x => x.OutlineStatus, StatusNames.Polling)
                        .Set(x => x.NextOutlinePollAtUtc, DateTime.UtcNow)
                        .Set(x => x.NextRetryAtUtc, (DateTime?)null)
                        .Set(x => x.LastError, null);

                    await customerRepository.UpdateByExpressionAsync(
                        outlinePollingPredicate,
                        u => outlinePollingUpdate,
                        cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    continue;
                }

                var claimPredicate = (Expression<Func<CustomerContentNormalizedRequest, bool>>)(x =>
                    x.Id == request.Id &&
                    x.Status == StatusNames.WaitingRetry &&
                    x.NextRetryAtUtc != null &&
                    x.NextRetryAtUtc <= now);

                var claimUpdate = Builders<CustomerContentNormalizedRequest>.Update
                    .Set(x => x.NextRetryAtUtc, DateTime.UtcNow.AddSeconds(_retrySettings.ClaimFailRescheduleDelaySeconds))
                    .Set(x => x.LastError, null);

                var claimResult = await customerRepository.UpdateByExpressionAsync(
                    claimPredicate,
                    u => claimUpdate,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (claimResult == 0)
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
                    var failPredicate = (Expression<Func<CustomerContentNormalizedRequest, bool>>)(x => x.Id == request.Id);
                    var failUpdate = Builders<CustomerContentNormalizedRequest>.Update
                        .Set(x => x.Status, StatusNames.Failed)
                        .Set(x => x.LastError, $"Unsupported customer retry step: {request.CurrentStep}")
                        .Set(x => x.NextRetryAtUtc, (DateTime?)null);

                    await customerRepository.UpdateByExpressionAsync(
                        failPredicate,
                        u => failUpdate,
                        cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

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
                var exceptionPredicate = (Expression<Func<CustomerContentNormalizedRequest, bool>>)(x => x.Id == request.Id);
                var exceptionUpdate = Builders<CustomerContentNormalizedRequest>.Update
                    .Set(x => x.Status, StatusNames.WaitingRetry)
                    .Set(x => x.NextRetryAtUtc, DateTime.UtcNow.AddSeconds(_retrySettings.ClaimFailRescheduleDelaySeconds));

                await customerRepository.UpdateByExpressionAsync(
                    exceptionPredicate,
                    u => exceptionUpdate,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                throw;
            }
        }
    }

    private async Task RetryAnalysisRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var options = new ListQueryOptions<AnalysisContentNormalizedRequest>
        {
            Filter = x => x.Items.Any(i =>
                i.Status == StatusNames.WaitingRetry &&
                i.NextRetryAtUtc != null &&
                i.NextRetryAtUtc <= now),
            MaxResultCount = _retrySettings.BatchSize
        };

        var requests = await analysisRepository
            .GetListAsync(options, cancellationToken)
            .ConfigureAwait(false);

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
                            .Set(x => x.Status, StatusNames.OutlineProviderPolling)
                            .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted)
                            .Set(x => x.LastError, null);

                        var result = await UpdateDueRetryAnalysisItemAsync(
                            request.Id,
                            item.CustomerContentId,
                            now,
                            pollingClaim,
                            cancellationToken);

                        if (result == 0)
                            continue;

                        continue;
                    }

                    var claimUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", DateTime.UtcNow.AddSeconds(_retrySettings.ClaimFailRescheduleDelaySeconds));

                    var claimResult = await UpdateDueRetryAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        now,
                        claimUpdate,
                        cancellationToken);

                    if (claimResult == 0)
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
                            .Set(x => x.Status, StatusNames.Failed)
                            .Set(x => x.LastError, $"Unsupported analysis retry step: {item.CurrentStep}");

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
                        .Set(x => x.Status, StatusNames.WaitingRetry);

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

    private Task<long> UpdateDueRetryAnalysisItemAsync(
        Guid analysisRequestId,
        Guid customerContentId,
        DateTime now,
        UpdateDefinition<AnalysisContentNormalizedRequest> update,
        CancellationToken cancellationToken)
    {
        var predicate = (Expression<Func<AnalysisContentNormalizedRequest, bool>>)(x =>
            x.Id == analysisRequestId &&
            x.Items.Any(i =>
                i.CustomerContentId == customerContentId &&
                i.Status == StatusNames.WaitingRetry &&
                i.NextRetryAtUtc != null &&
                i.NextRetryAtUtc <= now));

        return analysisRepository.UpdateByExpressionAsync(
            predicate,
            u => update,
            cancellationToken: cancellationToken);
    }

    private Task<long> UpdateAnalysisItemAsync(
        Guid analysisRequestId,
        Guid customerContentId,
        UpdateDefinition<AnalysisContentNormalizedRequest> update,
        CancellationToken cancellationToken)
    {
        var predicate = (Expression<Func<AnalysisContentNormalizedRequest, bool>>)(x =>
            x.Id == analysisRequestId &&
            x.Items.Any(i => i.CustomerContentId == customerContentId));

        return analysisRepository.UpdateByExpressionAsync(
            predicate,
            u => update,
            cancellationToken: cancellationToken);
    }
}