using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.VideoGeneratorService.Entities;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Services;

public sealed class VideoProviderPollingAppService(
    VideoMongoContext context,
    IVideoProviderResolver videoProviderResolver,
    IEventBus eventBus,
    ILogger<VideoProviderPollingAppService> logger)
{
    public async Task PollDueVideoRequestsAsync(CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var requests = await context.VideoRequests
            .Find(x =>
                x.Status == "VIDEO_PROVIDER_POLLING" &&
                x.NextProviderPollAtUtc != null &&
                x.NextProviderPollAtUtc <= now &&
                x.VideoProviderTrackId != null)
            .Limit(50)
            .ToListAsync(cancellationToken);

        foreach (var request in requests)
        {
            try
            {
                var claimResult = await ClaimDueVideoPollingAsync(
                    request.Id,
                    now,
                    cancellationToken);

                if (claimResult.ModifiedCount == 0)
                    continue;

                if (request.ProviderPollingCount >= request.MaxProviderPollingCount)
                {
                    request.Status = "FAILED";
                    request.LastError = "Video provider polling timeout.";
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceVideoAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEvent
                    {
                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
                        CorrelationId = request.CorrelationId,
                        Step = EventNames.VideoProviderPollingStarted,
                        ErrorMessage = request.LastError,
                        Retryable = false
                    }, cancellationToken);

                    continue;
                }

                var provider = videoProviderResolver.Resolve(request.VideoProviderKey);

                var status = await provider.GetStatusAsync(
                    request.VideoProviderTrackId!,
                    cancellationToken);

                if (status.IsFailed)
                {
                    request.Status = "FAILED";
                    request.LastError = status.ErrorMessage ?? "Video provider failed.";
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceVideoAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEvent
                    {
                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
                        CorrelationId = request.CorrelationId,
                        Step = EventNames.VideoProviderPollingStarted,
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

                    await ReplaceVideoAsync(request, cancellationToken);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(status.ProviderFileUrl))
                    throw new InvalidOperationException("Video provider completed but file url is empty.");

                request.ProviderPollingCount++;
                request.NextProviderPollAtUtc = null;
                request.ProviderVideoFileUrl = status.ProviderFileUrl;
                request.Status = "VIDEO_PROVIDER_COMPLETED";
                request.CurrentStep = EventNames.VideoProviderCompleted;
                request.LastError = null;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceVideoAsync(request, cancellationToken);

                await eventBus.PublishAsync(new VideoProviderCompletedEvent
                {
                    CustomerContentId = request.CustomerContentId,
                    AnalysisContentId = request.AnalysisContentId,
                    ContentProcessType = request.ContentProcessType,
                    CorrelationId = request.CorrelationId,
                    VideoRequestId = request.Id,
                    ProviderFileUrl = status.ProviderFileUrl
                }, cancellationToken);
            }
            catch (Exception ex)
            {
                request.ProviderPollingCount++;
                request.LastError = ex.Message;
                request.UpdatedAtUtc = DateTime.UtcNow;

                if (request.ProviderPollingCount >= request.MaxProviderPollingCount)
                {
                    request.Status = "FAILED";
                    request.NextProviderPollAtUtc = null;

                    await ReplaceVideoAsync(request, cancellationToken);

                    await eventBus.PublishAsync(new StepFailedEvent
                    {
                        CustomerContentId = request.CustomerContentId,
                        AnalysisContentId = request.AnalysisContentId,
                        ContentProcessType = request.ContentProcessType,
                        CorrelationId = request.CorrelationId,
                        Step = EventNames.VideoProviderPollingStarted,
                        ErrorMessage = ex.Message,
                        Retryable = false
                    }, cancellationToken);
                }
                else
                {
                    request.NextProviderPollAtUtc = DateTime.UtcNow.AddMinutes(5);
                    await ReplaceVideoAsync(request, cancellationToken);
                }

                logger.LogError(
                    ex,
                    "Video provider polling failed. VideoRequestId: {VideoRequestId}",
                    request.Id);
            }
        }
    }

    private Task<UpdateResult> ClaimDueVideoPollingAsync(
        Guid videoRequestId,
        DateTime now,
        CancellationToken cancellationToken)
    {
        return context.VideoRequests.UpdateOneAsync(
            x =>
                x.Id == videoRequestId &&
                x.Status == "VIDEO_PROVIDER_POLLING" &&
                x.NextProviderPollAtUtc != null &&
                x.NextProviderPollAtUtc <= now &&
                x.VideoProviderTrackId != null,
            Builders<VideoRequest>.Update
                .Set(x => x.NextProviderPollAtUtc, DateTime.UtcNow.AddMinutes(1))
                .Set(x => x.UpdatedAtUtc, DateTime.UtcNow),
            cancellationToken: cancellationToken);
    }

    private Task ReplaceVideoAsync(VideoRequest request, CancellationToken cancellationToken)
    {
        return context.VideoRequests.ReplaceOneAsync(
            x => x.Id == request.Id,
            request,
            cancellationToken: cancellationToken);
    }
}