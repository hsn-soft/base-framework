using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.Inbox;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Bson;
using MongoDB.Driver;

namespace HsnSoft.Base.EventBus.MongoDB;

/// <summary>
/// Makes MongoDB forget the handled messages by itself: a TTL index on <c>ProcessedAt</c> that only the <see cref="InboxStatuses.Completed"/> documents are in (a partial index), so a started or failed document, which holds a lease or a retry
/// count, is never removed. MongoDB removes the documents on the server, one pass a minute, so no instance of the service has anything to run; the index is created at start (when it exists with another retention it is changed in place).
/// </summary>
public sealed partial class MongoInboxRetention(IMongoCollection<InboxMessage> collection, IOptions<EventInboxCleanupOptions> options, ILogger<MongoInboxRetention> logger) : BackgroundService
{
    internal const string IndexName = "inbox_processed_ttl";
    private static readonly TimeSpan s_retry = TimeSpan.FromSeconds(30);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        TimeSpan retention = options.Value.RetentionPeriod;
        if (retention <= TimeSpan.Zero)
        {
            return;
        }

        // a database that is not there yet when the service starts must not stop it: the index is tried again until it is made
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await EnsureIndexAsync(retention, stoppingToken).ConfigureAwait(false);
                LogIndexReady(logger, collection.CollectionNamespace.CollectionName, retention.TotalDays);
                return;
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                LogIndexFailed(logger, exception, collection.CollectionNamespace.CollectionName);
                await Task.Delay(s_retry, stoppingToken).ConfigureAwait(false);
            }
        }
    }

    internal async Task EnsureIndexAsync(TimeSpan retention, CancellationToken cancellationToken)
    {
        CreateIndexModel<InboxMessage> model = new(
            Builders<InboxMessage>.IndexKeys.Ascending(row => row.ProcessedAt),
            new CreateIndexOptions<InboxMessage>
            {
                Name = IndexName,
                ExpireAfter = retention,
                PartialFilterExpression = Builders<InboxMessage>.Filter.Eq(row => row.Status, InboxStatuses.Completed),
            });
        try
        {
            await collection.Indexes.CreateOneAsync(model, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (MongoCommandException exception) when (exception.Code is 85 or 86)
        {
            // the index is there with another retention: it is changed in place (the same index, another expireAfterSeconds)
            BsonDocument command = new()
            {
                { "collMod", collection.CollectionNamespace.CollectionName },
                { "index", new BsonDocument { { "name", IndexName }, { "expireAfterSeconds", (long)retention.TotalSeconds } } },
            };
            await collection.Database.RunCommandAsync<BsonDocument>(command, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
    }

    [LoggerMessage(EventId = 4701, EventName = "EVENT_INBOX_TTL_READY", Level = LogLevel.Information, Message = "{Collection}: MongoDB forgets the messages handled more than {Days} day(s) ago")]
    private static partial void LogIndexReady(ILogger logger, string collection, double days);

    [LoggerMessage(EventId = 4702, EventName = "EVENT_INBOX_TTL_FAILED", Level = LogLevel.Warning, Message = "{Collection}: the retention index of the inbox could not be made, trying again")]
    private static partial void LogIndexFailed(ILogger logger, Exception exception, string collection);
}
