using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.VideoGeneratorService.Entities;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Services;

public sealed class AudioProviderPollingAppService(
    VideoMongoContext context,
    IAudioProviderResolver audioProviderResolver,
    IEventBus eventBus,
    ILogger<AudioProviderPollingAppService> logger)
{
    public async Task PollDueAudioRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var requests = await context.AudioRequests
            .Find(x =>
                x.Status == "AUDIO_PROVIDER_POLLING" &&
                x.NextProviderPollAtUtc != null &&
                x.NextProviderPollAtUtc <= now &&
                x.AudioProviderTrackId != null)
            .Limit(50)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                if (request.ProviderPollingCount >= request.MaxProviderPollingCount)
                {
                    request.Status = "FAILED";
                    request.LastError = "Audio provider polling timeout.";
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceAudioAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEvent
                    {

                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
                        CorrelationId = request.CorrelationId,
                        Step = EventNames.AudioProviderPollingStarted,
                        ErrorMessage = request.LastError,
                        Retryable = false
                    }, cancellationToken);

                    continue;
                }

                var provider = audioProviderResolver.Resolve(request.AudioProviderKey);

                var status = await provider.GetStatusAsync(
                    request.AudioProviderTrackId!,
                    cancellationToken);

                if (status.IsFailed)
                {
                    request.Status = "FAILED";
                    request.LastError = status.ErrorMessage ?? "Audio provider failed.";
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceAudioAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEvent
                    {
                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
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
                    request.NextProviderPollAtUtc = DateTime.UtcNow.AddMinutes(5);
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceAudioAsync(request, cancellationToken);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(status.ProviderFileUrl))
                    throw new InvalidOperationException("Audio provider completed but file url is empty.");

                request.ProviderPollingCount++;
                request.NextProviderPollAtUtc = null;
                request.ProviderAudioFileUrl = status.ProviderFileUrl;
                request.Status = "AUDIO_PROVIDER_COMPLETED";
                request.CurrentStep = EventNames.AudioProviderCompleted;
                request.LastError = null;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceAudioAsync(request, cancellationToken);

                await eventBus.PublishAsync(new AudioProviderCompletedEvent
                {
                    CustomerContentId = request.CustomerContentId,
                    AnalysisContentId = request.AnalysisContentId,
                    ContentProcessType = request.ContentProcessType,
                    CorrelationId = request.CorrelationId,
                    VideoRequestId = request.VideoRequestId,
                    AudioRequestId = request.Id,
                    ProviderFileUrl = status.ProviderFileUrl
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                request.ProviderPollingCount++;
                request.NextProviderPollAtUtc = DateTime.UtcNow.AddMinutes(5);
                request.LastError = ex.Message;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceAudioAsync(request, cancellationToken);

                logger.LogError(
                    ex,
                    "Audio provider polling failed. AudioRequestId: {AudioRequestId}",
                    request.Id);
            }
        }
    }

    private Task ReplaceAudioAsync(AudioRequest request, CancellationToken cancellationToken)
    {
        return context.AudioRequests.ReplaceOneAsync(
            x => x.Id == request.Id,
            request,
            cancellationToken: cancellationToken);
    }
}