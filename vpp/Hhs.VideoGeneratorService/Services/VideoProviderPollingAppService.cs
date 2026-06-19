using Hhs.Shared.Events;
using Hhs.Shared.RabbitMQ;
using Hhs.VideoGeneratorService.Configuration;
using Hhs.VideoGeneratorService.Entities;
using Hhs.VideoGeneratorService.Mongo;
using Hhs.VideoGeneratorService.Providers;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Services;

public sealed class VideoProviderPollingAppService(
    VideoMongoContext context,
    IVideoProviderResolver videoProviderResolver,
    IEventBus eventBus,
    ILogger<VideoProviderPollingAppService> logger,
    HttpClient httpClient,
    IOptions<ProviderEndpointsOptions> options)
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

                    await eventBus.PublishAsync(new StepFailedEto
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

                    await eventBus.PublishAsync(new StepFailedEto
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
                    request.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(5);
                    request.UpdatedAtUtc = DateTime.UtcNow;

                    await ReplaceVideoAsync(request, cancellationToken);
                    continue;
                }

                if (string.IsNullOrWhiteSpace(status.ProviderFileUrl))
                    throw new InvalidOperationException("Video provider completed but file url is empty.");

                request.ProviderFileName = status.FileName;

                // Download file from provider and upload to mock storage
                var localFileName = !string.IsNullOrWhiteSpace(status.FileName)
                    ? $"local_{status.FileName}"
                    : $"local_video_{request.Id:N}.mp4";
                var mockStorageUrl = await DownloadAndUploadToStorageAsync(
                    status.ProviderFileUrl,
                    localFileName,
                    cancellationToken);

                request.ProviderPollingCount++;
                request.NextProviderPollAtUtc = null;
                request.ProviderVideoFileUrl = mockStorageUrl;
                request.Status = "VIDEO_PROVIDER_COMPLETED";
                request.CurrentStep = EventNames.VideoProviderCompleted;
                request.LastError = null;
                request.UpdatedAtUtc = DateTime.UtcNow;

                await ReplaceVideoAsync(request, cancellationToken);

                await eventBus.PublishAsync(new VideoProviderCompletedEto
                {
                    CustomerContentId = request.CustomerContentId,
                    AnalysisContentId = request.AnalysisContentId,
                    ContentProcessType = request.ContentProcessType,
                    CorrelationId = request.CorrelationId,
                    VideoRequestId = request.Id,
                    ProviderFileUrl = mockStorageUrl
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

                    await eventBus.PublishAsync(new StepFailedEto
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
                    request.NextProviderPollAtUtc = DateTime.UtcNow.AddSeconds(5);
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
                .Set(x => x.NextProviderPollAtUtc, DateTime.UtcNow.AddSeconds(5))
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

    private async Task<string> DownloadAndUploadToStorageAsync(
        string downloadUrl,
        string fileName,
        CancellationToken cancellationToken)
    {
        try
        {
            // Download file from provider
            var fileContent = await httpClient.GetByteArrayAsync(downloadUrl, cancellationToken);

            // Upload to mock storage
            var storageUrl = $"{options.Value.StorageBaseUrl}/storage/upload-binary";
            using (var content = new ByteArrayContent(fileContent))
            {
                var response = await httpClient.PostAsync(
                    $"{storageUrl}?fileName={fileName}",
                    content,
                    cancellationToken);

                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
                var jsonDoc = System.Text.Json.JsonDocument.Parse(responseJson);
                var remoteUrl = jsonDoc.RootElement.GetProperty("url").GetString();

                return remoteUrl ?? throw new InvalidOperationException("No URL in storage response");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to download and upload video file from {DownloadUrl}", downloadUrl);
            throw;
        }
    }
}