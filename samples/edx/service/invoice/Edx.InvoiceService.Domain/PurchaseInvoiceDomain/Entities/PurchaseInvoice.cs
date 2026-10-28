using Edx.InvoiceService.Domain.InvoiceDomain.Entities;
using Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Consts;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;

namespace Edx.InvoiceService.Domain.PurchaseInvoiceDomain.Entities;

/// <summary>
/// An invoice another taxpayer issued to a tenant (the tenant is its customer): it came in at the post box of the tenant, in an envelope of the center, and the integration service handed it over (<c>PurchaseInvoiceReceivedEto</c>). What the document says of itself is kept (who it is from, its
/// number, its date, what it amounts to) and where its XML is (in the container of the invoice service); the document is not changed. A purchase invoice is not approved or signed by the tenant: it is received. The audit fields come from <see cref="FullAuditedEntity{TKey}"/>; the tenant is kept here and given by the persistence.
/// </summary>
public sealed class PurchaseInvoice : FullAuditedEntity<Guid>, IMultiTenant
{
    // for the database driver
    private PurchaseInvoice()
    {
    }

    /// <summary>The uuid of the invoice (its ETTN), which is what makes a purchase invoice be kept once however many times it is told of.</summary>
    public Guid InvoiceUuid { get; private set; }

    /// <summary>The uuid of the envelope it came in.</summary>
    public Guid EnvelopeId { get; private set; }

    public string Number { get; private set; } = string.Empty;

    public string NormalizedNumber { get; private set; } = string.Empty;

    public DateTime IssueDate { get; private set; }

    public string Currency { get; private set; } = string.Empty;

    /// <summary>The scenario of the invoice: <c>TEMELFATURA</c>, or <c>TICARIFATURA</c> (this tenant may answer it: accept or refuse).</summary>
    public string Profile { get; private set; } = string.Empty;

    public string SupplierRegisterNumber { get; private set; } = string.Empty;

    public string SupplierName { get; private set; } = string.Empty;

    public string CustomerRegisterNumber { get; private set; } = string.Empty;

    public string CustomerName { get; private set; } = string.Empty;

    public int LineCount { get; private set; }

    public decimal TaxExclusiveAmount { get; private set; }

    public decimal TaxInclusiveAmount { get; private set; }

    /// <summary>What the invoice amounts to: the amount to be paid.</summary>
    public decimal PayableAmount { get; private set; }

    /// <summary>Where the XML of the invoice is, in the container of the invoice service.</summary>
    public string XmlPath { get; private set; } = string.Empty;

    public DateTimeOffset ReceivedAt { get; private set; }

    /// <summary>What the center says of the envelope the invoice came in (1200 when this post box took it, then 1220, then 1300 once the center has our answer): a summary; the envelope is the GİB invoice integration's.</summary>
    public int EnvelopeStatusCode { get; private set; }

    public string EnvelopeStatusText { get; private set; } = string.Empty;

    public DateTimeOffset EnvelopeStatusAt { get; private set; }

    /// <summary>When this tenant asked to answer the invoice (a commercial one): it is answered once; the answer is in <see cref="Responses"/> when it is signed.</summary>
    public DateTimeOffset? AnswerRequestedAt { get; private set; }

    /// <summary>What happened to the invoice, in order: that it was received, and each status of its envelope.</summary>
    public IReadOnlyList<InvoiceHistoryEntry> History { get; private set; } = [];

    /// <summary>What this tenant answered to the invoice (a commercial invoice only); the shape is here, the flow is not.</summary>
    public IReadOnlyList<InvoiceResponse> Responses { get; private set; } = [];

    public Guid TenantId { get; private set; }

    public static PurchaseInvoice Receive(
        Guid invoiceUuid,
        Guid envelopeId,
        string number,
        DateTime issueDate,
        string currency,
        string supplierRegisterNumber,
        string supplierName,
        string customerRegisterNumber,
        string customerName,
        int lineCount,
        decimal taxExclusiveAmount,
        decimal taxInclusiveAmount,
        decimal payableAmount,
        string xmlPath,
        DateTimeOffset receivedAt,
        int envelopeStatusCode,
        string envelopeStatusText,
        string profile)
        => new()
        {
            Id = Guid.CreateVersion7(),
            InvoiceUuid = Guard.NotDefault<Guid>(invoiceUuid, nameof(InvoiceUuid)),
            EnvelopeId = envelopeId,
            Number = Guard.NotNullOrWhiteSpace(number, nameof(Number), PurchaseInvoiceConsts.NumberMaxLength),
            NormalizedNumber = StringHelper.Normalize(number),
            IssueDate = issueDate.Date,
            Currency = Guard.NotNullOrWhiteSpace(currency, nameof(Currency), 3).ToUpperInvariant(),
            Profile = Guard.NotNullOrWhiteSpace(profile, nameof(Profile), 16),
            SupplierRegisterNumber = Guard.NotNullOrWhiteSpace(supplierRegisterNumber, nameof(SupplierRegisterNumber), PurchaseInvoiceConsts.TaxIdMaxLength),
            SupplierName = Guard.NotNullOrWhiteSpace(supplierName, nameof(SupplierName), PurchaseInvoiceConsts.NameMaxLength),
            CustomerRegisterNumber = Guard.NotNullOrWhiteSpace(customerRegisterNumber, nameof(CustomerRegisterNumber), PurchaseInvoiceConsts.TaxIdMaxLength),
            CustomerName = Guard.NotNullOrWhiteSpace(customerName, nameof(CustomerName), PurchaseInvoiceConsts.NameMaxLength),
            LineCount = lineCount,
            TaxExclusiveAmount = taxExclusiveAmount,
            TaxInclusiveAmount = taxInclusiveAmount,
            PayableAmount = payableAmount,
            XmlPath = Guard.NotNullOrWhiteSpace(xmlPath, nameof(XmlPath), PurchaseInvoiceConsts.PathMaxLength),
            ReceivedAt = receivedAt,
            EnvelopeStatusCode = envelopeStatusCode,
            EnvelopeStatusText = envelopeStatusText,
            EnvelopeStatusAt = receivedAt,
            History = [InvoiceHistoryEntry.Of(SalesInvoice.HistoryKindStatus, "Received", receivedAt), InvoiceHistoryEntry.Of(SalesInvoice.HistoryKindEnvelope, "Placed", receivedAt, null, $"{envelopeStatusCode} {envelopeStatusText}")],
        };

    /// <summary>The GİB invoice integration read a newer status of the envelope at the center. An older or the same one is not applied.</summary>
    public bool ApplyEnvelopeStatus(int code, string text, DateTimeOffset at)
    {
        if (code == EnvelopeStatusCode || at < EnvelopeStatusAt)
        {
            return false;
        }

        (EnvelopeStatusCode, EnvelopeStatusText, EnvelopeStatusAt) = (code, text, at);
        History = [.. History, InvoiceHistoryEntry.Of(SalesInvoice.HistoryKindEnvelope, code.ToString(System.Globalization.CultureInfo.InvariantCulture), at, null, $"{code} {text}")];
        return true;
    }

    /// <summary>The tenant asked to answer the invoice: a commercial invoice that has not been answered (or is not being answered) can be.</summary>
    public bool CanBeAnswered => Profile == "TICARIFATURA" && AnswerRequestedAt is null && Responses.Count == 0;

    public void RequestAnswer(string kind, DateTimeOffset at, Guid? by, string? note)
    {
        AnswerRequestedAt = at;
        History = [.. History, InvoiceHistoryEntry.Of(SalesInvoice.HistoryKindStatus, "AnswerRequested" + kind, at, by, note)];
    }

    /// <summary>The envelope of the answer said something (it was sent, delivered, refused, failed): the answer keeps it, and the history says it.</summary>
    public bool ApplyAnswerEnvelope(Guid answerUuid, Guid envelopeId, string state, int statusCode, string statusText, DateTimeOffset at)
    {
        InvoiceResponse? answer = Responses.FirstOrDefault(response => response.DocumentUuid == answerUuid);
        if (answer is null)
        {
            return false;
        }

        answer.ApplyEnvelope(envelopeId, state, statusCode, statusText, at);
        History = [.. History, InvoiceHistoryEntry.Of(SalesInvoice.HistoryKindEnvelope, "Answer" + state, at, null, $"{statusCode} {statusText}")];
        return true;
    }

    /// <summary>The answer this tenant gave to the invoice (accepted or refused) is kept.</summary>
    public void AddResponse(InvoiceResponse response, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(response);

        Responses = [.. Responses, response];
        History = [.. History, InvoiceHistoryEntry.Of(SalesInvoice.HistoryKindStatus, "Response" + response.Kind, at, null, response.Note)];
    }
}
