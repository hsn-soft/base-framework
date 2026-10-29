using System;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.Inbox;
using Microsoft.Extensions.Options;
using MongoDB.Driver;

namespace HsnSoft.Base.EventBus.MongoDB;

/// <summary>
/// <see cref="IInboxStore"/> on a collection of the database of the service. A message is taken by inserting its document (the id of the message is <c>_id</c>, so of two deliveries at once one insert wins). The other (or a later delivery) finds the
/// document and decides from its status; a failed or abandoned one is taken over by one conditional update (the status and the lease are in the filter), so two callers cannot both take it. An insert that fails for another reason than
/// the document being there is thrown: the message is not dropped because the inbox could not be asked.
/// </summary>
public sealed class MongoInboxStore(IMongoCollection<InboxMessage> collection, IOptions<EventInboxOptions> options) : IInboxStore
{
    private readonly EventInboxOptions _options = options.Value;

    public async Task<InboxStart> TryStartAsync(Guid messageId, string eventName, string? correlationId, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(eventName);

        DateTime now = DateTime.UtcNow;
        InboxMessage message = new()
        {
            Id = messageId,
            EventName = Truncate(eventName, InboxMessage.EventNameMaxLength),
            CorrelationId = correlationId is null ? null : Truncate(correlationId, InboxMessage.CorrelationIdMaxLength),
            Status = InboxStatuses.Started,
            StartedAt = now,
            CreatedAt = now,
        };

        try
        {
            await collection.InsertOneAsync(message, cancellationToken: cancellationToken).ConfigureAwait(false);
            return InboxStart.Started;
        }
        catch (MongoWriteException)
        {
            // not because the document is there: a real failure, thrown, so the message is not lost
            if (!await collection.Find(row => row.Id == messageId).AnyAsync(cancellationToken).ConfigureAwait(false))
            {
                throw;
            }
        }

        return await TakeAgainAsync(messageId, now, cancellationToken).ConfigureAwait(false);
    }

    public Task CompleteAsync(Guid messageId, CancellationToken cancellationToken = default)
        => collection.UpdateOneAsync(
            row => row.Id == messageId,
            Builders<InboxMessage>.Update.Set(row => row.Status, InboxStatuses.Completed).Set(row => row.ProcessedAt, DateTime.UtcNow).Set(row => row.Error, null),
            cancellationToken: cancellationToken);

    public Task FailAsync(Guid messageId, string reason, CancellationToken cancellationToken = default)
        => collection.UpdateOneAsync(
            row => row.Id == messageId,
            Builders<InboxMessage>.Update.Set(row => row.Status, InboxStatuses.Failed).Set(row => row.Error, Truncate(reason ?? string.Empty, InboxMessage.ErrorMaxLength)),
            cancellationToken: cancellationToken);

    // the document is there: handled before, or taken over (it failed and may be tried again, or its handling died and the lease ran out), or left alone
    private async Task<InboxStart> TakeAgainAsync(Guid messageId, DateTime now, CancellationToken cancellationToken)
    {
        InboxMessage row = await collection.Find(candidate => candidate.Id == messageId).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false)
                           ?? throw new InvalidOperationException($"The inbox document of message {messageId} was there and is gone.");
        if (row.Status == InboxStatuses.Completed)
        {
            return InboxStart.AlreadyHandled;
        }

        DateTime leaseEnded = now - _options.ProcessingLease;
        int maxRetries = _options.MaxRetryCount;
        FilterDefinitionBuilder<InboxMessage> filter = Builders<InboxMessage>.Filter;
        UpdateResult taken = await collection.UpdateOneAsync(
            filter.And(
                filter.Eq(candidate => candidate.Id, messageId),
                filter.Or(
                    filter.And(filter.Eq(candidate => candidate.Status, InboxStatuses.Failed), filter.Lt(candidate => candidate.RetryCount, maxRetries)),
                    filter.And(filter.Eq(candidate => candidate.Status, InboxStatuses.Started), filter.Lt(candidate => candidate.StartedAt, leaseEnded)))),
            Builders<InboxMessage>.Update.Set(candidate => candidate.Status, InboxStatuses.Started).Inc(candidate => candidate.RetryCount, 1).Set(candidate => candidate.StartedAt, now).Set(candidate => candidate.Error, null),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        if (taken.ModifiedCount == 1)
        {
            return InboxStart.Started;
        }

        // not taken: say why from the document as it is now (another delivery may have taken it, or handled it, since it was read)
        InboxMessage? current = await collection.Find(candidate => candidate.Id == messageId).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        return current?.Status switch
        {
            InboxStatuses.Completed => InboxStart.AlreadyHandled,
            InboxStatuses.Failed => InboxStart.RetryLimitReached,
            _ => InboxStart.InProgress,
        };
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
}
