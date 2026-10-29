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
/// <see cref="SalesInvoice.IsLive"/> (being processed, a draft, approved or signed) and the persistence keeps it so: a unique index over the live, not deleted records, by tenant and by uuid and by number. The header and the lines are set when the draft is made.
/// The audit fields (who sent it) and the soft delete come from <see cref="FullAuditedEntity{TKey}"/>; the tenant is kept here and given by the persistence.
/// </summary>
public sealed class SalesInvoice : FullAuditedEntity<Guid>, IMultiTenant
{
    public const string HistoryKindStatus = "Status";
    public const string HistoryKindEnvelope = "Envelope";
    private const int TerminalRank = 3;

    // for the database driver
    private SalesInvoice()
    {
    }

    /// <summary>The uuid of the invoice, as the sender gave it.</summary>
    public Guid InvoiceId { get; private set; }

    /// <summary>The number of the invoice, as the sender gave it.</summary>
    public string Number { get; private set; } = string.Empty;

    /// <summary>The number in capitals: what is unique among the live records of a tenant, and what a list looks for.</summary>
    public string NormalizedNumber { get; private set; } = string.Empty;

    /// <summary>The queue item of the firm invoice integration this record came from.</summary>
    public Guid QueueId { get; private set; }

    public InvoiceSourceType SourceType { get; private set; }

    public InvoiceChannelType ChannelType { get; private set; }

    public SalesInvoiceStatus Status { get; private set; }

    /// <summary>Whether this record is the one the invoice is at: being processed, a draft, approved or signed. A failed or a rejected record is not.</summary>
    public bool IsLive { get; private set; }

    /// <summary>Where it went wrong (<c>Claim</c>, <c>Fetch</c>, <c>Extract</c>, <c>Validate</c>, <c>Limit</c>, <c>Convert</c>) and why, for a failed record.</summary>
    public string? FailureStage { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset StatusChangedAt { get; private set; }

    // the header and the lines: set when the draft is made
    public DateTime? IssueDate { get; private set; }

    public string? Currency { get; private set; }

    /// <summary>The scenario of the invoice: <c>TEMELFATURA</c> (it only has to reach the receiver) or <c>TICARIFATURA</c> (the receiver answers it: see <see cref="Responses"/>).</summary>
    public string? Profile { get; private set; }

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

    /// <summary>The uuid of the envelope the invoice was sent in (the last one).</summary>
    public Guid? EnvelopeId { get; private set; }

    /// <summary>The envelope's side of the invoice, as the GİB invoice integration told it (see <see cref="InvoiceEnvelopeState"/>): a summary, the envelope is the integration's.</summary>
    public InvoiceEnvelopeState EnvelopeState { get; private set; }

    /// <summary>The last code of the center about the envelope (1000, 1200, 1220, 1300, a refusal...).</summary>
    public int? EnvelopeStatusCode { get; private set; }

    public string? EnvelopeStatusText { get; private set; }

    /// <summary>Where and why the envelope failed, when it did.</summary>
    public string? EnvelopeNote { get; private set; }

    public DateTimeOffset? EnvelopeStatusAt { get; private set; }

    /// <summary>What happened to the invoice, in order: each status it moved to and each state its envelope reached.</summary>
    public IReadOnlyList<InvoiceHistoryEntry> History { get; private set; } = [];

    /// <summary>The answers of the receiver (a commercial invoice only); the shape is here, the flow is not.</summary>
    public IReadOnlyList<InvoiceResponse> Responses { get; private set; } = [];

    public Guid TenantId { get; private set; }

    /// <summary>A failed or a rejected record: the kind a correct one of the invoice replaces. (A signed invoice stays, whatever became of its envelope.)</summary>
    public bool IsReplaceable => Status is SalesInvoiceStatus.Failed or SalesInvoiceStatus.Rejected;

    /// <summary>The record of an invoice that was taken from the queue: it is being processed, and is the live one of its invoice.</summary>
    public static SalesInvoice Begin(Guid invoiceId, string number, Guid queueId, InvoiceSourceType sourceType, InvoiceChannelType channelType, DateTimeOffset at)
        => New(invoiceId, number, queueId, sourceType, channelType, at, SalesInvoiceStatus.Processing, isLive: true);

    public void SetUploadPath(string path) => UploadPath = Guard.NotNullOrWhiteSpace(path, nameof(UploadPath), InvoiceConsts.PathMaxLength);

    public void SetExtractedPath(string path) => ExtractedPath = Guard.NotNullOrWhiteSpace(path, nameof(ExtractedPath), InvoiceConsts.PathMaxLength);

    /// <summary>The paths of the objects of an attempt that failed were renamed (they start with <c>error_</c>).</summary>
    public void MovedObjects(string? uploadPath, string? extractedPath)
    {
        UploadPath = uploadPath;
        ExtractedPath = extractedPath;
    }

    /// <summary>The draft XML (the UBL-TR invoice) was made: what it says is kept (the header, the lines, the total) and the record waits for a person.</summary>
    public void MarkDrafted(InvoiceDraftInfo draft, string draftPath, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(draft);

        if (Status != SalesInvoiceStatus.Processing)
        {
            throw new InvoiceStateException(Id, Status, "drafted");
        }

        IssueDate = draft.IssueDate.Date;
        Currency = Guard.Length(Guard.NotNullOrWhiteSpace(draft.Currency, nameof(Currency), 3).ToUpperInvariant(), nameof(Currency), 3, 3)!;
        Profile = Guard.NotNullOrWhiteSpace(draft.Profile, nameof(Profile), 16);
        SupplierName = Guard.NotNullOrWhiteSpace(draft.SupplierName, nameof(SupplierName), InvoiceConsts.NameMaxLength);
        SupplierTaxId = Guard.NotNullOrWhiteSpace(draft.SupplierRegisterNumber, nameof(SupplierTaxId), InvoiceConsts.TaxIdMaxLength);
        CustomerName = Guard.NotNullOrWhiteSpace(draft.CustomerName, nameof(CustomerName), InvoiceConsts.NameMaxLength);
        CustomerTaxId = Guard.NotNullOrWhiteSpace(draft.CustomerRegisterNumber, nameof(CustomerTaxId), InvoiceConsts.TaxIdMaxLength);
        Lines = draft.Lines is { Count: > 0 and <= InvoiceConsts.MaxLines }
            ? [.. draft.Lines.Select(line => InvoiceLine.Of(line.Description, line.Quantity, line.UnitPrice, line.VatRate, line.Total))]
            : throw new DomainException($"An invoice has from 1 to {InvoiceConsts.MaxLines} lines.").WithKey("invoice_lines_invalid", InvoiceConsts.MaxLines);
        Total = Guard.Range(draft.Total, nameof(Total), 0m, 100_000_000_000m);
        DraftPath = Guard.NotNullOrWhiteSpace(draftPath, nameof(DraftPath), InvoiceConsts.PathMaxLength);
        Move(SalesInvoiceStatus.DraftCreated, at, live: true);
    }

    public void Fail(string stage, string reason, DateTimeOffset at)
    {
        FailureStage = stage;
        FailureReason = Guard.Length(reason, nameof(reason), InvoiceConsts.ReasonMaxLength);
        Move(SalesInvoiceStatus.Failed, at, live: false, null, $"{stage}: {FailureReason}");
    }

    /// <summary>A person approved the draft: from then on it is signed and not changed.</summary>
    public void Approve(DateTimeOffset at, Guid? by)
    {
        Require(SalesInvoiceStatus.DraftCreated, "approved");
        (ApprovedAt, ApprovedBy) = (at, by);
        Move(SalesInvoiceStatus.Approved, at, live: true, by);
    }

    /// <summary>A person rejected the draft: it is not live any more, and the same number may be sent again.</summary>
    public void Reject(string reason, DateTimeOffset at, Guid? by)
    {
        Require(SalesInvoiceStatus.DraftCreated, "rejected");
        RejectionReason = Guard.Length(Guard.NotNullOrWhiteSpace(reason, nameof(reason)), nameof(reason), InvoiceConsts.ReasonMaxLength);
        (RejectedAt, RejectedBy) = (at, by);
        Move(SalesInvoiceStatus.Rejected, at, live: false, by, RejectionReason);
    }

    /// <summary>The signed XML was written: the draft is gone and the record is done.</summary>
    public void Sign(string signedPath, DateTimeOffset at)
    {
        Require(SalesInvoiceStatus.Approved, "signed");
        SignedPath = Guard.NotNullOrWhiteSpace(signedPath, nameof(SignedPath), InvoiceConsts.PathMaxLength);
        DraftPath = null;
        SignedAt = at;
        Move(SalesInvoiceStatus.Signed, at, live: true);

        // the signed invoice waits for its envelope (the integration may have said something about it already: that stays)
        if (EnvelopeState == InvoiceEnvelopeState.None)
        {
            EnvelopeState = InvoiceEnvelopeState.ToSend;
            EnvelopeStatusAt = at;
            AddHistory(InvoiceHistoryEntry.Of(HistoryKindEnvelope, nameof(InvoiceEnvelopeState.ToSend), at));
        }
    }

    /// <summary>
    /// The GİB invoice integration told something about the envelope of the invoice. The events do not arrive in the order things happened in, so the state only goes forward: a news that is late (the envelope is delivered, or failed, already) is not applied, and the answer says so.
    /// </summary>
    public bool ApplyEnvelope(Guid envelopeId, InvoiceEnvelopeState state, int? statusCode, string? statusText, string? note, DateTimeOffset at)
    {
        if (state <= InvoiceEnvelopeState.None || Rank(state) < Rank(EnvelopeState) || Rank(EnvelopeState) == TerminalRank)
        {
            return false;
        }

        EnvelopeId = envelopeId;
        EnvelopeState = state;
        EnvelopeStatusCode = statusCode is null or 0 ? EnvelopeStatusCode : statusCode;
        EnvelopeStatusText = statusText ?? EnvelopeStatusText;
        EnvelopeNote = note is null ? null : Guard.Length(note, nameof(note), InvoiceConsts.ReasonMaxLength);
        EnvelopeStatusAt = at;
        AddHistory(InvoiceHistoryEntry.Of(HistoryKindEnvelope, state.ToString(), at, null, statusCode is null or 0 ? note : $"{statusCode} {statusText}{(note is null ? string.Empty : " - " + note)}"));
        return true;
    }

    /// <summary>The answer of the receiver to a commercial invoice came (or went): it is kept in the invoice. Only the shape of the answer is kept so far.</summary>
    public void AddResponse(InvoiceResponse response, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(response);

        Responses = [.. Responses, response];
        AddHistory(InvoiceHistoryEntry.Of(HistoryKindStatus, "Response" + response.Kind, at, null, response.Note));
    }

    private static SalesInvoice New(Guid invoiceId, string number, Guid queueId, InvoiceSourceType sourceType, InvoiceChannelType channelType, DateTimeOffset at, SalesInvoiceStatus status, bool isLive)
    {
        string text = Guard.NotNullOrWhiteSpace(number, nameof(Number), InvoiceConsts.NumberMaxLength);
        return new SalesInvoice
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
            History = [InvoiceHistoryEntry.Of(HistoryKindStatus, status.ToString(), at)],
        };
    }

    private void Require(SalesInvoiceStatus status, string what)
    {
        if (Status != status)
        {
            throw new InvoiceStateException(Id, Status, what);
        }
    }

    private void Move(SalesInvoiceStatus status, DateTimeOffset at, bool live, Guid? by = null, string? note = null)
    {
        Status = status;
        IsLive = live;
        StatusChangedAt = at;
        AddHistory(InvoiceHistoryEntry.Of(HistoryKindStatus, status.ToString(), at, by, note));
    }

    private void AddHistory(InvoiceHistoryEntry entry) => History = [.. History, entry];

    // the envelope's states go forward: none, to send, sent, then one of the three that end it
    private static int Rank(InvoiceEnvelopeState state) => state switch
    {
        InvoiceEnvelopeState.None => 0,
        InvoiceEnvelopeState.ToSend => 1,
        InvoiceEnvelopeState.Sent => 2,
        _ => TerminalRank,
    };

}
