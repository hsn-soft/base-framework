using System.Linq.Expressions;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Domain.Configuration;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class NormalizerOperationRetryWorkerService(
    IServiceProvider provider,
    IAnalysisContentNormalizedRequestRepository analysisRepository,
    ICustomerContentNormalizedRequestRepository customerRepository,
    IEventInboxMessageManager inboxManager,
    ILogger<NormalizerOperationRetryWorkerService> logger,
    NormalizerOperationAppService normalizerOperationAppService,
    NormalizerRetrySettings retrySettings) : ApplicationServiceBase(provider)
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

    public async Task RetryDueRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        await ResetStaleStartedInboxMessagesAsync(now, cancellationToken);
        await AdvanceReadyAnalysisItemsToOutlineAsync(cancellationToken);
        await AdvanceReadyAnalysisContentsToResultAsync(cancellationToken);
        await RetryCustomerRequestsAsync(now, cancellationToken);
        await RetryAnalysisRequestsAsync(now, cancellationToken);
    }

    /// <summary>
    /// Fan-in gate: outline must not start for ANY item until ALL sibling items have reached a
    /// terminal scraping state (Completed or Failed). Re-derives readiness directly from the
    /// items' current DB state every tick instead of reacting to one item's completion event —
    /// immune to that specific event being lost to a redelivery race. No parent-level claim is
    /// needed here: this only fans an item out to AnalysisItemOutlineStartedEto while its own
    /// OutlineStatus is still NotStarted, and StartAnalysisItemOutlineAsync's own per-item CAS
    /// (NotStarted/WaitingRetry -&gt; Started) makes a duplicate fan-out across ticks a safe no-op.
    /// </summary>
    private async Task AdvanceReadyAnalysisItemsToOutlineAsync(CancellationToken cancellationToken)
    {
        var options = new ListQueryOptions<AnalysisContentNormalizedRequest>
        {
            Filter = x => x.Status != NormalizeStatusNames.Completed && x.Status != NormalizeStatusNames.Failed &&
                          x.Items.Any(i => i.ScrapingStatus == ScrapingStatusNames.Completed && i.OutlineStatus == OutlineStatusNames.NotStarted),
            MaxResultCount = retrySettings.BatchSize
        };

        var candidates = await analysisRepository.GetListAsync(options, cancellationToken).ConfigureAwait(false);

        foreach (var request in candidates)
        {
            try
            {
                bool allScraped = request.Items.All(x => x.ScrapingStatus is ScrapingStatusNames.Completed or ScrapingStatusNames.Failed);
                if (!allScraped) continue; // still waiting on at least one sibling's scraping

                var readyItems = request.Items
                    .Where(x => x.ScrapingStatus == ScrapingStatusNames.Completed && x.OutlineStatus == OutlineStatusNames.NotStarted)
                    .OrderBy(x => x.SortOrder)
                    .ToList();

                foreach (var item in readyItems)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new AnalysisItemOutlineStartedEto { AnalysisContentId = request.AnalysisContentId, CustomerContentIdForItem = item.CustomerContentId }
                    );
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ADVANCE_ANALYSIS_OUTLINE_START_FAILED: RequestId={RequestId}", request.Id);
            }
        }
    }

    /// <summary>
    /// Fan-in gate: the result must not be published until ALL sibling items have reached a
    /// terminal outline state (Completed or Failed). The parent's top-level Status field is
    /// exclusively owned by this method's atomic claim (Created by
    /// CreateAnalysisContentNormalizeRequestAsync, Completed/Failed only here) — every per-item
    /// operation elsewhere only ever touches Items.$.* fields, never the parent's own Status,
    /// so this Status!=Completed/Failed filter can never be short-circuited by an unrelated
    /// sibling's retry/poll/fail write.
    /// </summary>
    private async Task AdvanceReadyAnalysisContentsToResultAsync(CancellationToken cancellationToken)
    {
        var options = new ListQueryOptions<AnalysisContentNormalizedRequest>
        {
            Filter = x => x.Status != NormalizeStatusNames.Completed && x.Status != NormalizeStatusNames.Failed,
            MaxResultCount = retrySettings.BatchSize
        };

        var candidates = await analysisRepository.GetListAsync(options, cancellationToken).ConfigureAwait(false);

        foreach (var request in candidates)
        {
            try
            {
                if (request.Items.Count == 0) continue; // items not populated yet

                if (request.Items.Any(x => x.OutlineStatus == OutlineStatusNames.Failed || x.Status == NormalizeStatusNames.Failed))
                {
                    var failPredicate = (Expression<Func<AnalysisContentNormalizedRequest, bool>>)(x =>
                        x.Id == request.Id && x.Status != NormalizeStatusNames.Completed && x.Status != NormalizeStatusNames.Failed);
                    var failUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                        .Set(x => x.Status, NormalizeStatusNames.Failed)
                        .Set(x => x.CurrentStep, EventNames.AnalysisItemOutlineCompleted)
                        .Set(x => x.LastError, "One or more items failed during processing.");

                    var failClaimed = await analysisRepository.UpdateByExpressionAsync(failPredicate, u => failUpdate, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (failClaimed == 0) continue;

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.AnalysisItemOutlineCompleted,
                        reference: new { request.ScopeKey, RefContentId = request.AnalysisContentId, RefNormalizedRequestId = request.Id },
                        facility: EventNames.AnalysisItemOutlineCompleted,
                        correlationId: request.CorrelationId,
                        exception: null
                    ));

                    continue;
                }

                if (request.Items.Any(x => x.OutlineStatus != OutlineStatusNames.Completed))
                    continue; // still waiting on at least one sibling's outline

                var claimPredicate = (Expression<Func<AnalysisContentNormalizedRequest, bool>>)(x =>
                    x.Id == request.Id && x.Status != NormalizeStatusNames.Completed && x.Status != NormalizeStatusNames.Failed);
                var claimUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                    .Set(x => x.Status, NormalizeStatusNames.Completed)
                    .Set(x => x.CurrentStep, EventNames.NormalizerResultPublished)
                    .Set(x => x.LastError, (string)null);

                var claimed = await analysisRepository.UpdateByExpressionAsync(claimPredicate, u => claimUpdate, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (claimed == 0) continue; // already claimed by another tick/instance

                await normalizerOperationAppService.AppendIntroOutroToAnalysisItemsAsync(request, cancellationToken);

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: EventNames.NormalizerResultPublished,
                    reference: new { request.ScopeKey, RefContentId = request.AnalysisContentId, RefNormalizedRequestId = request.Id },
                    facility: EventNames.NormalizerResultPublished,
                    correlationId: request.CorrelationId,
                    exception: null
                ));

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: request.CorrelationId,
                    eventMessage: new NormalizerResultPublishedEto
                    {
                        RefContentType = ContentType.AnalysisContent,
                        RefContentId = request.AnalysisContentId,
                        NormalizeRequestId = request.Id,
                        NormalizeStatus = NormalizeStatusNames.Completed,
                        NormalizeCurrentStep = EventNames.NormalizerResultPublished
                    }
                );
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ADVANCE_ANALYSIS_RESULT_FAILED: RequestId={RequestId}", request.Id);
            }
        }
    }

    private async Task ResetStaleStartedInboxMessagesAsync(DateTime now, CancellationToken cancellationToken)
    {
        var staleThreshold = now.AddMinutes(-retrySettings.StaleInboxMessageThresholdMinutes);
        var updated = await inboxManager.ResetStaleStartedMessagesAsync(staleThreshold, cancellationToken);
        if (updated > 0)
        {
            logger.LogWarning(
                "Normalizer retry worker reset {Count} stale inbox message(s) from 'Started' to 'Failed'. " +
                "These will be re-processed on next broker re-delivery.",
                updated);
        }
    }

    private async Task RetryCustomerRequestsAsync(
        DateTime now,
        CancellationToken cancellationToken)
    {
        var options = new ListQueryOptions<CustomerContentNormalizedRequest>
        {
            Filter = x => x.Status == NormalizeStatusNames.WaitingRetry &&
                         x.NextRetryAtUtc != null &&
                         x.NextRetryAtUtc <= now,
            MaxResultCount = retrySettings.BatchSize
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
                        x.Status == NormalizeStatusNames.WaitingRetry);

                    var outlinePollingUpdate = Builders<CustomerContentNormalizedRequest>.Update
                        .Set(x => x.Status, NormalizeStatusNames.OutlineProviderRequestPolling)
                        .Set(x => x.OutlineStatus, OutlineStatusNames.Polling)
                        .Set(x => x.NextOutlinePollAtUtc, DateTime.UtcNow)
                        .Set(x => x.NextRetryAtUtc, (DateTime?)null)
                        .Set(x => x.LastError, null);

                    await customerRepository.UpdateByExpressionAsync(
                        outlinePollingPredicate,
                        u => outlinePollingUpdate,
                        cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.RetryScheduled,
                        reference: new { request.ScopeKey, RefContentId = request.CustomerContentId, RefNormalizedRequestId = request.Id, FailedStep = EventNames.OutlineProviderPollingStarted },
                        facility: EventNames.RetryScheduled,
                        correlationId: request.CorrelationId,
                        exception: null
                    ));

                    continue;
                }

                var claimPredicate = (Expression<Func<CustomerContentNormalizedRequest, bool>>)(x =>
                    x.Id == request.Id &&
                    x.Status == NormalizeStatusNames.WaitingRetry &&
                    x.NextRetryAtUtc != null &&
                    x.NextRetryAtUtc <= now);

                var claimUpdate = Builders<CustomerContentNormalizedRequest>.Update
                    .Set(x => x.NextRetryAtUtc, DateTime.UtcNow.AddSeconds(retrySettings.ClaimFailRescheduleDelaySeconds))
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
                        correlationId: request.CorrelationId,
                        eventMessage: new CustomerContentScrapingStartedEto { CustomerContentNormalizeRequestId = request.Id, }
                    );
                }
                else if (request.CurrentStep == EventNames.CustomerContentOutlineStarted)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new CustomerContentOutlineStartedEto { CustomerContentNormalizeRequestId = request.Id }
                    );
                }
                else if (request.CurrentStep == EventNames.OutlineProviderRequestStarted)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new OutlineProviderRequestStartedEto
                        {
                            RefContentType = ContentType.CustomerContent,
                            RefNormalizedRequestId = request.Id,
                            ScopeKey = request.ScopeKey,
                            InputText = request.ScrapingResult?.Details
                                        ?? throw new InvalidOperationException("ScrapingResult.Text is required.")
                        }
                    );
                }
                else
                {
                    var failPredicate = (Expression<Func<CustomerContentNormalizedRequest, bool>>)(x => x.Id == request.Id);
                    var failUpdate = Builders<CustomerContentNormalizedRequest>.Update
                        .Set(x => x.Status, NormalizeStatusNames.Failed)
                        .Set(x => x.LastError, $"Unsupported customer retry step: {request.CurrentStep}")
                        .Set(x => x.NextRetryAtUtc, (DateTime?)null);

                    await customerRepository.UpdateByExpressionAsync(
                        failPredicate,
                        u => failUpdate,
                        cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: request.CurrentStep,
                        reference: new { request.ScopeKey, RefContentId = request.CustomerContentId, RefNormalizedRequestId = request.Id },
                        facility: request.CurrentStep,
                        correlationId: request.CorrelationId,
                        exception: null
                    ));

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

                    continue;
                }

                _logger.FrameworkErrorLog(LogHelper.Generate(
                    message: EventNames.RetryScheduled,
                    reference: new { request.ScopeKey, RefContentId = request.CustomerContentId, RefNormalizedRequestId = request.Id, FailedStep = request.CurrentStep },
                    facility: EventNames.RetryScheduled,
                    correlationId: request.CorrelationId,
                    exception: null
                ));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "RETRY_CUSTOMER_REQUEST_FAILED: RequestId={RequestId}", request.Id);

                var exceptionPredicate = (Expression<Func<CustomerContentNormalizedRequest, bool>>)(x => x.Id == request.Id);
                var exceptionUpdate = Builders<CustomerContentNormalizedRequest>.Update
                    .Set(x => x.Status, NormalizeStatusNames.WaitingRetry)
                    .Set(x => x.NextRetryAtUtc, DateTime.UtcNow.AddSeconds(retrySettings.ClaimFailRescheduleDelaySeconds));

                await customerRepository.UpdateByExpressionAsync(
                    exceptionPredicate,
                    u => exceptionUpdate,
                    cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                _logger.FrameworkErrorLog(LogHelper.Generate(
                    message: EventNames.RetryScheduled,
                    reference: new { request.ScopeKey, RefContentId = request.CustomerContentId, RefNormalizedRequestId = request.Id, FailedStep = request.CurrentStep },
                    facility: EventNames.RetryScheduled,
                    correlationId: request.CorrelationId,
                    exception: ex
                ));
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
                i.Status == NormalizeStatusNames.WaitingRetry &&
                i.NextRetryAtUtc != null &&
                i.NextRetryAtUtc <= now),
            MaxResultCount = retrySettings.BatchSize
        };

        var requests = await analysisRepository
            .GetListAsync(options, cancellationToken)
            .ConfigureAwait(false);

        foreach (var request in requests)
        {
            var items = request.Items
                .Where(x =>
                    x.Status == NormalizeStatusNames.WaitingRetry &&
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
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.OutlineProviderRequestPolling)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", OutlineStatusNames.Polling)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", DateTime.UtcNow)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", (DateTime?)null)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", (string?)null);

                        var result = await UpdateDueRetryAnalysisItemAsync(
                            request.Id,
                            item.CustomerContentId,
                            now,
                            pollingClaim,
                            cancellationToken);

                        if (result == 0)
                            continue;

                        _logger.FrameworkErrorLog(LogHelper.Generate(
                            message: EventNames.RetryScheduled,
                            reference: new { request.ScopeKey, RefContentId = request.AnalysisContentId, RefNormalizedRequestId = request.Id, item.CustomerContentId, FailedStep = EventNames.OutlineProviderPollingStarted },
                            facility: EventNames.RetryScheduled,
                            correlationId: request.CorrelationId,
                            exception: null
                        ));

                        continue;
                    }

                    var claimUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", DateTime.UtcNow.AddSeconds(retrySettings.ClaimFailRescheduleDelaySeconds));

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
                            correlationId: request.CorrelationId,
                            eventMessage: new AnalysisItemScrapingStartedEto
                            {
                                AnalysisContentId = request.AnalysisContentId,
                                CustomerContentIdForItem = item.CustomerContentId
                            }
                        );
                    }
                    else if (item.CurrentStep == EventNames.AnalysisItemOutlineStarted)
                    {
                        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                            correlationId: request.CorrelationId,
                            eventMessage: new AnalysisItemOutlineStartedEto
                            {
                                AnalysisContentId = request.AnalysisContentId,
                                CustomerContentIdForItem = item.CustomerContentId
                            }
                        );
                    }
                    else if (item.CurrentStep == EventNames.OutlineProviderRequestStarted)
                    {
                        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                            correlationId: request.CorrelationId,
                            eventMessage: new OutlineProviderRequestStartedEto
                            {
                                CustomerContentIdForItem = item.CustomerContentId,
                                RefContentType = ContentType.AnalysisContent,
                                RefNormalizedRequestId = request.Id,
                                ScopeKey = request.ScopeKey,
                                InputText = item.ScrapingResult?.Details
                                            ?? throw new InvalidOperationException("ScrapingResult.Text is required.")
                            }
                        );
                    }
                    else
                    {
                        var failUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.Failed)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", $"Unsupported analysis retry step: {item.CurrentStep}")
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", (DateTime?)null);

                        await UpdateAnalysisItemAsync(
                            request.Id,
                            item.CustomerContentId,
                            failUpdate,
                            cancellationToken);

                        _logger.FrameworkErrorLog(LogHelper.Generate(
                            message: item.CurrentStep,
                            reference: new { request.ScopeKey, RefContentId = request.AnalysisContentId, RefNormalizedRequestId = request.Id, item.CustomerContentId },
                            facility: item.CurrentStep,
                            correlationId: request.CorrelationId,
                            exception: null
                        ));

                        continue;
                    }

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.RetryScheduled,
                        reference: new { request.ScopeKey, RefContentId = request.AnalysisContentId, RefNormalizedRequestId = request.Id, item.CustomerContentId, FailedStep = item.CurrentStep },
                        facility: EventNames.RetryScheduled,
                        correlationId: request.CorrelationId,
                        exception: null
                    ));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "RETRY_ANALYSIS_REQUEST_FAILED: RequestId={RequestId} CustomerContentId={CustomerContentId}", request.Id, item.CustomerContentId);

                    var retryAgainUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.WaitingRetry)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", DateTime.UtcNow.AddSeconds(retrySettings.ClaimFailRescheduleDelaySeconds));

                    await UpdateAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        retryAgainUpdate,
                        cancellationToken);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.RetryScheduled,
                        reference: new { request.ScopeKey, RefContentId = request.AnalysisContentId, RefNormalizedRequestId = request.Id, item.CustomerContentId, FailedStep = item.CurrentStep },
                        facility: EventNames.RetryScheduled,
                        correlationId: request.CorrelationId,
                        exception: ex
                    ));
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
                i.Status == NormalizeStatusNames.WaitingRetry &&
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