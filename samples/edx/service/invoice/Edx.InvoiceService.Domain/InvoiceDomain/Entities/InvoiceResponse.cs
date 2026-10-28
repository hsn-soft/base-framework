namespace Edx.InvoiceService.Domain.InvoiceDomain.Entities;

/// <summary>What the receiver of a commercial invoice answered.</summary>
public enum InvoiceResponseKind
{
    Accepted = 1,

    Rejected = 2,
}

/// <summary>
/// The answer to a commercial invoice (the application response document that travels in an envelope of its own from the receiver's post box to the sender's unit), kept inside the invoice it is about. A basic invoice has none. The shape is here; making and sending the answers is not done yet.
/// </summary>
public sealed class InvoiceResponse
{
    // for the database driver
    private InvoiceResponse()
    {
    }

    public InvoiceResponseKind Kind { get; private set; }

    /// <summary>The number of the response document.</summary>
    public string DocumentNumber { get; private set; } = string.Empty;

    public Guid DocumentUuid { get; private set; }

    public DateTimeOffset At { get; private set; }

    public string? Note { get; private set; }

    /// <summary>The envelope the answer came or went in.</summary>
    public Guid? EnvelopeId { get; private set; }

    /// <summary>Where that envelope is (an answer that went out): <c>Sent</c>, <c>Delivered</c>, <c>Refused</c> or <c>Failed</c>, with the last code of the center.</summary>
    public string? EnvelopeState { get; private set; }

    public int? EnvelopeStatusCode { get; private set; }

    public string? EnvelopeStatusText { get; private set; }

    public DateTimeOffset? EnvelopeStatusAt { get; private set; }

    /// <summary>The envelope of the answer said something new: only the state goes forward is the caller's rule, the answer keeps what it was told last.</summary>
    public void ApplyEnvelope(Guid envelopeId, string state, int statusCode, string statusText, DateTimeOffset at)
        => (EnvelopeId, EnvelopeState, EnvelopeStatusCode, EnvelopeStatusText, EnvelopeStatusAt) = (envelopeId, state, statusCode, statusText, at);

    public static InvoiceResponse Of(InvoiceResponseKind kind, string documentNumber, Guid documentUuid, DateTimeOffset at, string? note = null, Guid? envelopeId = null)
        => new() { Kind = kind, DocumentNumber = documentNumber, DocumentUuid = documentUuid, At = at, Note = note, EnvelopeId = envelopeId };
}
