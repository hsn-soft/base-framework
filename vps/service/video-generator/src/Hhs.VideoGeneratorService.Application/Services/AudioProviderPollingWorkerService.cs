using System.Linq.Expressions;
using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.VideoGeneratorService.Application.Providers;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Repositories;
using Hhs.VideoGeneratorService.Domain.SettingDomain.Repositories;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class AudioProviderPollingWorkerService(
    IServiceProvider provider,
    IAudioRequestRepository audioRequestRepository,
    ICustomerVpSettingRepository customerVpSettingRepository,
    IAudioProviderResolver audioProviderResolver,
    ILogger<AudioProviderPollingWorkerService> logger,
    AudioPollingSettings pollingSettings) : ApplicationServiceBase(provider)
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

    public async Task PollDueAudioRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var options = new ListQueryOptions<AudioRequest>
        {
            Filter = x =>
                x.Status == AudioStatusNames.AudioProviderPolling &&
                x.NextProviderPollAtUtc != null &&
                x.NextProviderPollAtUtc <= now &&
                x.AudioProviderTrackingId != null,
            MaxResultCount = 50
        };

        var requests = await audioRequestRepository.GetListAsync(options, cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                var claimResult = await ClaimDueAudioPollingAsync(
                    request.Id,
                    now,
                    cancellationToken);

                if (claimResult == 0)
                    continue;

                if (request.ProviderPollingCount >= pollingSettings.MaxAttempts)
                {
                    request.Status = AudioStatusNames.Failed;
                    request.LastError = ErrorMessages.AudioProviderPollingTimeout;

                    await ReplaceAudioAsync(request, cancellationToken);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.AudioProviderPollingStarted,
                        reference: new { AudioRequestId = request.Id, VideoRequestId = request.VideoRequestId, request.ProviderPollingCount },
                        facility: EventNames.AudioProviderPollingStarted,
                        correlationId: request.CorrelationId,
                        exception: null
                    ));

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = request.RefContentId,
                            RefContentType = request.RefContentType,
                            Step = EventNames.AudioProviderPollingStarted,
                            ErrorMessage = request.LastError,
                            Retryable = false
                        }
                    );

                    continue;
                }

                var providerKeyResult = await customerVpSettingRepository.GetAudioProviderKeyByScopeKeyAsync(request.ScopeKey, cancellationToken);
                if (!providerKeyResult.Key)
                {
                    throw new InvalidOperationException($"Provider key value is unknown. Scope key: {request.ScopeKey}");
                }

                var provider = audioProviderResolver.Resolve(providerKeyResult.Value);

                var status = await provider.GetStatusAsync(request.AudioProviderTrackingId!);

                if (status.IsFailed)
                {
                    request.Status = AudioStatusNames.Failed;
                    request.LastError = status.ErrorMessage ?? "Audio provider failed.";

                    await ReplaceAudioAsync(request, cancellationToken);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.AudioProviderPollingStarted,
                        reference: new { AudioRequestId = request.Id, VideoRequestId = request.VideoRequestId, request.ProviderPollingCount, request.LastError },
                        facility: EventNames.AudioProviderPollingStarted,
                        correlationId: request.CorrelationId,
                        exception: null
                    ));

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = request.RefContentId,
                            RefContentType = request.RefContentType,
                            Step = EventNames.AudioProviderPollingStarted,
                            ErrorMessage = request.LastError,
                            Retryable = false
                        }
                    );

                    continue;
                }

                if (!status.IsCompleted)
                {
                    request.ProviderPollingCount++;
                    request.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(pollingSettings.IntervalSeconds);

                    await ReplaceAudioAsync(request, cancellationToken);

                    _logger.FrameworkInfoLog(LogHelper.Generate(
                        message: EventNames.AudioProviderPollingStarted,
                        reference: new { AudioRequestId = request.Id, VideoRequestId = request.VideoRequestId, Attempt = request.ProviderPollingCount, MaxAttempts = pollingSettings.MaxAttempts, request.NextProviderPollAtUtc },
                        facility: EventNames.AudioProviderPollingStarted,
                        correlationId: request.CorrelationId,
                        exception: null
                    ));

                    continue;
                }

                if (string.IsNullOrWhiteSpace(status.ProviderFileUrl))
                    throw new InvalidOperationException("Audio provider completed but file url is empty.");

                // set provider file url
                request.AudioProviderUrl = status.ProviderFileUrl;

                request.ProviderPollingCount++;
                request.NextProviderPollAtUtc = null;
                request.Status = AudioStatusNames.AudioProviderCompleted;
                request.CurrentStep = EventNames.AudioProviderCompleted;
                request.LastError = null;

                await ReplaceAudioAsync(request, cancellationToken);

                _logger.FrameworkInfoLog(LogHelper.Generate(
                    message: EventNames.AudioProviderCompleted,
                    reference: new { AudioRequestId = request.Id, VideoRequestId = request.VideoRequestId, TotalPolls = request.ProviderPollingCount },
                    facility: EventNames.AudioProviderCompleted,
                    correlationId: request.CorrelationId,
                    exception: null
                ));

                // Publish provider completed event - handler will trigger download cascade
                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    correlationId: request.CorrelationId,
                    eventMessage: new AudioProviderCompletedEto { AudioRequestId = request.Id, }
                );
            }
            catch (Exception ex)
            {
                request.ProviderPollingCount++;
                request.LastError = ex.Message;

                if (request.ProviderPollingCount >= pollingSettings.MaxAttempts)
                {
                    request.Status = AudioStatusNames.Failed;
                    request.NextProviderPollAtUtc = null;

                    await ReplaceAudioAsync(request, cancellationToken);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.AudioProviderPollingStarted,
                        reference: new { AudioRequestId = request.Id, VideoRequestId = request.VideoRequestId, request.ProviderPollingCount },
                        facility: EventNames.AudioProviderPollingStarted,
                        correlationId: request.CorrelationId,
                        exception: ex
                    ));

                    await EventBus.PublishAsync(
                        parentMessage: ParentIntegrationEvent,
                        correlationId: request.CorrelationId,
                        eventMessage: new StepFailedEto
                        {
                            RefContentId = request.RefContentId,
                            RefContentType = request.RefContentType,
                            Step = EventNames.AudioProviderPollingStarted,
                            ErrorMessage = ex.Message,
                            Retryable = false
                        }
                    );
                }
                else
                {
                    request.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(pollingSettings.BackoffIntervalSeconds);
                    await ReplaceAudioAsync(request, cancellationToken);

                    _logger.FrameworkErrorLog(LogHelper.Generate(
                        message: EventNames.RetryScheduled,
                        reference: new { AudioRequestId = request.Id, VideoRequestId = request.VideoRequestId, FailedStep = EventNames.AudioProviderPollingStarted, request.ProviderPollingCount, request.NextProviderPollAtUtc },
                        facility: EventNames.RetryScheduled,
                        correlationId: request.CorrelationId,
                        exception: ex
                    ));
                }

                logger.LogError(
                    ex,
                    "Audio provider polling failed. AudioRequestId: {AudioRequestId}",
                    request.Id);
            }
        }
    }

    private Task<long> ClaimDueAudioPollingAsync(
        Guid audioRequestId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var predicate = (Expression<Func<AudioRequest, bool>>)(x =>
            x.Id == audioRequestId &&
            x.Status == AudioStatusNames.AudioProviderPolling &&
            x.NextProviderPollAtUtc != null &&
            x.NextProviderPollAtUtc <= now &&
            x.AudioProviderTrackingId != null);

        var update = Builders<AudioRequest>.Update
            .Set(x => x.NextProviderPollAtUtc, DateTime.UtcNow.AddSeconds(pollingSettings.ErrorRescheduleDelaySeconds));

        return audioRequestRepository.UpdateByExpressionAsync(
            predicate,
            u => update,
            cancellationToken: cancellationToken);
    }

    private Task ReplaceAudioAsync(AudioRequest request, CancellationToken cancellationToken)
    {
        var predicate = (Expression<Func<AudioRequest, bool>>)(x => x.Id == request.Id);
        var update = Builders<AudioRequest>.Update
            .Set(x => x.Status, request.Status)
            .Set(x => x.CurrentStep, request.CurrentStep)
            .Set(x => x.InputText, request.InputText)
            .Set(x => x.AudioProviderKey, request.AudioProviderKey)
            .Set(x => x.SortOrder, request.SortOrder)
            .Set(x => x.AudioProviderTrackingId, request.AudioProviderTrackingId)
            .Set(x => x.AudioProviderUrl, request.AudioProviderUrl)
            .Set(x => x.NextProviderPollAtUtc, request.NextProviderPollAtUtc)
            .Set(x => x.ProviderPollingCount, request.ProviderPollingCount)
            .Set(x => x.AudioLocalPath, request.AudioLocalPath)
            .Set(x => x.AudioCdnProviderKey, request.AudioCdnProviderKey)
            .Set(x => x.AudioCdnUrl, request.AudioCdnUrl)
            .Set(x => x.AudioStorageUrl, request.AudioStorageUrl)
            .Set(x => x.LastError, request.LastError);

        return audioRequestRepository.UpdateByExpressionAsync(
            predicate,
            u => update,
            cancellationToken: cancellationToken);
    }
}