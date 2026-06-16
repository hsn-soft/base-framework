using Hhs.TextNormalizerService.Entities;
using Hhs.TextNormalizerService.Mongo;
using MongoDB.Driver;

namespace Hhs.TextNormalizerService.Infrastructure;

public sealed class NormalizerMongoIndexInitializer(NormalizerMongoContext context)
{
    public async Task CreateIndexesAsync(CancellationToken cancellationToken)
    {
        await context.CustomerRequests.Indexes.CreateOneAsync(
            new CreateIndexModel<CustomerContentNormalizedRequest>(
                Builders<CustomerContentNormalizedRequest>.IndexKeys.Ascending(x => x.SourceEventId),
                new CreateIndexOptions
                {
                    Name = "ux_customer_normalized_source_event_id",
                    Unique = true
                }),
            cancellationToken: cancellationToken);

        await context.AnalysisRequests.Indexes.CreateOneAsync(
            new CreateIndexModel<AnalysisContentNormalizedRequest>(
                Builders<AnalysisContentNormalizedRequest>.IndexKeys.Ascending(x => x.SourceEventId),
                new CreateIndexOptions
                {
                    Name = "ux_analysis_normalized_source_event_id",
                    Unique = true
                }),
            cancellationToken: cancellationToken);

        await context.InboxMessages.Indexes.CreateOneAsync(
            new CreateIndexModel<NormalizerInboxMessage>(
                Builders<NormalizerInboxMessage>.IndexKeys.Ascending(x => x.EventId),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        await context.CustomerRequests.Indexes.CreateOneAsync(
            new CreateIndexModel<CustomerContentNormalizedRequest>(
                Builders<CustomerContentNormalizedRequest>.IndexKeys.Ascending(x => x.CustomerContentId),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        await context.AnalysisRequests.Indexes.CreateOneAsync(
            new CreateIndexModel<AnalysisContentNormalizedRequest>(
                Builders<AnalysisContentNormalizedRequest>.IndexKeys.Ascending(x => x.AnalysisContentId),
                new CreateIndexOptions { Unique = true }),
            cancellationToken: cancellationToken);

        await context.AnalysisRequests.Indexes.CreateOneAsync(
            new CreateIndexModel<AnalysisContentNormalizedRequest>(
                Builders<AnalysisContentNormalizedRequest>.IndexKeys
                    .Ascending(x => x.Status)
                    .Ascending("Items.OutlineStatus")
                    .Ascending("Items.NextOutlinePollAtUtc")
                    .Ascending("Items.OutlineProviderTrackId")),
            cancellationToken: cancellationToken);

        await context.CustomerRequests.Indexes.CreateOneAsync(
            new CreateIndexModel<CustomerContentNormalizedRequest>(
                Builders<CustomerContentNormalizedRequest>.IndexKeys
                    .Ascending(x => x.Status)
                    .Ascending(x => x.NextRetryAtUtc)),
            cancellationToken: cancellationToken);

        await context.CustomerRequests.Indexes.CreateOneAsync(
            new CreateIndexModel<CustomerContentNormalizedRequest>(
                Builders<CustomerContentNormalizedRequest>.IndexKeys
                    .Ascending(x => x.Status)
                    .Ascending(x => x.NextOutlinePollAtUtc)),
            cancellationToken: cancellationToken);
    }
}