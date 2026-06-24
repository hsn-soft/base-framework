using Hhs.Shared.Contracts.Events;
using Hhs.Shared.Helper;
using Hhs.Shared.Helper.Configuration;
using Hhs.VideoGeneratorService.Application.Providers;
using Hhs.VideoGeneratorService.Domain.Configuration;
using Hhs.VideoGeneratorService.Domain.MediaDomain.Entities;
using Hhs.VideoGeneratorService.MongoDb.Context;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Application.Services;

public sealed class AudioProviderPollingAppService(
    IServiceProvider provider,
    VideoGeneratorServiceDbContext context,
    IAudioProviderResolver audioProviderResolver,
    ILogger<AudioProviderPollingAppService> logger,
    AudioPollingSettings pollingSettings) : ApplicationServiceBase(provider)
{
    public async Task PollDueAudioRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var requests = await context.AudioRequests
            .Find(x =>
                x.Status == StatusNames.AudioProviderPolling &&
                x.NextProviderPollAtUtc != null &&
                x.NextProviderPollAtUtc <= now &&
                x.AudioProviderTrackingId != null)
            .Limit(50)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                var claimResult = await ClaimDueAudioPollingAsync(
                    request.Id,
                    now,
                    cancellationToken);

                if (claimResult.ModifiedCount == 0)
                    continue;

                if (request.ProviderPollingCount >= 60)
                {
                    request.Status = StatusNames.Failed;
                    request.LastError = ErrorMessages.AudioProviderPollingTimeout;

                    await ReplaceAudioAsync(request, cancellationToken);

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

                string? audioProviderKey = SubscriptionScopeRegistry.GetAudioProviderKey(request.ScopeKey);
                var provider = audioProviderResolver.Resolve(audioProviderKey);

                var status = await provider.GetStatusAsync(request.AudioProviderTrackingId!);

                if (status.IsFailed)
                {
                    request.Status = StatusNames.Failed;
                    request.LastError = status.ErrorMessage ?? "Audio provider failed.";

                    await ReplaceAudioAsync(request, cancellationToken);

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
                    continue;
                }

                if (string.IsNullOrWhiteSpace(status.ProviderFileUrl))
                    throw new InvalidOperationException("Audio provider completed but file url is empty.");

                // set provider file url
                request.AudioProviderUrl = status.ProviderFileUrl;

                request.ProviderPollingCount++;
                request.NextProviderPollAtUtc = null;
                request.Status = StatusNames.AudioProviderCompleted;
                request.CurrentStep = EventNames.AudioProviderCompleted;
                request.LastError = null;

                await ReplaceAudioAsync(request, cancellationToken);

                // Publish provider completed event - handler will trigger download cascade
                await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent,
                    eventMessage: new AudioProviderCompletedEto { AudioRequestId = request.Id, }
                );
            }
            catch (Exception ex)
            {
                request.ProviderPollingCount++;
                request.LastError = ex.Message;

                if (request.ProviderPollingCount >= 60)
                {
                    request.Status = StatusNames.Failed;
                    request.NextProviderPollAtUtc = null;

                    await ReplaceAudioAsync(request, cancellationToken);

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
                }

                logger.LogError(
                    ex,
                    "Audio provider polling failed. AudioRequestId: {AudioRequestId}",
                    request.Id);
            }
        }
    }

    private Task<UpdateResult> ClaimDueAudioPollingAsync(
        Guid audioRequestId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return context.AudioRequests.UpdateOneAsync(
            x =>
                x.Id == audioRequestId &&
                x.Status == StatusNames.AudioProviderPolling &&
                x.NextProviderPollAtUtc != null &&
                x.NextProviderPollAtUtc <= now &&
                x.AudioProviderTrackingId != null,
            Builders<AudioRequest>.Update
                .Set(x => x.NextProviderPollAtUtc, DateTime.UtcNow.AddSeconds(pollingSettings.ErrorRescheduleDelaySeconds)),
            cancellationToken: cancellationToken);
    }

    private Task ReplaceAudioAsync(AudioRequest request, CancellationToken cancellationToken)
    {
        return context.AudioRequests.ReplaceOneAsync(
            x => x.Id == request.Id,
            request,
            cancellationToken: cancellationToken);
    }
}