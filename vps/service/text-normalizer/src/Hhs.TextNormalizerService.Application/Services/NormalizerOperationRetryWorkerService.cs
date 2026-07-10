using System.Linq.Expressions;
using Hhs.Shared.Contracts.EventInbox;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Domain.Configuration;
using Hhs.TextNormalizerService.Domain.Constants;
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
    /// Triggered on its own schedule (not part of RetryDueRequestsAsync) since this is a
    /// happy-path fan-in gate, not error recovery.
    /// </summary>
    public async Task CheckReadyAnalysisContentsToOutlineAsync(CancellationToken cancellationToken)
    {
        var options = new ListQueryOptions<AnalysisContentNormalizedRequest>
        {
            Filter = x => x.Status != NormalizeStatusNames.Completed && x.Status != NormalizeStatusNames.Failed &&
                          x.Items.Any(i => i.ScrapingStatus == ScrapingStatusNames.Completed && i.OutlineStatus == OutlineStatusNames.NotStarted),
            MaxResultCount = retrySettings.BatchSize,
            OrderByEntity = o => o.OrderBy(x => x.CreationTime)
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
    /// sibling's retry/poll/fail write. Triggered on its own schedule (not part of
    /// RetryDueRequestsAsync) since this is a happy-path fan-in gate, not error recovery.
    /// </summary>
    public async Task CheckReadyAnalysisContentsToResultAsync(CancellationToken cancellationToken)
    {
        var options = new ListQueryOptions<AnalysisContentNormalizedRequest>
        {
            Filter = x => x.Status != NormalizeStatusNames.Completed && x.Status != NormalizeStatusNames.Failed &&
                          x.Items.Count > 0 &&
                          x.Items.All(i => i.OutlineStatus == OutlineStatusNames.Completed || i.OutlineStatus == OutlineStatusNames.Failed),
            MaxResultCount = retrySettings.BatchSize,
            OrderByEntity = o => o.OrderBy(x => x.CreationTime)
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
                        .Set(x => x.CurrentMilestone, Milestones.AnalysisItemOutlineCompleted)
                        .Set(x => x.LastError, "One or more items failed during processing.");

                    long failClaimed = await analysisRepository.UpdateByExpressionAsync(failPredicate, _ => failUpdate, cancellationToken: cancellationToken).ConfigureAwait(false);
                    if (failClaimed == 0) continue;

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: Milestones.AnalysisItemOutlineCompleted,
                        reference: new
                        {
                            request.ScopeKey,
                            Type = nameof(AnalysisContentNormalizedRequest),
                            Key = request.Id,
                            RefType = "AnalysisContent",
                            RefKey = request.AnalysisContentId
                        },
                        facility: Facilities.MilestoneFailed,
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
                    .Set(x => x.CurrentMilestone, Milestones.NormalizerResultPublished)
                    .Set(x => x.LastError, null);

                long claimed = await analysisRepository.UpdateByExpressionAsync(claimPredicate, _ => claimUpdate, cancellationToken: cancellationToken).ConfigureAwait(false);
                if (claimed == 0) continue; // already claimed by another tick/instance

                await normalizerOperationAppService.AppendIntroOutroToAnalysisItemsAsync(request, cancellationToken);

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: Milestones.NormalizerResultPublished,
                    reference: new
                    {
                        request.ScopeKey,
                        Type = "AnalysisContent",
                        Key = request.AnalysisContentId,
                        RefType = nameof(AnalysisContentNormalizedRequest),
                        RefKey = request.Id,
                    },
                    facility: Facilities.NormalizerResultPublished,
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
                        NormalizeCurrentMilestone = Milestones.NormalizerResultPublished
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
        int updated = await inboxManager.ResetStaleStartedMessagesAsync(staleThreshold, cancellationToken);
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
                var claimPredicate = (Expression<Func<CustomerContentNormalizedRequest, bool>>)(x =>
                    x.Id == request.Id &&
                    x.Status == NormalizeStatusNames.WaitingRetry &&
                    x.NextRetryAtUtc != null &&
                    x.NextRetryAtUtc <= now);

                var claimUpdate = Builders<CustomerContentNormalizedRequest>.Update
                    .Set(x => x.NextRetryAtUtc, DateTime.UtcNow.AddSeconds(retrySettings.ClaimFailRescheduleDelaySeconds))
                    .Set(x => x.LastError, null);

                long claimResult = await customerRepository.UpdateByExpressionAsync(
                        claimPredicate,
                        _ => claimUpdate,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                if (claimResult == 0)
                    continue;

                if (request.CurrentMilestone == Milestones.CustomerContentScrapingStarted)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new CustomerContentScrapingStartedEto { CustomerContentNormalizeRequestId = request.Id, }
                    );
                }
                else if (request.CurrentMilestone == Milestones.CustomerContentOutlineStarted)
                {
                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new CustomerContentOutlineStartedEto { CustomerContentNormalizeRequestId = request.Id }
                    );
                }
                else if (request.CurrentMilestone == Milestones.OutlineProviderRequestStarted)
                {
                    if (request.ScrapingResult is null)
                        throw new InvalidOperationException(ErrorMessages.ScrapingResultRequired);

                    (string outlineInputText, string outlineInputPrompt) = await normalizerOperationAppService.BuildCustomerOutlineInputAsync(request, cancellationToken);

                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new OutlineProviderRequestStartedEto
                        {
                            RefContentType = ContentType.CustomerContent,
                            RefNormalizedRequestId = request.Id,
                            ScopeKey = request.ScopeKey,
                            InputText = outlineInputText,
                            InputPrompt = outlineInputPrompt
                        }
                    );
                }
                else
                {
                    var failPredicate = (Expression<Func<CustomerContentNormalizedRequest, bool>>)(x => x.Id == request.Id);
                    var failUpdate = Builders<CustomerContentNormalizedRequest>.Update
                        .Set(x => x.Status, NormalizeStatusNames.Failed)
                        .Set(x => x.LastError, $"Unsupported customer retry milestone: {request.CurrentMilestone}")
                        .Set(x => x.NextRetryAtUtc, null);

                    await customerRepository.UpdateByExpressionAsync(
                            failPredicate,
                            _ => failUpdate,
                            cancellationToken: cancellationToken)
                        .ConfigureAwait(false);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: request.CurrentMilestone,
                        reference: new
                        {
                            request.ScopeKey,
                            Type = nameof(CustomerContentNormalizedRequest),
                            Key = request.Id,
                            RefType = "CustomerContent",
                            RefKey = request.CustomerContentId,
                            FailedMilestone = request.CurrentMilestone
                        },
                        facility: Facilities.MilestoneFailed,
                        correlationId: request.CorrelationId,
                        exception: null
                    ));

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new MilestoneFailedEto
                        {
                            RefContentId = request.CustomerContentId,
                            RefContentType = ContentType.CustomerContent,
                            Milestone = request.CurrentMilestone,
                            ErrorMessage = $"Unsupported customer retry milestone: {request.CurrentMilestone}",
                            Retryable = false
                        }
                    );

                    continue;
                }

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: Milestones.RetryScheduled,
                    reference: new
                    {
                        request.ScopeKey,
                        Type = nameof(CustomerContentNormalizedRequest),
                        Key = request.Id,
                        RefType = "CustomerContent",
                        RefKey = request.CustomerContentId,
                        FailedMilestone = request.CurrentMilestone
                    },
                    facility: Facilities.RetryAttempted,
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
                        _ => exceptionUpdate,
                        cancellationToken: cancellationToken)
                    .ConfigureAwait(false);

                _logger.FrameworkErrorLog(LogHelper.Generate(
                    message: Milestones.RetryScheduled,
                    reference: new
                    {
                        request.ScopeKey,
                        Type = nameof(CustomerContentNormalizedRequest),
                        Key = request.Id,
                        RefType = "CustomerContent",
                        RefKey = request.CustomerContentId,
                        FailedMilestone = request.CurrentMilestone
                    },
                    facility: Facilities.RetryAttemptFailed,
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
                    var claimUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", DateTime.UtcNow.AddSeconds(retrySettings.ClaimFailRescheduleDelaySeconds));

                    long claimResult = await UpdateDueRetryAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        now,
                        claimUpdate,
                        cancellationToken);

                    if (claimResult == 0)
                        continue;

                    if (item.CurrentMilestone == Milestones.AnalysisItemScrapingStarted)
                    {
                        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                            correlationId: request.CorrelationId,
                            eventMessage: new AnalysisItemScrapingStartedEto { AnalysisContentId = request.AnalysisContentId, CustomerContentIdForItem = item.CustomerContentId }
                        );
                    }
                    else if (item.CurrentMilestone == Milestones.AnalysisItemOutlineStarted)
                    {
                        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                            correlationId: request.CorrelationId,
                            eventMessage: new AnalysisItemOutlineStartedEto { AnalysisContentId = request.AnalysisContentId, CustomerContentIdForItem = item.CustomerContentId }
                        );
                    }
                    else if (item.CurrentMilestone == Milestones.OutlineProviderRequestStarted)
                    {
                        if (item.ScrapingResult is null)
                            throw new InvalidOperationException(ErrorMessages.ScrapingResultRequired);

                        (string outlineInputText, string outlineInputPrompt) = await normalizerOperationAppService.BuildAnalysisItemOutlineInputAsync(request, item, cancellationToken);

                        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                            correlationId: request.CorrelationId,
                            eventMessage: new OutlineProviderRequestStartedEto
                            {
                                CustomerContentIdForItem = item.CustomerContentId,
                                RefContentType = ContentType.AnalysisContent,
                                RefNormalizedRequestId = request.Id,
                                ScopeKey = request.ScopeKey,
                                InputText = outlineInputText,
                                InputPrompt = outlineInputPrompt
                            }
                        );
                    }
                    else
                    {
                        var failUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.Failed)
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", $"Unsupported analysis retry milestone: {item.CurrentMilestone}")
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextRetryAtUtc)}", (DateTime?)null);

                        await UpdateAnalysisItemAsync(
                            request.Id,
                            item.CustomerContentId,
                            failUpdate,
                            cancellationToken);

                        _logger.FrameworkErrorLog(LogHelper.Generate(
                            message: item.CurrentMilestone,
                            reference: new
                            {
                                request.ScopeKey,
                                Type = nameof(AnalysisNormalizedItem),
                                Key = item.CustomerContentId,
                                RefType = "AnalysisContent",
                                RefKey = request.AnalysisContentId,
                                AnalysisContentNormalizeRequestId = request.Id,
                                FailedMilestone = item.CurrentMilestone
                            },
                            facility: Facilities.MilestoneFailed,
                            correlationId: request.CorrelationId,
                            exception: null
                        ));

                        continue;
                    }

                    _logger.FrameworkInfoLog(LogHelper.Generate(
                        message: Milestones.RetryScheduled,
                        reference: new
                        {
                            request.ScopeKey,
                            Type = nameof(AnalysisNormalizedItem),
                            Key = item.CustomerContentId,
                            RefType = "AnalysisContent",
                            RefKey = request.AnalysisContentId,
                            AnalysisContentNormalizeRequestId = request.Id,
                            FailedMilestone = item.CurrentMilestone
                        },
                        facility: Facilities.RetryAttempted,
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
                        message: Milestones.RetryScheduled,
                        reference: new
                        {
                            request.ScopeKey,
                            Type = nameof(AnalysisNormalizedItem),
                            Key = item.CustomerContentId,
                            RefType = "AnalysisContent",
                            RefKey = request.AnalysisContentId,
                            AnalysisContentNormalizeRequestId = request.Id,
                            FailedMilestone = item.CurrentMilestone
                        },
                        facility: Facilities.RetryAttemptFailed,
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
            _ => update,
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
            _ => update,
            cancellationToken: cancellationToken);
    }
}