using Edx.Mock.Center.Domain.Participants;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Envelope;
using HsnSoft.Base;
using HsnSoft.Base.Domain.Entities;

namespace Edx.Mock.Center.Domain.Envelopes;

/// <summary>Where an envelope in the queue came from.</summary>
public enum EnvelopeOrigin
{
    /// <summary>Somebody handed it to the center (<c>sendDocument</c>).</summary>
    Received = 1,

    /// <summary>The center made it: the system response it sends a sender.</summary>
    CenterResponse = 2,
}

/// <summary>Where an envelope is in the work of the center; each stage has a worker that moves it on.</summary>
public enum EnvelopeStage
{
    /// <summary>Queued: the analysis has not seen it.</summary>
    ToAnalyze = 1,

    /// <summary>Analyzed (taken, or refused with a code): the system response and the delivery are to be prepared.</summary>
    Analyzed = 2,

    /// <summary>Waiting to be delivered to the address it was sent to (again, when a try failed).</summary>
    ToDeliver = 3,

    /// <summary>Delivered: the other side took it.</summary>
    Delivered = 4,

    /// <summary>Nothing more to do.</summary>
    Done = 5,

    /// <summary>Could not be delivered: the other side refused it, or the tries are used up.</summary>
    Failed = 6,
}

/// <summary>
/// An envelope the center has: one it was handed (and its fate: analyzed, delivered to the receiver's post box, answered by the post box) or one it made (a system response to a sender). The zip itself is in the blob store; here is what the center knows of it: its header, its
/// status (the code the sender reads with <c>getApplicationResponse</c>) and where it is in the work.
/// </summary>
public sealed class EnvelopeQueueItem : Entity<Guid>
{
    // for the database driver
    private EnvelopeQueueItem()
    {
    }

    /// <summary>The uuid of the envelope (unique in the center).</summary>
    public string EnvelopeId { get; private set; } = string.Empty;

    public EnvelopeOrigin Origin { get; private set; }

    public EnvelopeStage Stage { get; private set; }

    /// <summary>Where the zip is in the blob store.</summary>
    public string ObjectName { get; private set; } = string.Empty;

    public long ContentLength { get; private set; }

    public DateTimeOffset ReceivedAt { get; private set; }

    /// <summary>The code of the status (<see cref="EfaturaStatus"/>) and its text: what the sender is told.</summary>
    public int StatusCode { get; private set; }

    public string StatusDescription { get; private set; } = string.Empty;

    // what the header says (known after the analysis; for an envelope the center made, from the start)
    public string? DocumentType { get; private set; }

    public string? SenderAlias { get; private set; }

    public string? SenderRegisterNumber { get; private set; }

    public string? SenderTitle { get; private set; }

    public string? ReceiverAlias { get; private set; }

    public string? ReceiverRegisterNumber { get; private set; }

    public string? ReceiverTitle { get; private set; }

    public string? ElementType { get; private set; }

    /// <summary>When the envelope was made, as its header says.</summary>
    public DateTime? EnvelopeCreatedAt { get; private set; }

    public Guid? SenderParticipantId { get; private set; }

    public Guid? ReceiverParticipantId { get; private set; }

    /// <summary>For a system envelope: the envelope it answers about.</summary>
    public string? ReferenceEnvelopeId { get; private set; }

    /// <summary>What identifies the invoices in the envelope, so the center can tell that an invoice was sent before: the sender and the number of the invoice, and its uuid.</summary>
    public IReadOnlyList<string> DocumentKeys { get; private set; } = [];

    /// <summary>How many documents (invoices) the envelope holds: at most 1000, all for the post box of the receiver.</summary>
    public int DocumentCount { get; private set; }

    // the deliveries
    public int SendCount { get; private set; }

    public DateTimeOffset? LastSendAt { get; private set; }

    public DateTimeOffset NextSendAt { get; private set; }

    public string? LastError { get; private set; }

    /// <summary>An envelope somebody handed to the center: queued, status 1000.</summary>
    public static EnvelopeQueueItem Receive(Guid id, string envelopeId, string objectName, long contentLength, DateTimeOffset at)
        => new()
        {
            Id = id,
            EnvelopeId = Guard.NotNullOrWhiteSpace(envelopeId, nameof(EnvelopeId), 36),
            Origin = EnvelopeOrigin.Received,
            Stage = EnvelopeStage.ToAnalyze,
            ObjectName = Guard.NotNullOrWhiteSpace(objectName, nameof(ObjectName), 500),
            ContentLength = contentLength,
            ReceivedAt = at,
            StatusCode = EfaturaStatus.Queued,
            StatusDescription = EfaturaStatus.NameOf(EfaturaStatus.Queued),
            NextSendAt = at,
        };

    /// <summary>A system response the center made for a sender: it waits to be delivered to the address the sender sent from.</summary>
    public static EnvelopeQueueItem Respond(Guid id, string envelopeId, string objectName, long contentLength, EnvelopeHeader header, Participant? receiver, string referenceEnvelopeId, DateTimeOffset at)
    {
        ArgumentNullException.ThrowIfNull(header);

        return new EnvelopeQueueItem
        {
            Id = id,
            EnvelopeId = Guard.NotNullOrWhiteSpace(envelopeId, nameof(EnvelopeId), 36),
            Origin = EnvelopeOrigin.CenterResponse,
            Stage = EnvelopeStage.ToDeliver,
            ObjectName = Guard.NotNullOrWhiteSpace(objectName, nameof(ObjectName), 500),
            ContentLength = contentLength,
            ReceivedAt = at,
            StatusCode = EfaturaStatus.Queued,
            StatusDescription = EfaturaStatus.NameOf(EfaturaStatus.Queued),
            DocumentType = header.DocumentType,
            SenderAlias = header.Sender.Alias,
            SenderRegisterNumber = header.Sender.RegisterNumber,
            SenderTitle = header.Sender.Title,
            ReceiverAlias = header.Receiver.Alias,
            ReceiverRegisterNumber = header.Receiver.RegisterNumber,
            ReceiverTitle = header.Receiver.Title,
            ElementType = header.ElementType,
            EnvelopeCreatedAt = header.CreatedAt,
            ReceiverParticipantId = receiver?.Id,
            ReferenceEnvelopeId = referenceEnvelopeId,
            NextSendAt = at,
        };
    }

    public void StartAnalysis()
    {
        StatusCode = EfaturaStatus.Processing;
        StatusDescription = EfaturaStatus.NameOf(EfaturaStatus.Processing);
    }

    /// <summary>The analysis took the envelope: what its header says, and who the sender and the receiver are.</summary>
    public void Accept(EnvelopeHeader header, Participant sender, Participant receiver, IEnumerable<string> documentKeys, int documentCount)
    {
        ArgumentNullException.ThrowIfNull(header);
        ArgumentNullException.ThrowIfNull(sender);
        ArgumentNullException.ThrowIfNull(receiver);
        ArgumentNullException.ThrowIfNull(documentKeys);

        ReadHeader(header);
        DocumentKeys = [.. documentKeys];
        DocumentCount = documentCount;
        SenderParticipantId = sender.Id;
        ReceiverParticipantId = receiver.Id;
        Stage = EnvelopeStage.Analyzed;
        StatusCode = EfaturaStatus.Processed;
        StatusDescription = EfaturaStatus.NameOf(EfaturaStatus.Processed);
    }

    /// <summary>The analysis refused the envelope, with the code that says why; the header is kept when it could be read.</summary>
    public void Refuse(int code, string? description, EnvelopeHeader? header, Participant? sender, Participant? receiver)
    {
        if (header is not null)
        {
            ReadHeader(header);
        }

        SenderParticipantId = sender?.Id;
        ReceiverParticipantId = receiver?.Id;
        Stage = EnvelopeStage.Analyzed;
        StatusCode = code;
        StatusDescription = description ?? EfaturaStatus.NameOf(code);
    }

    /// <summary>The system response and the delivery are prepared: an envelope that is taken goes on to its receiver, one that is refused (or a system envelope) is done.</summary>
    public void Prepared(bool deliver, DateTimeOffset at)
    {
        Stage = deliver ? EnvelopeStage.ToDeliver : EnvelopeStage.Done;
        NextSendAt = at;
    }

    /// <summary>The other side took the envelope. A document waits for the system response of the receiver's post box (1220); a system envelope is the end of its road (1300).</summary>
    public void Delivered(DateTimeOffset at)
    {
        SendCount++;
        LastSendAt = at;
        LastError = null;
        Stage = EnvelopeStage.Delivered;
        (StatusCode, StatusDescription) = Origin == EnvelopeOrigin.CenterResponse || DocumentType == EnvelopeTypes.System
            ? (EfaturaStatus.Completed, EfaturaStatus.NameOf(EfaturaStatus.Completed))
            : (EfaturaStatus.NoSystemResponse, EfaturaStatus.NameOf(EfaturaStatus.NoSystemResponse));
    }

    /// <summary>The other side could not be reached: tried again at <paramref name="next"/>, or, when the tries are used up, given up (1215).</summary>
    public void DeliveryFailed(string error, DateTimeOffset at, DateTimeOffset next, bool retry)
    {
        SendCount++;
        LastSendAt = at;
        LastError = error;
        if (retry)
        {
            NextSendAt = next;
            StatusCode = EfaturaStatus.NotDeliveredRetrying;
            StatusDescription = EfaturaStatus.NameOf(EfaturaStatus.NotDeliveredRetrying);
            return;
        }

        Stage = EnvelopeStage.Failed;
        StatusCode = EfaturaStatus.DeliveryFailed;
        StatusDescription = EfaturaStatus.NameOf(EfaturaStatus.DeliveryFailed);
    }

    /// <summary>The post box answered about this envelope: 1200 means it took it in (the road ends, 1300); anything else is a refusal (1230) with what it said. Only an envelope that was delivered and waits (1220) is changed.</summary>
    public void Answered(int resultCode, string description)
    {
        if (StatusCode != EfaturaStatus.NoSystemResponse)
        {
            return;
        }

        if (resultCode == EfaturaStatus.Processed)
        {
            StatusCode = EfaturaStatus.Completed;
            StatusDescription = EfaturaStatus.NameOf(EfaturaStatus.Completed);
        }
        else
        {
            StatusCode = EfaturaStatus.SystemResponseFailed;
            StatusDescription = $"{EfaturaStatus.NameOf(EfaturaStatus.SystemResponseFailed)} => [ {resultCode}:{description} ]";
        }
    }

    /// <summary>The center asked the post box what became of the envelope and got no answer: it asks again later.</summary>
    public void Polled(DateTimeOffset at) => LastSendAt = at;

    public void ReferTo(string referenceEnvelopeId) => ReferenceEnvelopeId = referenceEnvelopeId;


    private void ReadHeader(EnvelopeHeader header)
    {
        DocumentType = header.DocumentType;
        SenderAlias = header.Sender.Alias;
        SenderRegisterNumber = header.Sender.RegisterNumber;
        SenderTitle = header.Sender.Title;
        ReceiverAlias = header.Receiver.Alias;
        ReceiverRegisterNumber = header.Receiver.RegisterNumber;
        ReceiverTitle = header.Receiver.Title;
        ElementType = header.ElementType;
        EnvelopeCreatedAt = header.CreatedAt;
    }
}
