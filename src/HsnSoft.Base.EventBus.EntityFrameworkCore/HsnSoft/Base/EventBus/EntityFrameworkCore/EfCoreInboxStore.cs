using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using HsnSoft.Base.EventBus.Inbox;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.EntityFrameworkCore;

/// <summary>
/// <see cref="IInboxStore"/> on the context of the service (the inbox table is in its database, mapped by <see cref="InboxModelBuilderExtensions.ConfigureEventInbox"/>). A message is taken by inserting its row: the id is the key, so of two deliveries at
/// once one insert wins. The other (or a later delivery) finds the row and decides from its status; a failed or abandoned row is taken over by one statement (<c>UPDATE ... WHERE</c> the status and the lease say so), so two callers cannot both take it.
/// An insert that fails for another reason than the row being there is thrown: the message is not dropped because the inbox could not be asked. The dispatcher asks the store in a scope of its own, so this context is not the handler's.
/// </summary>
public sealed class EfCoreInboxStore<TContext>(TContext db, IOptions<EventInboxOptions> options) : IInboxStore
    where TContext : DbContext
{
    private readonly EventInboxOptions _options = options.Value;

    private DbSet<InboxMessage> Messages => db.Set<InboxMessage>();

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

        Messages.Add(message);
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
            return InboxStart.Started;
        }
        catch (DbUpdateException)
        {
            db.Entry(message).State = EntityState.Detached;

            // not because the row is there: a real failure, thrown, so the message is not lost
            if (!await Messages.AsNoTracking().AnyAsync(row => row.Id == messageId, cancellationToken).ConfigureAwait(false))
            {
                throw;
            }
        }

        return await TakeAgainAsync(messageId, now, cancellationToken).ConfigureAwait(false);
    }

    public async Task CompleteAsync(Guid messageId, CancellationToken cancellationToken = default)
    {
        DateTime now = DateTime.UtcNow;
        await Messages.Where(row => row.Id == messageId).ExecuteUpdateAsync(
            set => set.SetProperty(row => row.Status, InboxStatuses.Completed).SetProperty(row => row.ProcessedAt, now).SetProperty(row => row.Error, (string?)null),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task FailAsync(Guid messageId, string reason, CancellationToken cancellationToken = default)
    {
        string text = Truncate(reason ?? string.Empty, InboxMessage.ErrorMaxLength);
        await Messages.Where(row => row.Id == messageId).ExecuteUpdateAsync(
            set => set.SetProperty(row => row.Status, InboxStatuses.Failed).SetProperty(row => row.Error, text),
            cancellationToken).ConfigureAwait(false);
    }

    // the row is there: handled before, or taken over (it failed and may be tried again, or its handling died and the lease ran out), or left alone
    private async Task<InboxStart> TakeAgainAsync(Guid messageId, DateTime now, CancellationToken cancellationToken)
    {
        InboxMessage row = await Messages.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == messageId, cancellationToken).ConfigureAwait(false)
                           ?? throw new InvalidOperationException($"The inbox row of message {messageId} was there and is gone.");

        if (row.Status == InboxStatuses.Completed)
        {
            return InboxStart.AlreadyHandled;
        }

        DateTime leaseEnded = now - _options.ProcessingLease;
        int maxRetries = _options.MaxRetryCount;
        int taken = await Messages
            .Where(candidate => candidate.Id == messageId
                                && ((candidate.Status == InboxStatuses.Failed && candidate.RetryCount < maxRetries)
                                    || (candidate.Status == InboxStatuses.Started && candidate.StartedAt < leaseEnded)))
            .ExecuteUpdateAsync(
                set => set.SetProperty(candidate => candidate.Status, InboxStatuses.Started)
                    .SetProperty(candidate => candidate.RetryCount, candidate => candidate.RetryCount + 1)
                    .SetProperty(candidate => candidate.StartedAt, now)
                    .SetProperty(candidate => candidate.Error, (string?)null),
                cancellationToken).ConfigureAwait(false);
        if (taken == 1)
        {
            return InboxStart.Started;
        }

        // not taken: say why from the row as it is now (another delivery may have taken it, or handled it, since it was read)
        InboxMessage? current = await Messages.AsNoTracking().FirstOrDefaultAsync(candidate => candidate.Id == messageId, cancellationToken).ConfigureAwait(false);
        return current?.Status switch
        {
            InboxStatuses.Completed => InboxStart.AlreadyHandled,
            InboxStatuses.Failed => InboxStart.RetryLimitReached,
            _ => InboxStart.InProgress,
        };
    }

    private static string Truncate(string value, int length) => value.Length <= length ? value : value[..length];
}
