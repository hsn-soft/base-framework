using System.Linq.Expressions;
using Hhs.Shared.Contracts.Events;
using Hhs.TextNormalizerService.Application.Consts;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Enums;
using Hhs.TextNormalizerService.Application.Providers;
using Hhs.TextNormalizerService.Application.Providers.Outline;
using Hhs.TextNormalizerService.Domain.Configuration.Providers.Outline;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Entities;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Models;
using Hhs.TextNormalizerService.Domain.NormalizeDomain.Repositories;
using Hhs.TextNormalizerService.Domain.SettingDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Application.Services;

public sealed class OutlineProviderPollingWorkerService(
    IServiceProvider provider,
    IAnalysisContentNormalizedRequestRepository analysisRepository,
    ICustomerContentNormalizedRequestRepository customerRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    IOutlineProviderResolver outlineProviderResolver,
    ILogger<OutlineProviderPollingWorkerService> logger,
    OutlinePollingSettings pollingSettings) : ApplicationServiceBase(provider)
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

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
            Filter = x => x.Status == NormalizeStatusNames.OutlineProviderRequestPolling &&
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

                if (request.OutlinePollingCount >= pollingSettings.MaxAttempts)
                {
                    request.Status = NormalizeStatusNames.Failed;
                    request.LastError = ErrorMessages.OutlineProviderPollingTimeout;

                    await ReplaceCustomerAsync(request, cancellationToken);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.OutlineProviderPollingStarted,
                        reference: new { request.ScopeKey, Type = nameof(CustomerContentNormalizedRequest), Key = request.Id, RefType = "CustomerContent", RefKey = request.CustomerContentId, request.OutlinePollingCount },
                        facility: Facilities.StepFailed,
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
                            Step = EventNames.OutlineProviderPollingStarted,
                            ErrorMessage = request.LastError,
                            Retryable = false
                        }
                    );

                    continue;
                }

                var providerKeyResult = await customerVpSettingRepository.GetOutlineProviderKeyByScopeKeyAsync(request.ScopeKey, cancellationToken);
                if (!providerKeyResult.Key)
                {
                    throw new InvalidOperationException($"Provider key value is unknown. Scope key: {request.ScopeKey}");
                }

                var outlineProvider = outlineProviderResolver.Resolve(providerKeyResult.Value);

                var status = await outlineProvider.GetStatusAsync
                (
                    new OutlineStatusRequest
                    {
                        ProviderTrackId = request.OutlineProviderTrackId
                                          ?? throw new InvalidOperationException()
                    }
                );

                if (status.IsProcessFailed)
                {
                    request.Status = NormalizeStatusNames.Failed;
                    request.LastError = status.ErrorMessage ?? ErrorMessages.OutlineProviderFailed;

                    await ReplaceCustomerAsync(request, cancellationToken);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.OutlineProviderPollingStarted,
                        reference: new { request.ScopeKey, Type = nameof(CustomerContentNormalizedRequest), Key = request.Id, RefType = "CustomerContent", RefKey = request.CustomerContentId, request.LastError },
                        facility: Facilities.StepFailed,
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

                    _logger.FrameworkInfoLog(LogHelper.Generate(
                        message: EventNames.OutlineProviderPollingStarted,
                        reference: new { request.ScopeKey, Type = nameof(CustomerContentNormalizedRequest), Key = request.Id, RefType = "CustomerContent", RefKey = request.CustomerContentId, Attempt = request.OutlinePollingCount, MaxAttempts = pollingSettings.MaxAttempts, request.NextOutlinePollAtUtc },
                        facility: Facilities.OutlineProviderPollingStarted,
                        correlationId: request.CorrelationId,
                        exception: null
                    ));

                    continue;
                }

                request.OutlinePollingCount++;
                request.NextOutlinePollAtUtc = null;

                request.Status = NormalizeStatusNames.OutlineProviderRequestCompleted;
                request.CurrentStep = EventNames.OutlineProviderRequestCompleted;

                request.OutlineStatus = OutlineStatusNames.ProviderCompleted;

                await ReplaceCustomerAsync(request, cancellationToken);

                if (string.IsNullOrWhiteSpace(status.OutlinedData))
                    throw new InvalidOperationException("Outline provider completed but script is empty.");

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: EventNames.OutlineProviderRequestCompleted,
                    reference: new { request.ScopeKey, Type = nameof(CustomerContentNormalizedRequest), Key = request.Id, RefType = "CustomerContent", RefKey = request.CustomerContentId, TotalPolls = request.OutlinePollingCount },
                    facility: Facilities.OutlineProviderRequestCompleted,
                    correlationId: request.CorrelationId,
                    exception: null
                ));

                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: request.CorrelationId,
                    eventMessage: new OutlineProviderCompletedEto { RefContentType = ContentType.CustomerContent, RefNormalizedRequestId = request.Id, OutlinedData = status.OutlinedData! }
                );
            }
            catch (Exception ex)
            {
                request.OutlinePollingCount++;
                request.LastError = ex.Message;

                if (request.OutlinePollingCount >= pollingSettings.MaxAttempts)
                {
                    request.Status = NormalizeStatusNames.Failed;
                    request.OutlineStatus = OutlineStatusNames.Failed;
                    request.CurrentStep = EventNames.OutlineProviderPollingStarted;
                    request.NextOutlinePollAtUtc = null;

                    await ReplaceCustomerAsync(request, cancellationToken);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.OutlineProviderPollingStarted,
                        reference: new { request.ScopeKey, Type = nameof(CustomerContentNormalizedRequest), Key = request.Id, RefType = "CustomerContent", RefKey = request.CustomerContentId, request.OutlinePollingCount },
                        facility: Facilities.StepFailed,
                        correlationId: request.CorrelationId,
                        exception: ex
                    ));

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
                    request.Status = NormalizeStatusNames.OutlineProviderRequestPolling;
                    request.OutlineStatus = OutlineStatusNames.Polling;
                    request.CurrentStep = EventNames.OutlineProviderPollingStarted;
                    request.NextOutlinePollAtUtc = DateTime.UtcNow.AddSeconds(pollingSettings.BackoffIntervalSeconds);

                    await ReplaceCustomerAsync(request, cancellationToken);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.RetryScheduled,
                        reference: new { request.ScopeKey, Type = nameof(CustomerContentNormalizedRequest), Key = request.Id, RefType = "CustomerContent", RefKey = request.CustomerContentId, FailedStep = EventNames.OutlineProviderPollingStarted, request.OutlinePollingCount, request.NextOutlinePollAtUtc },
                        facility: Facilities.RetryScheduled,
                        correlationId: request.CorrelationId,
                        exception: ex
                    ));
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
                i.OutlineStatus == OutlineStatusNames.Polling &&
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
                    i.OutlineStatus == OutlineStatusNames.Polling &&
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

                    if (item.OutlinePollingCount >= pollingSettings.MaxAttempts)
                    {
                        await FailAnalysisPollingItemAsync(
                            request,
                            item,
                            ErrorMessages.OutlineProviderPollingTimeout,
                            false,
                            cancellationToken);

                        continue;
                    }

                    var providerKeyResult = await customerVpSettingRepository.GetOutlineProviderKeyByScopeKeyAsync(request.ScopeKey, cancellationToken);
                    if (!providerKeyResult.Key)
                    {
                        throw new InvalidOperationException($"Provider key value is unknown. Scope key: {request.ScopeKey}");
                    }
                    var outlineProvider = outlineProviderResolver.Resolve(providerKeyResult.Value);

                    var status = await outlineProvider.GetStatusAsync
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

                        _logger.FrameworkInfoLog(LogHelper.Generate(
                            message: EventNames.OutlineProviderPollingStarted,
                            reference: new { request.ScopeKey, Type = nameof(AnalysisNormalizedItem), Key = item.CustomerContentId, RefType = "AnalysisContent", RefKey = request.AnalysisContentId, AnalysisContentNormalizeRequestId = request.Id, Attempt = item.OutlinePollingCount + 1, MaxAttempts = pollingSettings.MaxAttempts },
                            facility: Facilities.OutlineProviderPollingStarted,
                            correlationId: request.CorrelationId,
                            exception: null
                        ));

                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(status.OutlinedData))
                        throw new InvalidOperationException("Outline provider completed but script is empty.");

                    var completedUpdate = Builders<AnalysisContentNormalizedRequest>.Update
                        .Inc($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlinePollingCount)}", 1)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.OutlineProviderRequestCompleted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.OutlineProviderRequestCompleted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", (DateTime?)null)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", OutlineStatusNames.ProviderCompleted);

                    await UpdateAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        completedUpdate,
                        cancellationToken);

                    _logger.FrameworkInfoLog(LogHelper.Generate(
                        message: EventNames.OutlineProviderRequestCompleted,
                        reference: new { request.ScopeKey, Type = nameof(AnalysisNormalizedItem), Key = item.CustomerContentId, RefType = "AnalysisContent", RefKey = request.AnalysisContentId, AnalysisContentNormalizeRequestId = request.Id },
                        facility: Facilities.OutlineProviderRequestCompleted,
                        correlationId: request.CorrelationId,
                        exception: null
                    ));

                    await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new OutlineProviderCompletedEto { CustomerContentIdForItem = item.CustomerContentId, RefContentType = ContentType.AnalysisContent, RefNormalizedRequestId = request.Id, OutlinedData = status.OutlinedData! }
                    );
                }
                catch (Exception ex)
                {
                    int nextCount = item.OutlinePollingCount + 1;

                    if (nextCount >= pollingSettings.MaxAttempts)
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
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.OutlineProviderRequestPolling)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.OutlineProviderPollingStarted)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", OutlineStatusNames.Polling)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", ex.Message)
                        .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", DateTime.UtcNow.AddSeconds(pollingSettings.ErrorRescheduleDelaySeconds));

                    await UpdateAnalysisItemAsync(
                        request.Id,
                        item.CustomerContentId,
                        update,
                        cancellationToken);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.RetryScheduled,
                        reference: new { request.ScopeKey, Type = nameof(AnalysisNormalizedItem), Key = item.CustomerContentId, RefType = "AnalysisContent", RefKey = request.AnalysisContentId, AnalysisContentNormalizeRequestId = request.Id, FailedStep = EventNames.OutlineProviderPollingStarted, OutlinePollingCount = nextCount },
                        facility: Facilities.RetryScheduled,
                        correlationId: request.CorrelationId,
                        exception: ex
                    ));

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
                i.OutlineStatus == OutlineStatusNames.Polling &&
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
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.Status)}", NormalizeStatusNames.Failed)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.CurrentStep)}", EventNames.OutlineProviderPollingStarted)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.OutlineStatus)}", OutlineStatusNames.Failed)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.LastError)}", errorMessage)
            .Set($"{nameof(AnalysisContentNormalizedRequest.Items)}.$.{nameof(AnalysisNormalizedItem.NextOutlinePollAtUtc)}", (DateTime?)null);

        await UpdateAnalysisItemAsync(
            request.Id,
            item.CustomerContentId,
            update,
            cancellationToken);

        _logger.FrameworkErrorLog(LogHelper.Generate(
            message: EventNames.OutlineProviderPollingStarted,
            reference: new { request.ScopeKey, Type = nameof(AnalysisNormalizedItem), Key = item.CustomerContentId, RefType = "AnalysisContent", RefKey = request.AnalysisContentId, AnalysisContentNormalizeRequestId = request.Id, errorMessage },
            facility: Facilities.StepFailed,
            correlationId: request.CorrelationId,
            exception: null
        ));

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
            x.Status == NormalizeStatusNames.OutlineProviderRequestPolling &&
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