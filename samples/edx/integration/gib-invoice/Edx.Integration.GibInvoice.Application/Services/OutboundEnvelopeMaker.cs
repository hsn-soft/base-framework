using Edx.Integration.GibInvoice.Domain.DirectoryDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;
using Edx.Shared.Contracts.Events.Integration;
using Edx.Shared.Contracts.Events.Invoicing;
using Edx.Shared.Efatura.Envelope;
using Edx.Shared.Efatura.Ubl;
using Edx.Shared.Helper;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Logging;

namespace Edx.Integration.GibInvoice.Application.Services;

/// <summary>
/// An invoice was approved and signed (<see cref="InvoiceSignedEto"/>, as its tenant): this puts it in an envelope. The sender is the sender unit of the tenant; the receiver is the post box of the customer, which the list of the registered users gives (by the VKN or TCKN of the customer). A
/// customer that is not in the list is not an e-invoice user and cannot be sent to: the invoice fails there, with that reason, and nothing is sent. Otherwise the zip is kept and the envelope waits for the sender (<see cref="OutboundSender"/>).
/// </summary>
public sealed class OutboundEnvelopeMaker(
    IOutboundEnvelopeRepository envelopes,
    IInboundEnvelopeRepository inbound,
    UserDirectory directory,
    EnvelopeStorage storage,
    IEventBus eventBus,
    ICurrentTenant tenant,
    TimeProvider time,
    ILogger<OutboundEnvelopeMaker> logger)
{
    public async Task OnInvoiceSignedAsync(InvoiceSignedEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        Guid tenantId = tenant.Id ?? throw new InvalidOperationException("An envelope is made for a tenant: the event has no tenant.");
        EInvoiceProfile profile = EdxEInvoiceProfiles.ForTenant(tenantId) ?? throw new InvalidOperationException($"The tenant {tenantId} has no e-invoice account.");
        DateTimeOffset now = time.GetUtcNow();
        EnvelopeParty sender = new(profile.SenderUnitAlias, profile.RegisterNumber, profile.Title);

        ExchangeUser? receiver = await directory.FindAsync(message.CustomerRegisterNumber, cancellationToken).ConfigureAwait(false);
        string? alias = receiver?.PostBoxAliases is { Count: > 0 } aliases ? aliases[0] : null;
        if (receiver is null || alias is null)
        {
            string reason = $"The customer {message.CustomerRegisterNumber} ({message.CustomerName}) is not a user of the e-invoice system: it has no post box to send the invoice to.";
            Guid failedId = Guid.NewGuid();
            await envelopes.TryAddAsync(
                OutboundEnvelope.ForUndeliverableInvoice(failedId.ToString(), sender, message.CustomerRegisterNumber, message.CustomerName, message.RecordId, message.InvoiceId, message.InvoiceNumber, message.QueueId, "Receiver", reason, now),
                cancellationToken).ConfigureAwait(false);
            await eventBus.PublishAsync(new InvoiceDeliveryFailedEto(message.RecordId, message.InvoiceId, null, "Receiver", 0, reason), cancellationToken: cancellationToken).ConfigureAwait(false);
            logger.LogInformation("Invoice {InvoiceId} ({InvoiceNumber}) cannot be sent: {Reason}", message.InvoiceId, message.InvoiceNumber, reason);
            return;
        }

        byte[] signed = await storage.ReadAsync(message.Container, message.ObjectName, cancellationToken).ConfigureAwait(false)
                        ?? throw new InvalidOperationException($"The signed invoice '{message.ObjectName}' is not in the store of the invoice service.");
        System.Xml.Linq.XElement invoice = UblXml.Parse(signed);
        UblInvoiceSummary summary = UblInvoiceReader.Read(invoice);
        if (summary.Uuid != message.InvoiceId || summary.SupplierRegisterNumber != profile.RegisterNumber)
        {
            throw new InvalidOperationException($"The signed invoice '{message.ObjectName}' is not invoice {message.InvoiceId} of {profile.RegisterNumber}.");
        }

        (string envelopeId, byte[] zip) = EnvelopeBuilder.BuildInvoiceEnvelope(sender, new EnvelopeParty(alias, receiver.RegisterNumber, receiver.Title), invoice, time.GetLocalNow().DateTime);
        string objectName = storage.SalesName(tenantId, time.GetUtcNow(), envelopeId);
        await storage.SaveAsync(objectName, zip, "application/zip", cancellationToken).ConfigureAwait(false);
        OutboundEnvelope envelope = OutboundEnvelope.ForInvoice(
            envelopeId, objectName, zip.Length, sender, new EnvelopeParty(alias, receiver.RegisterNumber, receiver.Title), message.RecordId, message.InvoiceId, message.InvoiceNumber, message.QueueId, now);
        await envelopes.TryAddAsync(envelope, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("The envelope {EnvelopeId} of invoice {InvoiceId} ({InvoiceNumber}) for {ReceiverAlias} was made", envelopeId, message.InvoiceId, message.InvoiceNumber, alias);
    }

    /// <summary>
    /// The answer of a tenant to a commercial invoice that came in was signed (<see cref="PurchaseAnswerSignedEto"/>, as the tenant): it is put in an envelope of its own (<c>POSTBOXENVELOPE</c>) from the post box of the tenant to the sender unit the invoice came from (the envelope the invoice came in says which),
    /// and waits for the sender worker like an invoice envelope does.
    /// </summary>
    public async Task OnPurchaseAnswerSignedAsync(PurchaseAnswerSignedEto message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        Guid tenantId = tenant.Id ?? throw new InvalidOperationException("An envelope is made for a tenant: the event has no tenant.");
        EInvoiceProfile profile = EdxEInvoiceProfiles.ForTenant(tenantId) ?? throw new InvalidOperationException($"The tenant {tenantId} has no e-invoice account.");
        InboundEnvelope came = await inbound.FindByEnvelopeIdAsync(message.InvoiceEnvelopeId.ToString(), cancellationToken).ConfigureAwait(false)
                               ?? throw new InvalidOperationException($"The envelope {message.InvoiceEnvelopeId} the invoice {message.InvoiceUuid} came in is not known.");
        if (string.IsNullOrEmpty(came.SenderAlias) || came.SenderRegisterNumber != message.SupplierRegisterNumber)
        {
            throw new InvalidOperationException($"The envelope {message.InvoiceEnvelopeId} does not say where the supplier {message.SupplierRegisterNumber} is to be answered.");
        }

        byte[] signed = await storage.ReadAsync(message.Container, message.ObjectName, cancellationToken).ConfigureAwait(false)
                        ?? throw new InvalidOperationException($"The signed answer '{message.ObjectName}' is not in the store of the invoice service.");
        System.Xml.Linq.XElement answer = UblXml.Parse(signed);
        UblInvoiceAnswer read = UblInvoiceAnswer.Read(answer);
        if (read.Uuid != message.AnswerUuid || read.Sender.RegisterNumber != profile.RegisterNumber)
        {
            throw new InvalidOperationException($"The signed answer '{message.ObjectName}' is not the answer {message.AnswerUuid} of {profile.RegisterNumber}.");
        }

        DateTimeOffset now = time.GetUtcNow();
        EnvelopeParty from = new(profile.PostBoxAlias, profile.RegisterNumber, profile.Title);
        EnvelopeParty to = new(came.SenderAlias, came.SenderRegisterNumber ?? message.SupplierRegisterNumber, came.SenderTitle ?? message.SupplierName);
        (string envelopeId, byte[] zip) = EnvelopeBuilder.BuildAnswerEnvelope(from, to, answer, time.GetLocalNow().DateTime);
        string objectName = storage.PurchaseAnswerName(tenantId, now, envelopeId);
        await storage.SaveAsync(objectName, zip, "application/zip", cancellationToken).ConfigureAwait(false);
        await envelopes.TryAddAsync(OutboundEnvelope.ForAnswer(envelopeId, objectName, zip.Length, from, to, message.InvoiceUuid, message.InvoiceNumber, message.AnswerUuid, message.InvoiceEnvelopeId.ToString(), now), cancellationToken).ConfigureAwait(false);
        logger.LogInformation("The envelope {EnvelopeId} of the answer ({AnswerKind}) to invoice {InvoiceUuid} ({InvoiceNumber}) for {ReceiverAlias} was made", envelopeId, message.Kind, message.InvoiceUuid, message.InvoiceNumber, to.Alias);
    }
}
