using Edx.Integration.GibInvoice.Application.Options;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;
using Edx.Shared.Contracts.Events.Integration;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Client;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Edx.Integration.GibInvoice.Application.Services;

/// <summary>
/// Hands an outbound envelope to the center (<c>sendDocument</c>: the zip named by the uuid of the envelope, with its MD5). The center takes it (and answers fast: it queues it), or it refuses the call (a fault: the name, the hash, the uuid is there already, which means an earlier try got through
/// and its answer was lost), or it cannot be reached (it is tried again after a wait, up to the limit of the settings, and then the envelope fails). The invoice service is told when an invoice is sent, and when it failed to be.
/// </summary>
public sealed class OutboundSender(
    IOutboundEnvelopeRepository envelopes,
    EnvelopeStorage storage,
    OutboundStatusService status,
    IEventBus eventBus,
    ICurrentTenant tenant,
    TimeProvider time,
    IOptions<GibCenterOptions> options,
    ILogger<OutboundSender> logger)
{
    public async Task SendAsync(OutboundEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        DateTimeOffset now = time.GetUtcNow();
        byte[]? zip = await storage.ReadAsync(envelope.ObjectName, cancellationToken).ConfigureAwait(false);
        if (zip is null)
        {
            envelope.MarkSendFailed("The envelope is not in the store.", now, now, retry: false);
        }
        else
        {
            using EFaturaClient client = new(new Uri(options.Value.Endpoint), options.Value.MaxMessageSize);
            EFaturaResult result = await client.SendAsync(envelope.EnvelopeId, zip).ConfigureAwait(false);
            if (result.Accepted || result.Code == EfaturaStatus.EnvelopeAlreadyThere)
            {
                // a uuid the center has already means an earlier try got through and its answer was lost: the envelope is there
                envelope.MarkSent(now);
            }
            else if (result.CanRetry)
            {
                envelope.MarkSendFailed(result.Message ?? "unknown", now, now.AddSeconds(options.Value.SendRetryWaitSeconds), retry: envelope.SendCount < options.Value.SendRetryLimit);
            }
            else
            {
                envelope.MarkSendRefused(result.Code, result.Message ?? EfaturaStatus.NameOf(result.Code), now);
            }
        }

        await envelopes.UpdateAsync(envelope, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Envelope {EnvelopeId} ({EnvelopeKind}) is {EnvelopeState} after {SendCount} tries", envelope.EnvelopeId, envelope.Kind, envelope.State, envelope.SendCount);

        // the invoice service is told where the envelope of an answer is (sent, or failed to be sent)
        if (envelope.Kind == OutboundKind.InvoiceAnswer && envelope.State is OutboundState.Sent or OutboundState.Failed)
        {
            await status.PublishAnswerAsync(envelope, cancellationToken).ConfigureAwait(false);
        }

        // the invoice service is told: it is sent, or it failed to be (a system response is told of to nobody)
        if (envelope is { Kind: OutboundKind.Invoice, InvoiceRecordId: { } recordId, InvoiceId: { } invoiceId } && Guid.TryParse(envelope.EnvelopeId, out Guid envelopeId))
        {
            using IDisposable scope = tenant.Change(envelope.TenantId);
            if (envelope.State == OutboundState.Sent)
            {
                await eventBus.PublishAsync(new InvoiceSentEto(recordId, invoiceId, envelopeId), cancellationToken: cancellationToken).ConfigureAwait(false);
            }
            else if (envelope.State == OutboundState.Failed)
            {
                await status.PublishAsync(envelope, OutboundOutcome.Failed, cancellationToken).ConfigureAwait(false);
            }
        }
    }
}
