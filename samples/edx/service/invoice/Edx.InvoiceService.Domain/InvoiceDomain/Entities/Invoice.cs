using Edx.InvoiceService.Domain.InvoiceDomain.Consts;
using Edx.InvoiceService.Domain.InvoiceDomain.Exceptions;
using Edx.Shared.Contracts.Invoicing;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;

namespace Edx.InvoiceService.Domain.InvoiceDomain.Entities;

/// <summary>
/// An invoice of a tenant, from the moment the invoice service takes it from the queue to its signed XML, and the record of each time it was sent. The primary key (<see cref="Entity{TKey}.Id"/>) is the id of the record, new for each time an invoice is sent;
/// <see cref="InvoiceId"/> is the uuid the sender gave the invoice and repeats: an attempt that failed (or was rejected) stays as it is, visible, until a correct one of the same invoice comes and the old ones are deleted softly. At most one record of an invoice is
/// <see cref="IsLive"/> (being processed, a draft, approved or signed) and the persistence keeps it so: a unique index over the live, not deleted records, by tenant and by uuid and by number. The header and the lines are set when the draft is made.
/// The audit fields (who sent it) and the soft delete come from <see cref="FullAuditedEntity{TKey}"/>; the tenant is kept here and given by the persistence.
/// </summary>
public sealed class Invoice : FullAuditedEntity<Guid>, IMultiTenant
{
    // for the database driver
    private Invoice()
    {
    }

    /// <summary>The uuid of the invoice, as the sender gave it.</summary>
    public Guid InvoiceId { get; private set; }

    /// <summary>The number of the invoice, as the sender gave it.</summary>
    public string Number { get; private set; } = string.Empty;

    /// <summary>The number in capitals: what is unique among the live records of a tenant, and what a list looks for.</summary>
    public string NormalizedNumber { get; private set; } = string.Empty;

    /// <summary>The queue item of the intake service this record came from.</summary>
    public Guid QueueId { get; private set; }

    public InvoiceSourceType SourceType { get; private set; }

    public InvoiceChannelType ChannelType { get; private set; }

    public InvoiceStatus Status { get; private set; }

    /// <summary>Whether this record is the one the invoice is at: being processed, a draft, approved or signed. A failed or a rejected record is not.</summary>
    public bool IsLive { get; private set; }

    /// <summary>Where it went wrong (<c>Claim</c>, <c>Fetch</c>, <c>Extract</c>, <c>Validate</c>, <c>Limit</c>, <c>Convert</c>) and why, for a failed record.</summary>
    public string? FailureStage { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset StatusChangedAt { get; private set; }

    // the header and the lines: set when the draft is made
    public DateTime? IssueDate { get; private set; }

    public string? Currency { get; private set; }

    public string? SupplierName { get; private set; }

    public string? SupplierTaxId { get; private set; }

    public string? CustomerName { get; private set; }

    public string? CustomerTaxId { get; private set; }

    public IReadOnlyList<InvoiceLine> Lines { get; private set; } = [];

    /// <summary>What the invoice amounts to: the sum of its lines, with VAT. None until the draft is made.</summary>
    public decimal? Total { get; private set; }

    // where its objects are in the blob store of the invoice service (the container is one setting of the service; the names are kept as they were written)
    public string? UploadPath { get; private set; }

    public string? ExtractedPath { get; private set; }

    public string? DraftPath { get; private set; }

    public string? SignedPath { get; private set; }

    public DateTimeOffset? ApprovedAt { get; private set; }

    public Guid? ApprovedBy { get; private set; }

    public DateTimeOffset? RejectedAt { get; private set; }

    public Guid? RejectedBy { get; private set; }

    public string? RejectionReason { get; private set; }

    public DateTimeOffset? SignedAt { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>A failed or a rejected record: the kind a correct one of the invoice replaces.</summary>
    public bool IsReplaceable => Status is InvoiceStatus.Failed or InvoiceStatus.Rejected;

    /// <summary>The record of an invoice that was taken from the queue: it is being processed, and is the live one of its invoice.</summary>
    public static Invoice Begin(Guid invoiceId, string number, Guid queueId, InvoiceSourceType sourceType, InvoiceChannelType channelType, DateTimeOffset at)
        => New(invoiceId, number, queueId, sourceType, channelType, at, InvoiceStatus.Processing, isLive: true);

    public void SetUploadPath(string path) => UploadPath = Guard.NotNullOrWhiteSpace(path, nameof(UploadPath), InvoiceConsts.PathMaxLength);

    public void SetExtractedPath(string path) => ExtractedPath = Guard.NotNullOrWhiteSpace(path, nameof(ExtractedPath), InvoiceConsts.PathMaxLength);

    /// <summary>The paths of the objects of an attempt that failed were renamed (they start with <c>error_</c>).</summary>
    public void MovedObjects(string? uploadPath, string? extractedPath)
    {
        UploadPath = uploadPath;
        ExtractedPath = extractedPath;
    }

    /// <summary>The draft XML was made from what the sender sent: the header and the lines are kept and the record waits for a person.</summary>
    public void MarkDrafted(InvoiceSubmissionDto submission, string draftPath, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(submission);

        if (Status != InvoiceStatus.Processing)
        {
            throw new InvoiceStateException(Id, Status, "drafted");
        }

        IssueDate = submission.IssueDate.Date;
        Currency = Guard.Length(Guard.NotNullOrWhiteSpace(submission.Currency, nameof(Currency), 3).ToUpperInvariant(), nameof(Currency), 3, 3)!;
        SupplierName = Guard.NotNullOrWhiteSpace(submission.SupplierName, nameof(SupplierName), InvoiceConsts.NameMaxLength);
        SupplierTaxId = Optional(submission.SupplierTaxId, nameof(SupplierTaxId));
        CustomerName = Guard.NotNullOrWhiteSpace(submission.CustomerName, nameof(CustomerName), InvoiceConsts.NameMaxLength);
        CustomerTaxId = Optional(submission.CustomerTaxId, nameof(CustomerTaxId));
        Lines = submission.Lines is { Count: > 0 and <= InvoiceConsts.MaxLines }
            ? [.. submission.Lines.Select(line => InvoiceLine.Of(line.Description, line.Quantity, line.UnitPrice, line.VatRate))]
            : throw new DomainException($"An invoice has from 1 to {InvoiceConsts.MaxLines} lines.").WithKey("invoice_lines_invalid", InvoiceConsts.MaxLines);
        Total = Lines.Sum(line => line.Total);
        DraftPath = Guard.NotNullOrWhiteSpace(draftPath, nameof(DraftPath), InvoiceConsts.PathMaxLength);
        Move(InvoiceStatus.DraftCreated, at, live: true);
    }

    public void Fail(string stage, string reason, DateTimeOffset at)
    {
        FailureStage = stage;
        FailureReason = Guard.Length(reason, nameof(reason), InvoiceConsts.ReasonMaxLength);
        Move(InvoiceStatus.Failed, at, live: false);
    }

    /// <summary>A person approved the draft: from then on it is signed and not changed.</summary>
    public void Approve(DateTimeOffset at, Guid? by)
    {
        Require(InvoiceStatus.DraftCreated, "approved");
        (ApprovedAt, ApprovedBy) = (at, by);
        Move(InvoiceStatus.Approved, at, live: true);
    }

    /// <summary>A person rejected the draft: it is not live any more, and the same number may be sent again.</summary>
    public void Reject(string reason, DateTimeOffset at, Guid? by)
    {
        Require(InvoiceStatus.DraftCreated, "rejected");
        RejectionReason = Guard.Length(Guard.NotNullOrWhiteSpace(reason, nameof(reason)), nameof(reason), InvoiceConsts.ReasonMaxLength);
        (RejectedAt, RejectedBy) = (at, by);
        Move(InvoiceStatus.Rejected, at, live: false);
    }

    /// <summary>The signed XML was written: the draft is gone and the record is done.</summary>
    public void Sign(string signedPath, DateTimeOffset at)
    {
        Require(InvoiceStatus.Approved, "signed");
        SignedPath = Guard.NotNullOrWhiteSpace(signedPath, nameof(SignedPath), InvoiceConsts.PathMaxLength);
        DraftPath = null;
        SignedAt = at;
        Move(InvoiceStatus.Signed, at, live: true);
    }

    private static Invoice New(Guid invoiceId, string number, Guid queueId, InvoiceSourceType sourceType, InvoiceChannelType channelType, DateTimeOffset at, InvoiceStatus status, bool isLive)
    {
        string text = Guard.NotNullOrWhiteSpace(number, nameof(Number), InvoiceConsts.NumberMaxLength);
        return new Invoice
        {
            Id = Guid.CreateVersion7(),
            InvoiceId = Guard.NotDefault<Guid>(invoiceId, nameof(InvoiceId)),
            Number = text,
            NormalizedNumber = StringHelper.Normalize(text),
            QueueId = queueId,
            SourceType = sourceType,
            ChannelType = channelType,
            Status = status,
            IsLive = isLive,
            StatusChangedAt = at,
        };
    }

    private void Require(InvoiceStatus status, string what)
    {
        if (Status != status)
        {
            throw new InvoiceStateException(Id, Status, what);
        }
    }

    private void Move(InvoiceStatus status, DateTimeOffset at, bool live)
    {
        Status = status;
        IsLive = live;
        StatusChangedAt = at;
    }

    private static string? Optional(string? text, string name) => string.IsNullOrWhiteSpace(text) ? null : Guard.Length(text.Trim(), name, InvoiceConsts.TaxIdMaxLength);
}
