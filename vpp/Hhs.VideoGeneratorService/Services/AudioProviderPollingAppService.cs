using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.Shared.Configuration;
using Hhs.VideoGeneratorService.Configuration;
using Hhs.VideoGeneratorService.Configuration.Providers.Audio;
using Hhs.VideoGeneratorService.Entities;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using Hhs.VideoGeneratorService.Providers.FileDownloader;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Services;

public sealed class AudioProviderPollingAppService(
    VideoMongoContext context,
    IAudioProviderResolver audioProviderResolver,
    IEventBus eventBus,
    ILogger<AudioProviderPollingAppService> logger,
    IRemoteFileDownloader fileDownloader,
    AudioFastProviderSettings audioFastSettings,
    AudioQueueProviderSettings audioQueueSettings,
    AudioPollingSettings pollingSettings)
{
    private readonly AudioPollingSettings _pollingSettings = pollingSettings;

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
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceAudioAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        RefContentId = request.RefContentId,
                        RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        Step = EventNames.AudioProviderPollingStarted,
                        ErrorMessage = request.LastError,
                        Retryable = false
                    }, cancellationToken);

                    continue;
                }

                string? audioProviderKey = SubscriptionScopeRegistry.GetAudioProviderKey(request.ScopeKey);
                var provider = audioProviderResolver.Resolve(audioProviderKey);

                var status = await provider.GetStatusAsync(
                    request.AudioProviderTrackingId!,
                    cancellationToken);

                if (status.IsFailed)
                {
                    request.Status = StatusNames.Failed;
                    request.LastError = status.ErrorMessage ?? "Audio provider failed.";
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceAudioAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        RefContentId = request.RefContentId,
                        RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        Step = EventNames.AudioProviderPollingStarted,
                        ErrorMessage = request.LastError,
                        Retryable = false
                    }, cancellationToken);

                    continue;
                }

                if (!status.IsCompleted)
                {
                    request.ProviderPollingCount++;
                    request.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(_pollingSettings.IntervalSeconds);
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceAudioAsync(request, cancellationToken);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(status.ProviderFileUrl))
                    throw new InvalidOperationException("Audio provider completed but file url is empty.");

                request.ProviderPollingCount++;
                request.NextProviderPollAtUtc = null;
                request.Status = StatusNames.AudioProviderCompleted;
                request.CurrentStep = EventNames.AudioProviderCompleted;
                request.LastError = null;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceAudioAsync(request, cancellationToken);

                // Publish provider completed event - handler will trigger download cascade
                await eventBus.PublishAsync(new AudioProviderCompletedEto
                {
                    RefContentId = request.RefContentId,
                    RefContentType = request.RefContentType,
                    CorrelationId = request.CorrelationId,
                    VideoRequestId = request.VideoRequestId,
                    AudioRequestId = request.Id,
                    ProviderFileUrl = status.ProviderFileUrl
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                request.ProviderPollingCount++;
                request.LastError = ex.Message;
                request.UpdatedAtUtc = DateTime.UtcNow;

                if (request.ProviderPollingCount >= 60)
                {
                    request.Status = StatusNames.Failed;
                    request.NextProviderPollAtUtc = null;

                    await ReplaceAudioAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEto
                    {
                        RefContentId = request.RefContentId,
                        RefContentType = request.RefContentType,
                        CorrelationId = request.CorrelationId,
                        Step = EventNames.AudioProviderPollingStarted,
                        ErrorMessage = ex.Message,
                        Retryable = false
                    }, cancellationToken);
                }
                else
                {
                    request.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(_pollingSettings.BackoffIntervalSeconds);
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
                .Set(x => x.NextProviderPollAtUtc, DateTime.UtcNow.AddSeconds(_pollingSettings.ErrorRescheduleDelaySeconds))
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
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