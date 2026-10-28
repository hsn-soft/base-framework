using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;
using Edx.Shared.Contracts.Events.Integration;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Logging;

namespace Edx.Integration.GibInvoice.Application.Services;

/// <summary>
/// What becomes of an outbound envelope when news of it comes, from the center or from the post box of the receiver: the envelope is changed and kept, and the invoice service is told when the invoice has reached its receiver or has not. The events are published as the tenant of the envelope
/// (the workers run as the system tenant, which no event may be sent as). A system response is not told of to anybody: nothing waits for it.
/// </summary>
public sealed class OutboundStatusService(IOutboundEnvelopeRepository envelopes, IEventBus eventBus, ICurrentTenant tenant, TimeProvider time, ILogger<OutboundStatusService> logger)
{
    /// <summary>The center says what became of the envelope (its status code).</summary>
    public Task ApplyCenterStatusAsync(OutboundEnvelope envelope, int code, string description, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return ApplyAsync(envelope, () => envelope.ApplyCenterStatus(code, description, time.GetUtcNow()), cancellationToken);
    }

    /// <summary>The post box of the receiver answered about the envelope (1200 took it in; anything else refused it).</summary>
    public Task ApplyReceiverAnswerAsync(OutboundEnvelope envelope, int code, string description, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        return ApplyAsync(envelope, () => envelope.ApplyReceiverAnswer(code, description, time.GetUtcNow()), cancellationToken);
    }

    /// <summary>The envelope is changed (the caller says how), kept, and, when it reached its end, told to the invoice service.</summary>
    public async Task ApplyAsync(OutboundEnvelope envelope, Func<OutboundOutcome> change, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);
        ArgumentNullException.ThrowIfNull(change);

        OutboundOutcome outcome = change();
        if (outcome == OutboundOutcome.Unchanged)
        {
            return;
        }

        await envelopes.UpdateAsync(envelope, cancellationToken).ConfigureAwait(false);
        await PublishAsync(envelope, outcome, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Envelope {EnvelopeId} is {EnvelopeState} ({StatusCode} {StatusDescription})", envelope.EnvelopeId, envelope.State, envelope.StatusCode, envelope.StatusDescription);
    }

    /// <summary>Tells the invoice service how the road of an invoice ended (delivered, or failed and where); a system response is not told of.</summary>
    public async Task PublishAsync(OutboundEnvelope envelope, OutboundOutcome outcome, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.Kind == OutboundKind.InvoiceAnswer)
        {
            await PublishAnswerAsync(envelope, cancellationToken).ConfigureAwait(false);
            return;
        }

        if (envelope.Kind != OutboundKind.Invoice || envelope.InvoiceRecordId is not { } recordId || envelope.InvoiceId is not { } invoiceId)
        {
            return;
        }

        Guid envelopeId = Guid.Parse(envelope.EnvelopeId);
        using IDisposable scope = tenant.Change(envelope.TenantId);
        switch (outcome)
        {
            case OutboundOutcome.Delivered:
                await eventBus.PublishAsync(new InvoiceDeliveredEto(recordId, invoiceId, envelopeId, envelope.StatusCode), cancellationToken: cancellationToken).ConfigureAwait(false);
                break;
            case OutboundOutcome.Failed:
                await eventBus.PublishAsync(
                    new InvoiceDeliveryFailedEto(recordId, invoiceId, envelopeId, envelope.FailureStage ?? "Delivery", envelope.StatusCode, envelope.FailureReason ?? "The invoice did not reach its receiver."),
                    cancellationToken: cancellationToken).ConfigureAwait(false);
                break;
        }
    }

    /// <summary>Tells the invoice service where the envelope of an answer is (it keeps it on the answer): sent, delivered, refused or failed.</summary>
    public async Task PublishAnswerAsync(OutboundEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        if (envelope.InvoiceId is not { } invoiceUuid || envelope.AnswerUuid is not { } answerUuid)
        {
            return;
        }

        using IDisposable scope = tenant.Change(envelope.TenantId);
        await eventBus.PublishAsync(
            new PurchaseAnswerEnvelopeEto(invoiceUuid, answerUuid, Guid.Parse(envelope.EnvelopeId), envelope.State.ToString(), envelope.StatusCode, envelope.StatusDescription, envelope.FailureReason, time.GetUtcNow()),
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}
