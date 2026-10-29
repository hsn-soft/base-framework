using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Consts;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Envelope;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;

/// <summary>Where an envelope that came in is.</summary>
public enum InboundState
{
    /// <summary>Taken from the center and queued: the checks have not been made.</summary>
    Queued = 1,

    /// <summary>An invoice that passed the checks: it is kept as a purchase invoice of the tenant, and the post box answered the center with 1200.</summary>
    Accepted = 2,

    /// <summary>It did not pass a check: the post box answered the center with the code (when it knew who to answer).</summary>
    Refused = 3,

    /// <summary>A system response (the center's, or the receiver's post box's) about an envelope of ours: its news was applied to that envelope.</summary>
    Applied = 4,

    /// <summary>A system response about something that is not ours, or that was refused: it is kept and nothing is done (a system response is never answered).</summary>
    Ignored = 5,
}

/// <summary>
/// An envelope the center delivered to the post box of the service (<c>sendDocument</c> of the post box): an invoice for a tenant, or a system response about an envelope of a tenant's. The zip is in the blob store; here is its header, what the service made of it (the code it answers with) and
/// where it is. The tenant is not known when the envelope comes in (it is the owner of the alias it is for): <see cref="TenantId"/> is empty until the checks find it, and an envelope no tenant owns stays so.
/// </summary>
public sealed class InboundEnvelope : Entity<Guid>, IMultiTenant
{
    // for the database driver
    private InboundEnvelope()
    {
    }

    public string EnvelopeId { get; private set; } = string.Empty;

    public InboundState State { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>Where the zip is in the blob store of the service.</summary>
    public string ObjectName { get; private set; } = string.Empty;

    public long ContentLength { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    public DateTimeOffset? ProcessedAt { get; private set; }

    /// <summary>What the post box says of the envelope (1000 queued, 1100 being checked, 1200 taken, or the code of the check it failed): the answer of <c>getApplicationResponse</c> and of the system response.</summary>
    public int StatusCode { get; private set; }

    public string StatusDescription { get; private set; } = string.Empty;

    /// <summary>How many times the processing failed on something that is not the envelope's fault.</summary>
    public int Attempts { get; private set; }

    // the header
    public string? DocumentType { get; private set; }

    public string? SenderAlias { get; private set; }

    public string? SenderRegisterNumber { get; private set; }

    public string? SenderTitle { get; private set; }

    public string? ReceiverAlias { get; private set; }

    public string? ReceiverRegisterNumber { get; private set; }

    public string? ReceiverTitle { get; private set; }

    public DateTime? EnvelopeCreatedAt { get; private set; }

    // an invoice: what it says of itself and where its XML is
    public Guid? InvoiceUuid { get; private set; }

    public string? InvoiceNumber { get; private set; }

    public string? SupplierRegisterNumber { get; private set; }

    public string? SupplierName { get; private set; }

    /// <summary>For a system response: the uuid of the envelope of ours it is about.</summary>
    public string? ReferenceEnvelopeId { get; private set; }

    /// <summary>The system response the post box made for this envelope (an <see cref="OutboundEnvelope"/>).</summary>
    public Guid? AnswerOutboundId { get; private set; }

    /// <summary>What the center says of this envelope (asked with its uuid): 1220 while it waits for this post box's answer, 1300 once it has it, a failure code otherwise. Null until it was asked.</summary>
    public int? CenterStatusCode { get; private set; }

    public string? CenterStatusDescription { get; private set; }

    public DateTimeOffset? CenterStatusAt { get; private set; }

    /// <summary>When the center was last asked (the next ask waits <c>Center:StatusPollAfterSeconds</c> from it).</summary>
    public DateTimeOffset? CenterCheckedAt { get; private set; }

    /// <summary>What happened to the envelope, in order: what this post box did and the codes of the center it heard.</summary>
    public IReadOnlyList<EnvelopeEvent> History { get; private set; } = [];

    /// <summary>An envelope the center handed over: queued, status 1000.</summary>
    public static InboundEnvelope Receive(string envelopeId, string objectName, long contentLength, DateTimeOffset at)
        => new()
        {
            Id = Guid.CreateVersion7(),
            EnvelopeId = Guard.NotNullOrWhiteSpace(envelopeId, nameof(EnvelopeId), 36),
            State = InboundState.Queued,
            ObjectName = Guard.NotNullOrWhiteSpace(objectName, nameof(ObjectName), EnvelopeConsts.PathMaxLength),
            ContentLength = contentLength,
            ReceivedAt = at,
            StatusCode = EfaturaStatus.Queued,
            StatusDescription = EfaturaStatus.NameOf(EfaturaStatus.Queued),
            History = [EnvelopeEvent.Of(at, EfaturaStatus.Queued, EfaturaStatus.NameOf(EfaturaStatus.Queued), EnvelopeEvent.SourceCenter, "the center delivered it to this post box")],
        };

    public void StartProcessing()
    {
        StatusCode = EfaturaStatus.Processing;
        StatusDescription = EfaturaStatus.NameOf(EfaturaStatus.Processing);
    }

    /// <summary>The processing failed on something that is not the envelope's fault: it is tried again, and when the tries are used up it is answered with 1195.</summary>
    public bool ProcessingFailed() => ++Attempts >= EnvelopeConsts.MaxProcessAttempts;

    public void ReadHeader(EnvelopeHeader header)
    {
        ArgumentNullException.ThrowIfNull(header);

        DocumentType = header.DocumentType;
        SenderAlias = header.Sender.Alias;
        SenderRegisterNumber = header.Sender.RegisterNumber;
        SenderTitle = header.Sender.Title;
        ReceiverAlias = header.Receiver.Alias;
        ReceiverRegisterNumber = header.Receiver.RegisterNumber;
        ReceiverTitle = header.Receiver.Title;
        EnvelopeCreatedAt = header.CreatedAt;
    }

    /// <summary>The owner of the alias the envelope is for (the tenant).</summary>
    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    /// <summary>An invoice that passed the checks.</summary>
    public void AcceptInvoice(Guid invoiceUuid, string number, string supplierRegisterNumber, string supplierName, Guid? answerOutboundId, DateTimeOffset at)
    {
        InvoiceUuid = invoiceUuid;
        InvoiceNumber = number;
        SupplierRegisterNumber = supplierRegisterNumber;
        SupplierName = supplierName;
        AnswerOutboundId = answerOutboundId;
        End(InboundState.Accepted, EfaturaStatus.Processed, EfaturaStatus.NameOf(EfaturaStatus.Processed), at);
    }

    /// <summary>An answer to a commercial invoice of the tenant that passed the checks: the invoice service is told, and the post box answers the center (1200) when it is asked.</summary>
    public void AcceptAnswer(string invoiceNumber, DateTimeOffset at)
    {
        InvoiceNumber = invoiceNumber;
        End(InboundState.Accepted, EfaturaStatus.Processed, EfaturaStatus.NameOf(EfaturaStatus.Processed), at);
    }

    public void Refuse(int code, string description, Guid? answerOutboundId, DateTimeOffset at)
    {
        AnswerOutboundId = answerOutboundId;
        End(InboundState.Refused, code, description, at);
    }

    /// <summary>A system response of the center or of a post box about an envelope of ours was applied to it.</summary>
    public void Applied(string referenceEnvelopeId, DateTimeOffset at)
    {
        ReferenceEnvelopeId = referenceEnvelopeId;
        End(InboundState.Applied, EfaturaStatus.Processed, EfaturaStatus.NameOf(EfaturaStatus.Processed), at);
    }

    /// <summary>A system response that is about nothing of ours (or that failed a check): it is kept, and nothing is done.</summary>
    public void Ignore(string? referenceEnvelopeId, int code, string description, DateTimeOffset at)
    {
        ReferenceEnvelopeId = referenceEnvelopeId;
        End(InboundState.Ignored, code, description, at);
    }

    private void End(InboundState state, int code, string description, DateTimeOffset at)
    {
        State = state;
        StatusCode = code;
        StatusDescription = Guard.Length(description, nameof(description), EnvelopeConsts.ReasonMaxLength)!;
        ProcessedAt = at;
        History = [.. History, EnvelopeEvent.Of(at, code, StatusDescription, EnvelopeEvent.SourceLocal, state.ToString())];
    }

    /// <summary>The center was asked about the envelope. A newer status is kept (and is the news); the same one only moves the time of the last ask. Returns <see langword="true"/> for news.</summary>
    public bool ApplyCenterStatus(int code, string description, DateTimeOffset at)
    {
        CenterCheckedAt = at;
        if (CenterStatusCode == code)
        {
            return false;
        }

        (CenterStatusCode, CenterStatusDescription, CenterStatusAt) = (code, string.IsNullOrWhiteSpace(description) ? EfaturaStatus.NameOf(code) : description, at);
        History = [.. History, EnvelopeEvent.Of(at, code, CenterStatusDescription, EnvelopeEvent.SourcePoll)];
        return true;
    }

    /// <summary>The center was asked and had nothing to say: it is asked again after the same wait.</summary>
    public void CenterChecked(DateTimeOffset at) => CenterCheckedAt = at;

    /// <summary>Whether the center still has something to say about this envelope: it is an invoice envelope this post box took, and the center has not ended it (completed, or failed).</summary>
    public bool AwaitsCenterStatus => State == InboundState.Accepted && CenterStatusCode is null or EfaturaStatus.Processed or EfaturaStatus.NoSystemResponse or EfaturaStatus.NotDeliveredRetrying or EfaturaStatus.Queued or EfaturaStatus.Processing;
}
