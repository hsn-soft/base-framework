using System.Linq.Expressions;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Configuration;
using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Application.Providers;
using Hhs.TextNormalizerService.Application.Providers.Outline;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.EventBus;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class OutlineProviderPollingAppService(
    IServiceProvider provider,
    IAnalysisContentNormalizedRequestRepository analysisRepository,
    ICustomerContentNormalizedRequestRepository customerRepository,
    IOutlineProviderResolver outlineProviderResolver,
    IEventBus eventBus,
    ILogger<OutlineProviderPollingAppService> logger,
    OutlinePollingSettings pollingSettings) : ApplicationServiceBase(provider)
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
        var options = new ListQueryOptions<CustomerContentNormalizedRequest>
        {
            Filter = x => x.Status == StatusNames.OutlineProviderPolling &&
                         x.NextOutlinePollAtUtc != null &&
                         x.NextOutlinePollAtUtc <= now &&
                         x.OutlineProviderTrackId != null,
            MaxResultCount = 50
        };

        var requests = await customerRepository
            .GetListAsync(options, cancellationToken)
            .ConfigureAwait(false);

        foreach (var request in requests)
        {
            try
            {
                var claimResult = await ClaimDueCustomerPollingAsync(
                    request.Id,
                    now,
                    cancellationToken);

                if (claimResult == 0)
                    continue;

                if (request.OutlinePollingCount >= request.MaxOutlinePollingCount)
                {
                    request.Status = StatusNames.Failed;
                    request.LastError = ErrorMessages.OutlineProviderPollingTimeout;

                    await ReplaceCustomerAsync(request, cancellationToken);

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = request.CustomerContentId,
                            RefContentType = ContentType.CustomerContent,
                            Step = EventNames.OutlineProviderPollingStarted,
                            ErrorMessage = request.LastError,
                            Retryable = false
                        }
                    );

                    continue;
                }

                var provider = outlineProviderResolver.Resolve(SubscriptionScopeRegistry.GetOutlineProviderKey(request.ScopeKey));

                var status = await provider.GetStatusAsync
                (
                    new OutlineStatusRequest
                    {
                        ProviderTrackId = request.OutlineProviderTrackId
                                          ?? throw new InvalidOperationException()
                    }
                );

                if (status.IsProcessFailed)
                {
                    request.Status = StatusNames.Failed;
                    request.LastError = status.ErrorMessage ?? ErrorMessages.OutlineProviderFailed;

                    await ReplaceCustomerAsync(request, cancellationToken);

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = request.CustomerContentId,
                            RefContentType = ContentType.CustomerContent,
                            Step = EventNames.OutlineProviderPollingStarted,
                            ErrorMessage = request.LastError,
                            Retryable = false
                        }
                    );

                    continue;
                }

                if (!status.IsProcessed)
                {
                    request.OutlinePollingCount++;
                    request.NextOutlinePollAtUtc = DateTime.UtcNow.AddSeconds(pollingSettings.IntervalSeconds);

                    await ReplaceCustomerAsync(request, cancellationToken);
                    continue;
                }

                request.OutlinePollingCount++;
                request.NextOutlinePollAtUtc = null;
                request.Status = StatusNames.OutlineProviderCompleted;
                request.OutlineStatus = StatusNames.ProviderCompleted;
                request.CurrentStep = EventNames.OutlineProviderCompleted;

                await ReplaceCustomerAsync(request, cancellationToken);

                if (string.IsNullOrWhiteSpace(status.OutlinedData))
                    throw new InvalidOperationException("Outline provider completed but script is empty.");

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    eventMessage: new OutlineProviderCompletedEto { RefContentType = ContentType.CustomerContent, NormalizedRequestId = request.Id, Script = status.OutlinedData! }
                );
            }
            catch (Exception ex)
            {
                request.OutlinePollingCount++;
                request.LastError = ex.Message;

                if (request.OutlinePollingCount >= request.MaxOutlinePollingCount)
                {
                    request.Status = StatusNames.Failed;
                    request.OutlineStatus = StatusNames.Failed;
                    request.CurrentStep = EventNames.OutlineProviderPollingStarted;
                    request.NextOutlinePollAtUtc = null;

                    await ReplaceCustomerAsync(request, cancellationToken);

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = request.CustomerContentId,
                            RefContentType = ContentType.CustomerContent,
                            Step = EventNames.OutlineProviderPollingStarted,
                            ErrorMessage = ex.Message,
                            Retryable = false
                        }
                    );
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
        var options = new ListQueryOptions<AnalysisContentNormalizedRequest>
        {
            Filter = x => x.Items.Any(i =>
                i.OutlineStatus == StatusNames.Polling &&
                i.NextOutlinePollAtUtc <= now &&
                i.OutlineProviderTrackId != null),
            MaxResultCount = 50
        };

        var requests = await analysisRepository
            .GetListAsync(options, cancellationToken)
            .ConfigureAwait(false);

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
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", DateTime.UtcNow.AddSeconds(pollingSettings.IntervalSeconds));

                    var claimResult = await UpdateDuePollingAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        now,
                        claimUpdate,
                        cancellationToken);

                    if (claimResult == 0)
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
                        }
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
                            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", DateTime.UtcNow.AddSeconds(pollingSettings.ErrorRescheduleDelaySeconds));

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
                        .Set(x => x.CurrentStep, EventNames.OutlineProviderCompleted);

                    await UpdateAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        completedUpdate,
                        cancellationToken);

                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        eventMessage: new OutlineProviderCompletedEto { CustomerContentIdForItem = item.CustomerContentId, RefContentType = ContentType.AnalysisContent, NormalizedRequestId = request.Id, Script = status.OutlinedData! }
                    );
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
                        .Set(x => x.Status, StatusNames.OutlineProviderPolling)
                        .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted)
                        .Set(x => x.LastError, ex.Message);

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

    private Task<long> UpdateDuePollingAnalysisItemAsync(
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
                i.OutlineStatus == StatusNames.Polling &&
                i.NextOutlinePollAtUtc != null &&
                i.NextOutlinePollAtUtc <= now &&
                i.OutlineProviderTrackId != null));

        return analysisRepository.UpdateByExpressionAsync(
            predicate,
            u => update,
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
            .Set(x => x.Status, StatusNames.Failed)
            .Set(x => x.CurrentStep, EventNames.OutlineProviderPollingStarted)
            .Set(x => x.LastError, errorMessage);

        await UpdateAnalysisItemAsync(
            request.Id,
            item.CustomerContentId,
            update,
            cancellationToken);

        await EventBus.PublishAsync(
            parentMessage: ParentIntegrationEvent,
            correlationId: request.CorrelationId,
            eventMessage: new StepFailedEto
            {
                RefContentId = request.AnalysisContentId,
                RefContentType = ContentType.AnalysisContent,
                Step = EventNames.OutlineProviderPollingStarted,
                ErrorMessage = errorMessage,
                Retryable = retryable
            }
        );
    }

    private Task UpdateAnalysisItemAsync(
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

    private async Task ReplaceCustomerAsync(
        CustomerContentNormalizedRequest request,
        CancellationToken cancellationToken)
    {
        var predicate = (Expression<Func<CustomerContentNormalizedRequest, bool>>)(x => x.Id == request.Id);
        var update = Builders<CustomerContentNormalizedRequest>.Update
            .Set(x => x.Status, request.Status)
            .Set(x => x.OutlineStatus, request.OutlineStatus)
            .Set(x => x.OutlinePollingCount, request.OutlinePollingCount)
            .Set(x => x.NextOutlinePollAtUtc, request.NextOutlinePollAtUtc)
            .Set(x => x.OutlineProviderTrackId, request.OutlineProviderTrackId)
            .Set(x => x.LastError, request.LastError)
            .Set(x => x.CurrentStep, request.CurrentStep);

        await customerRepository.UpdateByExpressionAsync(
            predicate,
            u => update,
            cancellationToken: cancellationToken)
            .ConfigureAwait(false);
    }

    private Task<long> ClaimDueCustomerPollingAsync(
        Guid customerRequestId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var predicate = (Expression<Func<CustomerContentNormalizedRequest, bool>>)(x =>
            x.Id == customerRequestId &&
            x.Status == StatusNames.OutlineProviderPolling &&
            x.NextOutlinePollAtUtc != null &&
            x.NextOutlinePollAtUtc <= now &&
            x.OutlineProviderTrackId != null);

        var update = Builders<CustomerContentNormalizedRequest>.Update
            .Set(x => x.NextOutlinePollAtUtc, DateTime.UtcNow.AddSeconds(pollingSettings.ErrorRescheduleDelaySeconds));

        return customerRepository.UpdateByExpressionAsync(
            predicate,
            u => update,
            cancellationToken: cancellationToken);
    }
}