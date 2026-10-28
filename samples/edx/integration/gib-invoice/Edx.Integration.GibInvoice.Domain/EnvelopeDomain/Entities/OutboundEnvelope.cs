using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Consts;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Envelope;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;
using HsnSoft.Base.MultiTenancy;

namespace Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;

/// <summary>What an outbound envelope carries.</summary>
public enum OutboundKind
{
    /// <summary>An invoice of a tenant (a <c>SENDERENVELOPE</c>), on its way to its receiver through the center.</summary>
    Invoice = 1,

    /// <summary>The answer of the post box of a tenant about an envelope that came in (a <c>SYSTEMENVELOPE</c>), on its way to the sender of that envelope through the center.</summary>
    SystemResponse = 2,

    /// <summary>The answer of this tenant's post box to a commercial invoice that came in (an application response, <c>POSTBOXENVELOPE</c>), sent to the sender unit of its supplier.</summary>
    InvoiceAnswer = 3,
}

/// <summary>Where an outbound envelope is.</summary>
public enum OutboundState
{
    /// <summary>Made, and waiting for the center to be told (again, when a try failed).</summary>
    ToSend = 1,

    /// <summary>The center took it (<c>sendDocument</c> answered): its status is asked of the center and told by system responses, until it is delivered or fails.</summary>
    Sent = 2,

    /// <summary>The end of a good road: for an invoice, the post box of the receiver took it in (1300); for a system response, the center took it.</summary>
    Delivered = 3,

    /// <summary>The center refused the envelope when it analyzed it (a code between 1110 and 1181): the invoice has to be corrected and sent in another envelope.</summary>
    Refused = 4,

    /// <summary>It did not get there: the center could not be reached (the tries are used up) or refused the call, or it could not deliver it (1215), or the post box refused it (1230). The reason says which.</summary>
    Failed = 5,
}

/// <summary>What happened to an outbound envelope when a status was applied to it.</summary>
public enum OutboundOutcome
{
    /// <summary>It waits: nothing is decided yet.</summary>
    Waiting = 0,

    /// <summary>It was delivered.</summary>
    Delivered = 1,

    /// <summary>It was refused or it failed (see <see cref="OutboundEnvelope.FailureStage"/>).</summary>
    Failed = 2,

    /// <summary>It was not changed (it had ended already).</summary>
    Unchanged = 3,
}

/// <summary>
/// An envelope the integration service hands to the center: an invoice of a tenant, or the answer of its post box about an invoice that came in. The zip is in the blob store; here is its header (who it is from and to), where it is in its road and what the center said of it last. The tenant is
/// the tenant the envelope is made for (the sender of the invoice, or the receiver the answer is from). <see cref="Entity{TKey}"/>'s id is the id of the record; <see cref="EnvelopeId"/> is the uuid the center knows the envelope by.
/// </summary>
public sealed class OutboundEnvelope : Entity<Guid>, IMultiTenant
{
    // for the database driver
    private OutboundEnvelope()
    {
    }

    /// <summary>The uuid of the envelope: the name of its zip, and what the center is asked about it by.</summary>
    public string EnvelopeId { get; private set; } = string.Empty;

    public OutboundKind Kind { get; private set; }

    public OutboundState State { get; private set; }

    public Guid TenantId { get; private set; }

    /// <summary>Where the zip is in the blob store of the service (empty when none was made: the receiver is not an e-invoice user).</summary>
    public string ObjectName { get; private set; } = string.Empty;

    public long ContentLength { get; private set; }

    // the header
    public string SenderAlias { get; private set; } = string.Empty;

    public string SenderRegisterNumber { get; private set; } = string.Empty;

    public string ReceiverAlias { get; private set; } = string.Empty;

    public string ReceiverRegisterNumber { get; private set; } = string.Empty;

    public string ReceiverTitle { get; private set; } = string.Empty;

    // for an invoice: which one (the record of the invoice service, the uuid, the number, the queue item of the firm invoice integration)
    public Guid? InvoiceRecordId { get; private set; }

    public Guid? InvoiceId { get; private set; }

    public string? InvoiceNumber { get; private set; }

    public Guid? QueueId { get; private set; }

    /// <summary>For a system response: the uuid of the envelope that came in and is answered.</summary>
    public string? ReferenceEnvelopeId { get; private set; }

    /// <summary>For an answer: the uuid of the answer document (the invoice it answers is <see cref="InvoiceId"/>).</summary>
    public Guid? AnswerUuid { get; private set; }

    /// <summary>What the center last said of the envelope, as a code (<see cref="EfaturaStatus"/>) and its text.</summary>
    public int StatusCode { get; private set; }

    public string StatusDescription { get; private set; } = string.Empty;

    public string? FailureStage { get; private set; }

    public string? FailureReason { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset StatusChangedAt { get; private set; }

    public int SendCount { get; private set; }

    public DateTimeOffset? LastSendAt { get; private set; }

    public DateTimeOffset NextSendAt { get; private set; }

    public string? LastError { get; private set; }

    /// <summary>What happened to the envelope, in order (the codes of the center it heard, and what this service did).</summary>
    public IReadOnlyList<EnvelopeEvent> History { get; private set; } = [];

    /// <summary>The envelope of an invoice, made and waiting to be sent.</summary>
    public static OutboundEnvelope ForInvoice(
        string envelopeId,
        string objectName,
        long contentLength,
        EnvelopeParty sender,
        EnvelopeParty receiver,
        Guid invoiceRecordId,
        Guid invoiceId,
        string invoiceNumber,
        Guid queueId,
        DateTimeOffset at)
        => Make(envelopeId, OutboundKind.Invoice, objectName, contentLength, sender, receiver, at, OutboundState.ToSend, invoiceRecordId, invoiceId, invoiceNumber, queueId, null);

    /// <summary>An invoice that could not be put in an envelope (the receiver is not a registered user): the record says so and nothing is sent.</summary>
    public static OutboundEnvelope ForUndeliverableInvoice(string envelopeId, EnvelopeParty sender, string receiverRegisterNumber, string receiverTitle, Guid invoiceRecordId, Guid invoiceId, string invoiceNumber, Guid queueId, string stage, string reason, DateTimeOffset at)
    {
        OutboundEnvelope envelope = Make(envelopeId, OutboundKind.Invoice, string.Empty, 0, sender, new EnvelopeParty(string.Empty, receiverRegisterNumber, receiverTitle), at, OutboundState.Failed, invoiceRecordId, invoiceId, invoiceNumber, queueId, null);
        envelope.FailureStage = stage;
        envelope.FailureReason = Guard.Length(reason, nameof(reason), EnvelopeConsts.ReasonMaxLength);
        return envelope;
    }

    /// <summary>The answer of a post box about an envelope that came in, made and waiting to be sent.</summary>
    /// <summary>The envelope of the answer to a commercial invoice that came in: from the post box of the tenant that answered to the sender unit of the supplier. <paramref name="invoiceEnvelopeId"/> is the envelope the invoice came in.</summary>
    public static OutboundEnvelope ForAnswer(string envelopeId, string objectName, long contentLength, EnvelopeParty sender, EnvelopeParty receiver, Guid invoiceUuid, string invoiceNumber, Guid answerUuid, string invoiceEnvelopeId, DateTimeOffset at)
    {
        OutboundEnvelope envelope = Make(envelopeId, OutboundKind.InvoiceAnswer, objectName, contentLength, sender, receiver, at, OutboundState.ToSend, null, invoiceUuid, invoiceNumber, null, invoiceEnvelopeId);
        envelope.AnswerUuid = answerUuid;
        return envelope;
    }

    public static OutboundEnvelope ForSystemResponse(string envelopeId, string objectName, long contentLength, EnvelopeParty sender, EnvelopeParty receiver, string referenceEnvelopeId, DateTimeOffset at)
        => Make(envelopeId, OutboundKind.SystemResponse, objectName, contentLength, sender, receiver, at, OutboundState.ToSend, null, null, null, null, referenceEnvelopeId);

    /// <summary>The center took the envelope. An invoice goes on being followed; a system response is done.</summary>
    public void MarkSent(DateTimeOffset at)
    {
        SendCount++;
        LastSendAt = at;
        LastError = null;
        State = Kind == OutboundKind.SystemResponse ? OutboundState.Delivered : OutboundState.Sent;
        StatusCode = EfaturaStatus.Queued;
        StatusDescription = EfaturaStatus.NameOf(EfaturaStatus.Queued);
        StatusChangedAt = at;
        Log(at, StatusCode, StatusDescription, EnvelopeEvent.SourceSend, $"attempt {SendCount}");
    }

    /// <summary>The center could not be reached: it is tried again at <paramref name="next"/>, or, when the tries are used up, the envelope failed.</summary>
    public void MarkSendFailed(string error, DateTimeOffset at, DateTimeOffset next, bool retry)
    {
        SendCount++;
        LastSendAt = at;
        LastError = Guard.Length(error, nameof(error), EnvelopeConsts.ReasonMaxLength);
        if (retry)
        {
            NextSendAt = next;
            Log(at, 0, "The center could not be reached", EnvelopeEvent.SourceLocal, $"attempt {SendCount}, tried again at {next:u}: {error}");
            return;
        }

        Fail("Send", $"The center could not be reached: {error}", at);
    }

    /// <summary>The center refused the call (a fault of <c>sendDocument</c>): there is no use trying again.</summary>
    public void MarkSendRefused(int code, string message, DateTimeOffset at)
    {
        SendCount++;
        LastSendAt = at;
        LastError = $"{code}:{message}";
        StatusCode = code;
        StatusDescription = message;
        Log(at, code, message, EnvelopeEvent.SourceSend, $"attempt {SendCount}");
        Fail("Send", $"The center refused the envelope ({code} {message}).", at);
    }

    /// <summary>
    /// The center says what became of the envelope (its status code). It waits (1000, 1100, 1200, 1210, 1220), or it is delivered (1300), or it was refused when it was analyzed (1110 to 1181), or it could not be delivered (1215), or the post box refused it (1230, 1235).
    /// Only an envelope that is not at its end is changed.
    /// </summary>
    public OutboundOutcome ApplyCenterStatus(int code, string description, DateTimeOffset at)
    {
        if (State is not (OutboundState.ToSend or OutboundState.Sent))
        {
            return OutboundOutcome.Unchanged;
        }

        bool news = code != StatusCode;
        StatusCode = code;
        StatusDescription = string.IsNullOrWhiteSpace(description) ? EfaturaStatus.NameOf(code) : description;
        if (news)
        {
            Log(at, code, StatusDescription, EnvelopeEvent.SourcePoll);
        }

        if (code == EfaturaStatus.Completed)
        {
            Move(OutboundState.Delivered, at);
            return OutboundOutcome.Delivered;
        }

        if (EfaturaStatus.IsRefusal(code))
        {
            State = OutboundState.Refused;
            FailureStage = "Center";
            FailureReason = Guard.Length($"The center refused the envelope: {code} {StatusDescription}", nameof(description), EnvelopeConsts.ReasonMaxLength);
            StatusChangedAt = at;
            return OutboundOutcome.Failed;
        }

        if (code is EfaturaStatus.DeliveryFailed or EfaturaStatus.SystemResponseFailed or EfaturaStatus.InvoiceCancelled)
        {
            Fail("Delivery", $"The invoice did not reach its receiver: {code} {StatusDescription}", at);
            return OutboundOutcome.Failed;
        }

        StatusChangedAt = at;
        return OutboundOutcome.Waiting;
    }

    /// <summary>The post box of the receiver answered about the envelope (a system envelope the center forwarded): 1200 means it took the invoice in; any other code is a refusal.</summary>
    public OutboundOutcome ApplyReceiverAnswer(int code, string description, DateTimeOffset at)
    {
        if (State is not (OutboundState.ToSend or OutboundState.Sent))
        {
            return OutboundOutcome.Unchanged;
        }

        StatusCode = code == EfaturaStatus.Processed ? EfaturaStatus.Completed : EfaturaStatus.SystemResponseFailed;
        StatusDescription = EfaturaStatus.NameOf(StatusCode);
        Log(at, StatusCode, StatusDescription, EnvelopeEvent.SourcePostBox, $"{code} {description}");
        if (code == EfaturaStatus.Processed)
        {
            Move(OutboundState.Delivered, at);
            return OutboundOutcome.Delivered;
        }

        Fail("Delivery", $"The post box of the receiver refused the invoice: {code} {description}", at);
        return OutboundOutcome.Failed;
    }

    /// <summary>The status was asked of the center and nothing new came: it is asked again later.</summary>
    public void Polled(DateTimeOffset at) => StatusChangedAt = at;

    private void Log(DateTimeOffset at, int code, string text, string source, string? note = null)
        => History = [.. History, EnvelopeEvent.Of(at, code, text, source, note)];

    private void Move(OutboundState state, DateTimeOffset at)
    {
        State = state;
        StatusChangedAt = at;
    }

    private void Fail(string stage, string reason, DateTimeOffset at)
    {
        FailureStage = stage;
        FailureReason = Guard.Length(reason, nameof(reason), EnvelopeConsts.ReasonMaxLength);
        Move(OutboundState.Failed, at);
        Log(at, 0, nameof(OutboundState.Failed), EnvelopeEvent.SourceLocal, $"{stage}: {FailureReason}");
    }

    private static OutboundEnvelope Make(
        string envelopeId,
        OutboundKind kind,
        string objectName,
        long contentLength,
        EnvelopeParty sender,
        EnvelopeParty receiver,
        DateTimeOffset at,
        OutboundState state,
        Guid? invoiceRecordId,
        Guid? invoiceId,
        string? invoiceNumber,
        Guid? queueId,
        string? referenceEnvelopeId)
        => new()
        {
            Id = Guid.CreateVersion7(),
            EnvelopeId = Guard.NotNullOrWhiteSpace(envelopeId, nameof(EnvelopeId), 36),
            Kind = kind,
            State = state,
            ObjectName = objectName,
            ContentLength = contentLength,
            SenderAlias = sender.Alias,
            SenderRegisterNumber = sender.RegisterNumber,
            ReceiverAlias = receiver.Alias,
            ReceiverRegisterNumber = receiver.RegisterNumber,
            ReceiverTitle = receiver.Title,
            InvoiceRecordId = invoiceRecordId,
            InvoiceId = invoiceId,
            InvoiceNumber = invoiceNumber,
            QueueId = queueId,
            ReferenceEnvelopeId = referenceEnvelopeId,
            StatusCode = EfaturaStatus.Queued,
            StatusDescription = EfaturaStatus.NameOf(EfaturaStatus.Queued),
            CreatedAt = at,
            StatusChangedAt = at,
            NextSendAt = at,
            History = [EnvelopeEvent.Of(at, 0, state.ToString(), EnvelopeEvent.SourceLocal, "the envelope was made")],
        };
}
