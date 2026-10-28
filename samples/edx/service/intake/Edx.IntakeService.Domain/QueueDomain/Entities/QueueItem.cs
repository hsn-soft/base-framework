using Edx.IntakeService.Domain.QueueDomain.Consts;
using Edx.Shared.Contracts.Invoicing;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Edx.IntakeService.Domain.QueueDomain.Entities;

/// <summary>
/// The record of an invoice that was received and kept in the blob store, and of what became of it: the queue. The primary key is the id of the queue item (new for each time an invoice is sent); <see cref="InvoiceId"/> is the uuid the sender gave the invoice and repeats when it is sent
/// again, so the items of an invoice are found by it. An item that failed or was rejected stays (it is shown) until a correct one of the same invoice comes, and is then deleted softly. The bytes are not here. The audit fields and the soft delete come from
/// <see cref="FullAuditedEntity{TKey}"/> (who sent it, when, who deleted it); the tenant is kept here and given by the persistence.
/// </summary>
public sealed class QueueItem : FullAuditedEntity<Guid>, IMultiTenant
{
    // for the database driver
    private QueueItem()
    {
    }

    public Guid InvoiceId { get; private set; }

    public string InvoiceNumber { get; private set; } = string.Empty;

    /// <summary>The host of the intake service that received the invoice (the REST, the gRPC or the WCF one).</summary>
    public string ReceivedBy { get; private set; } = string.Empty;

    public InvoiceSourceType SourceType { get; private set; }

    public InvoiceChannelType ChannelType { get; private set; }

    /// <summary>The SHA-256 (hex) of the bytes that were kept: the zip the sender made (it gave this hash, and the service checked it), or the model file.</summary>
    public string FileHash { get; private set; } = string.Empty;

    /// <summary>The name the sender gave the file (cleaned of any path), or the name the service gave a model.</summary>
    public string FileName { get; private set; } = string.Empty;

    public string ContentType { get; private set; } = string.Empty;

    public long SizeBytes { get; private set; }

    /// <summary>The name of the one file inside the zip; none for a model.</summary>
    public string? EntryName { get; private set; }

    public Guid FolderId { get; private set; }

    public string FolderPath { get; private set; } = string.Empty;

    public string Container { get; private set; } = string.Empty;

    /// <summary>The name of the object in the container; it starts with <c>error_</c> (after the folders) when the item failed.</summary>
    public string ObjectName { get; private set; } = string.Empty;

    public QueueStatus Status { get; private set; }

    /// <summary>Why the item failed or was rejected.</summary>
    public string? StatusReason { get; private set; }

    public DateTimeOffset StatusChangedAt { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>Whether the invoice got through (a draft was made, or it is completed): such an item is what a question about the invoice is answered with.</summary>
    public bool IsSuccessful => Status is QueueStatus.DraftCreated or QueueStatus.Completed;

    public static QueueItem Create(
        Guid id,
        Guid invoiceId,
        string invoiceNumber,
        string receivedBy,
        InvoiceSourceType sourceType,
        InvoiceChannelType channelType,
        string fileHash,
        string fileName,
        string contentType,
        long sizeBytes,
        string? entryName,
        Guid folderId,
        string folderPath,
        string container,
        string objectName,
        DateTimeOffset at)
        => new()
        {
            Id = id,
            InvoiceId = Guard.NotDefault<Guid>(invoiceId, nameof(InvoiceId)),
            InvoiceNumber = Guard.NotNullOrWhiteSpace(invoiceNumber, nameof(InvoiceNumber), QueueItemConsts.InvoiceNumberMaxLength),
            ReceivedBy = Guard.NotNullOrWhiteSpace(receivedBy, nameof(ReceivedBy), QueueItemConsts.ServiceNameMaxLength),
            SourceType = sourceType,
            ChannelType = channelType,
            FileHash = Guard.NotNullOrWhiteSpace(fileHash, nameof(FileHash), 128),
            FileName = Guard.NotNullOrWhiteSpace(fileName, nameof(FileName), QueueItemConsts.FileNameMaxLength),
            ContentType = Guard.NotNullOrWhiteSpace(contentType, nameof(ContentType)),
            SizeBytes = Guard.NotNegative(sizeBytes, nameof(SizeBytes)),
            EntryName = entryName,
            FolderId = folderId,
            FolderPath = folderPath,
            Container = Guard.NotNullOrWhiteSpace(container, nameof(Container)),
            ObjectName = Guard.NotNullOrWhiteSpace(objectName, nameof(ObjectName), QueueItemConsts.ObjectNameMaxLength),
            Status = QueueStatus.Queued,
            StatusChangedAt = at,
        };

    // The events of the invoice service come on topics of their own, so their order is not the order things happened in: a status only moves forward (Queued, then DraftCreated, then Rejected or Completed; Failed only from Queued),
    // and an event that would take it back is not applied (it says so by returning false).
    public bool MarkDraftCreated(DateTimeOffset at) => Move(QueueStatus.DraftCreated, null, at, Status == QueueStatus.Queued);

    public bool MarkRejected(string reason, DateTimeOffset at) => Move(QueueStatus.Rejected, reason, at, Status is QueueStatus.Queued or QueueStatus.DraftCreated);

    public bool MarkFailed(string reason, DateTimeOffset at) => Move(QueueStatus.Failed, reason, at, Status == QueueStatus.Queued);

    public bool MarkCompleted(DateTimeOffset at) => Move(QueueStatus.Completed, null, at, Status is QueueStatus.Queued or QueueStatus.DraftCreated);

    /// <summary>The object was moved to another name (a failed item's object is named with <c>error_</c> in front).</summary>
    public void MovedTo(string objectName) => ObjectName = Guard.NotNullOrWhiteSpace(objectName, nameof(ObjectName), QueueItemConsts.ObjectNameMaxLength);

    private bool Move(QueueStatus status, string? reason, DateTimeOffset at, bool allowed)
    {
        if (!allowed)
        {
            return false;
        }

        Status = status;
        StatusReason = string.IsNullOrWhiteSpace(reason) ? null : Guard.Length(reason.Trim(), nameof(StatusReason), QueueItemConsts.ReasonMaxLength);
        StatusChangedAt = at;
        return true;
    }
}
