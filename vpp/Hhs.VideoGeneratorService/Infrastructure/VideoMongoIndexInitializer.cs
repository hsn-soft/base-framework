using Hhs.VideoGeneratorService.Entities;
using Hhs.VideoGeneratorService.Mongo;
using MongoDB.Driver;

namespace Hhs.VideoGeneratorService.Infrastructure;

public sealed class VideoMongoIndexInitializer(VideoMongoContext context)
{
    public async Task CreateIndexesAsync(CancellationToken cancellationToken = default)
    {
        await CreateVideoRequestIndexesAsync(cancellationToken);
        await CreateAudioRequestIndexesAsync(cancellationToken);
        await CreateInboxIndexesAsync(cancellationToken);
    }

    private async Task CreateVideoRequestIndexesAsync(CancellationToken cancellationToken)
    {
        var indexes = new List<CreateIndexModel<VideoRequest>>
        {
            new(
                Builders<VideoRequest>.IndexKeys.Ascending(x => x.CorrelationId),
                new CreateIndexOptions { Name = "ix_video_requests_correlation_id" }
            ),

            new(
                Builders<VideoRequest>.IndexKeys.Ascending(x => x.CustomerContentId),
                new CreateIndexOptions { Name = "ix_video_requests_customer_content_id" }
            ),

            new(
                Builders<VideoRequest>.IndexKeys.Ascending(x => x.AnalysisContentId),
                new CreateIndexOptions { Name = "ix_video_requests_analysis_content_id" }
            ),

            new(
                Builders<VideoRequest>.IndexKeys
                    .Ascending(x => x.Status)
                    .Ascending(x => x.NextRetryAtUtc),
                new CreateIndexOptions { Name = "ix_video_requests_retry_due" }
            ),

            new(
                Builders<VideoRequest>.IndexKeys
                    .Ascending(x => x.Status)
                    .Ascending(x => x.NextProviderPollAtUtc)
                    .Ascending(x => x.VideoProviderTrackId),
                new CreateIndexOptions { Name = "ix_video_requests_provider_poll_due" }
            ),

            new(
                Builders<VideoRequest>.IndexKeys
                    .Ascending(x => x.VideoProviderKey)
                    .Ascending(x => x.Status),
                new CreateIndexOptions { Name = "ix_video_requests_provider_status" }
            )
        };

        await context.VideoRequests.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task CreateAudioRequestIndexesAsync(CancellationToken cancellationToken)
    {
        var indexes = new List<CreateIndexModel<AudioRequest>>
        {
            new(
                Builders<AudioRequest>.IndexKeys.Ascending(x => x.CorrelationId),
                new CreateIndexOptions { Name = "ix_audio_requests_correlation_id" }
            ),

            new(
                Builders<AudioRequest>.IndexKeys.Ascending(x => x.VideoRequestId),
                new CreateIndexOptions { Name = "ix_audio_requests_video_request_id" }
            ),

            new(
                Builders<AudioRequest>.IndexKeys
                    .Ascending(x => x.VideoRequestId)
                    .Ascending(x => x.SortOrder),
                new CreateIndexOptions { Name = "ix_audio_requests_video_request_sort_order" }
            ),

            new(
                Builders<AudioRequest>.IndexKeys.Ascending(x => x.CustomerContentId),
                new CreateIndexOptions { Name = "ix_audio_requests_customer_content_id" }
            ),

            new(
                Builders<AudioRequest>.IndexKeys.Ascending(x => x.AnalysisContentId),
                new CreateIndexOptions { Name = "ix_audio_requests_analysis_content_id" }
            ),

            new(
                Builders<AudioRequest>.IndexKeys
                    .Ascending(x => x.Status)
                    .Ascending(x => x.NextRetryAtUtc),
                new CreateIndexOptions { Name = "ix_audio_requests_retry_due" }
            ),

            new(
                Builders<AudioRequest>.IndexKeys
                    .Ascending(x => x.Status)
                    .Ascending(x => x.NextProviderPollAtUtc)
                    .Ascending(x => x.AudioProviderTrackId),
                new CreateIndexOptions { Name = "ix_audio_requests_provider_poll_due" }
            ),

            new(
                Builders<AudioRequest>.IndexKeys
                    .Ascending(x => x.AudioProviderKey)
                    .Ascending(x => x.Status),
                new CreateIndexOptions { Name = "ix_audio_requests_provider_status" }
            )
        };

        await context.AudioRequests.Indexes.CreateManyAsync(indexes, cancellationToken);
    }

    private async Task CreateInboxIndexesAsync(CancellationToken cancellationToken)
    {
        var indexes = new List<CreateIndexModel<VideoGeneratorInboxMessage>>
        {
            new(
                Builders<VideoGeneratorInboxMessage>.IndexKeys
                    .Ascending(x => x.Status)
                    .Ascending(x => x.CreatedAtUtc),
                new CreateIndexOptions { Name = "ix_video_inbox_status_created_at" }
            ),

            new(
                Builders<VideoGeneratorInboxMessage>.IndexKeys
                    .Ascending(x => x.EventName)
                    .Ascending(x => x.CreatedAtUtc),
                new CreateIndexOptions { Name = "ix_video_inbox_event_name_created_at" }
            )
        };

        await context.InboxMessages.Indexes.CreateManyAsync(indexes, cancellationToken);
    }
}