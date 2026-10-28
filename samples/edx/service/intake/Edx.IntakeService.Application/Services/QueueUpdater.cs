using Edx.IntakeService.Domain.QueueDomain.Entities;
using Edx.Shared.Contracts.Events.Invoicing;
using HsnSoft.Base.BlobStoring;
using HsnSoft.Base.Domain.Repositories;
using Microsoft.Extensions.Logging;

namespace Edx.IntakeService.Application.Services;

/// <summary>
/// What became of the invoices of the queue, as the invoice service says it (events): the status of the queue item follows. An event for an item that is not there (it was replaced and deleted, or it belongs to another tenant) is logged and left. A status only moves forward
/// (see <see cref="QueueItem"/>), so an event that comes late does not take an item back. The object of an item that failed is named with <c>error_</c> in front, so what is in the store says so too.
/// </summary>
public sealed class QueueUpdater(IRepository<QueueItem, Guid> queue, IBlobStore blobs, TimeProvider time, ILogger<QueueUpdater> logger)
{
    private const string ErrorPrefix = "error_";

    public Task OnDraftCreatedAsync(InvoiceDraftCreatedEto message, CancellationToken cancellationToken = default)
        => ApplyAsync(message.QueueId, "draft created", (item, at) => item.MarkDraftCreated(at), cancellationToken);

    public Task OnRejectedAsync(InvoiceRejectedEto message, CancellationToken cancellationToken = default)
        => ApplyAsync(message.QueueId, "rejected", (item, at) => item.MarkRejected(message.Reason, at), cancellationToken);

    public Task OnCompletedAsync(InvoiceCompletedEto message, CancellationToken cancellationToken = default)
        => ApplyAsync(message.QueueId, "completed", (item, at) => item.MarkCompleted(at), cancellationToken);

    public async Task OnFailedAsync(InvoiceImportFailedEto message, CancellationToken cancellationToken = default)
    {
        QueueItem? item = await ApplyAsync(message.QueueId, "failed", (found, at) => found.MarkFailed($"{message.Stage}: {message.Reason}", at), cancellationToken).ConfigureAwait(false);
        if (item is { Status: QueueStatus.Failed } && !item.ObjectName.Contains($"/{ErrorPrefix}", StringComparison.Ordinal))
        {
            await RenameAsync(item, cancellationToken).ConfigureAwait(false);
        }
    }

    /// <summary>A correct invoice came and took the place of this one: the item is deleted (softly).</summary>
    public async Task OnSupersededAsync(InvoiceSupersededEto message, CancellationToken cancellationToken = default)
    {
        QueueItem? item = await queue.FindAsync(message.QueueId, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            logger.LogWarning("Queue item {QueueItemId} was replaced and is not in the queue", message.QueueId);
            return;
        }

        await queue.DeleteAsync(item, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Queue item {QueueItemId} of invoice {InvoiceId} was replaced by a correct one and deleted", item.Id, item.InvoiceId);
    }

    private async Task<QueueItem?> ApplyAsync(Guid queueId, string what, Func<QueueItem, DateTimeOffset, bool> change, CancellationToken cancellationToken)
    {
        QueueItem? item = await queue.FindAsync(queueId, cancellationToken).ConfigureAwait(false);
        if (item is null)
        {
            logger.LogWarning("Queue item {QueueItemId} was said {QueueEvent} and is not in the queue", queueId, what);
            return null;
        }

        if (!change(item, time.GetUtcNow()))
        {
            logger.LogInformation("Queue item {QueueItemId} is {QueueStatus}: the news that it is {QueueEvent} came late and is left", queueId, item.Status, what);
            return item;
        }

        await queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        return item;
    }

    // the object is read and written under its new name and the old one is taken away (the store has no move)
    private async Task RenameAsync(QueueItem item, CancellationToken cancellationToken)
    {
        int slash = item.ObjectName.LastIndexOf('/');
        string renamed = slash < 0 ? ErrorPrefix + item.ObjectName : $"{item.ObjectName[..(slash + 1)]}{ErrorPrefix}{item.ObjectName[(slash + 1)..]}";
        Stream? content = await blobs.OpenReadAsync(item.Container, item.ObjectName, cancellationToken).ConfigureAwait(false);
        if (content is null)
        {
            return;
        }

        await using (content.ConfigureAwait(false))
        {
            await blobs.SaveAsync(item.Container, renamed, content, item.ContentType, cancellationToken).ConfigureAwait(false);
        }

        await blobs.DeleteAsync(item.Container, item.ObjectName, cancellationToken).ConfigureAwait(false);
        item.MovedTo(renamed);
        await queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
    }
}
