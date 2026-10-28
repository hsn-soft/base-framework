using Edx.Integration.GibInvoice.Application.Options;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;
using Edx.Shared.Contracts.Events.Integration;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Envelope;
using Edx.Shared.Efatura.Ubl;
using Edx.Shared.Helper;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Edx.Integration.GibInvoice.Application.Services;

/// <summary>
/// The checks of the post box (integration guide, 3.2) on an envelope that came in, and what follows. An <b>invoice</b> (<c>SENDERENVELOPE</c>): the zip and the header are read, the post box is one of a tenant (the alias, with its VKN), the documents are invoices that follow the UBL-TR schema, carry a
/// signature (present; the post box would verify it, and this one does not yet) and are for the tenant and from the sender of the envelope, and none was taken before (1163). One that passes is kept as a purchase invoice of the tenant (the invoice service is told) and the post box answers the center with a
/// system response, 1200; one that does not is answered with the code that says why (1100 to 1195). A <b>system response</b> (<c>SYSTEMENVELOPE</c>) is the center's word (or the receiver's post box's, forwarded) about an envelope of the tenant's: it is applied to that envelope, and not answered. Anything
/// else is refused. A failure that is not the envelope's fault (the store, the database) is tried again, up to the limit, and then answered with 1195.
/// </summary>
public sealed class InboundProcessor(
    IInboundEnvelopeRepository inbound,
    IOutboundEnvelopeRepository outbound,
    EnvelopeStorage storage,
    OutboundStatusService status,
    IEventBus eventBus,
    ICurrentTenant tenant,
    TimeProvider time,
    IOptions<GibCenterOptions> options,
    ILogger<InboundProcessor> logger)
{
    private const int MaxProblemsInReason = 3;

    public async Task ProcessAsync(InboundEnvelope item, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.StartProcessing();
        await inbound.UpdateAsync(item, cancellationToken).ConfigureAwait(false);

        EnvelopeHeader? header = null;
        try
        {
            byte[] zip = await storage.ReadAsync(item.ObjectName, cancellationToken).ConfigureAwait(false) ?? throw new EnvelopeException(EfaturaStatus.NotCopiedFromArchive);
            EnvelopeContent content = EnvelopeReader.ReadZip(zip);
            header = content.Header;
            item.ReadHeader(header);
            if (!string.Equals(header.InstanceIdentifier, item.EnvelopeId, StringComparison.Ordinal))
            {
                throw new EnvelopeException(EfaturaStatus.EnvelopeIdAndZipNameDiffer);
            }

            switch (header.DocumentType)
            {
                case EnvelopeTypes.Sender:
                    await AcceptInvoicesAsync(item, content, cancellationToken).ConfigureAwait(false);
                    break;
                case EnvelopeTypes.PostBox:
                    await AcceptAnswerAsync(item, content, cancellationToken).ConfigureAwait(false);
                    break;
                case EnvelopeTypes.System:
                    await ApplySystemResponseAsync(item, content, cancellationToken).ConfigureAwait(false);
                    break;
                default:
                    throw new EnvelopeException(EfaturaStatus.DocumentNotParsed, $"{EfaturaStatus.NameOf(EfaturaStatus.DocumentNotParsed)}: this post box takes invoices, the answers to them and system responses, not a {header.DocumentType}.");
            }
        }
        catch (EnvelopeException exception)
        {
            await RefuseAsync(item, header, exception.Code, exception.Message, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Envelope {EnvelopeId} could not be processed", item.EnvelopeId);
            if (item.ProcessingFailed())
            {
                await RefuseAsync(item, header, EfaturaStatus.SystemError, EfaturaStatus.NameOf(EfaturaStatus.SystemError), cancellationToken).ConfigureAwait(false);
            }
            else
            {
                // it is tried again at the next turn of the worker
                await inbound.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
            }
        }
    }

    // the answer of a receiver to a commercial invoice of one of our tenants: it came to the sender unit of the tenant, is checked like an invoice (schema, signature, who it is from and for), and the invoice service is told
    private async Task AcceptAnswerAsync(InboundEnvelope item, EnvelopeContent content, CancellationToken cancellationToken)
    {
        EnvelopeHeader header = content.Header;
        EInvoiceProfile profile = EdxEInvoiceProfiles.ForSenderUnit(header.Receiver.Alias) ?? throw new EnvelopeException(EfaturaStatus.AddressNotFound);
        if (profile.RegisterNumber != header.Receiver.RegisterNumber)
        {
            throw new EnvelopeException(EfaturaStatus.AuthorityNotChecked, $"{EfaturaStatus.NameOf(EfaturaStatus.AuthorityNotChecked)}: the sender unit {header.Receiver.Alias} is not the one of {header.Receiver.RegisterNumber}.");
        }

        item.AssignTenant(profile.TenantId);
        if (header.ElementType != ElementTypes.ApplicationResponse || content.Elements.Count != 1 || header.ElementCount != 1 || UblDocument.ElementTypeOf(content.Elements[0]) != ElementTypes.ApplicationResponse)
        {
            throw new EnvelopeException(EfaturaStatus.DocumentNotParsed, $"{EfaturaStatus.NameOf(EfaturaStatus.DocumentNotParsed)}: a POSTBOXENVELOPE holds one APPLICATIONRESPONSE document.");
        }

        System.Xml.Linq.XElement document = content.Elements[0];
        IReadOnlyList<string> problems = UblSchemas.Default.Validate(document);
        if (problems.Count > 0)
        {
            throw new EnvelopeException(EfaturaStatus.SchemaFailed, $"{EfaturaStatus.NameOf(EfaturaStatus.SchemaFailed)} => {string.Join(" | ", problems.Take(MaxProblemsInReason))}");
        }

        if (options.Value.RequireSignature && !UblDocument.HasSignature(document))
        {
            throw new EnvelopeException(EfaturaStatus.SignatureOwnerNotFound, $"{EfaturaStatus.NameOf(EfaturaStatus.SignatureOwnerNotFound)}: the answer has no signature.");
        }

        UblInvoiceAnswer answer;
        try
        {
            answer = UblInvoiceAnswer.Read(document);
        }
        catch (UblInvoiceException exception)
        {
            throw new EnvelopeException(EfaturaStatus.DocumentNotParsed, $"{EfaturaStatus.NameOf(EfaturaStatus.DocumentNotParsed)}: {exception.Message}");
        }

        if (answer.Receiver.RegisterNumber != profile.RegisterNumber || answer.Sender.RegisterNumber != header.Sender.RegisterNumber)
        {
            throw new EnvelopeException(EfaturaStatus.AuthorityNotChecked, $"{EfaturaStatus.NameOf(EfaturaStatus.AuthorityNotChecked)}: the answer is from {answer.Sender.RegisterNumber} to {answer.Receiver.RegisterNumber}, the envelope from {header.Sender.RegisterNumber} to {header.Receiver.RegisterNumber}.");
        }

        DateTimeOffset now = time.GetUtcNow();
        using IDisposable scope = tenant.Change(profile.TenantId);
        await eventBus.PublishAsync(
            new SalesInvoiceAnswerReceivedEto(Guid.Parse(item.EnvelopeId), answer.InvoiceNumber, answer.InvoiceUuid, answer.Kind.ToString(), answer.Number, answer.Uuid, answer.Note, storage.Container, item.ObjectName, now),
            cancellationToken: cancellationToken).ConfigureAwait(false);
        item.AcceptAnswer(answer.InvoiceNumber, now);
        await inbound.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Envelope {EnvelopeId} was taken: the answer ({AnswerKind}) of {Receiver} to invoice {InvoiceNumber} of {TenantCode}", item.EnvelopeId, answer.Kind, answer.Sender.RegisterNumber, answer.InvoiceNumber, profile.TenantCode);
    }

    private async Task AcceptInvoicesAsync(InboundEnvelope item, EnvelopeContent content, CancellationToken cancellationToken)
    {
        EnvelopeHeader header = content.Header;

        // the post box is one of a tenant, and the VKN the envelope says is the tenant's
        EInvoiceProfile profile = EdxEInvoiceProfiles.ForPostBox(header.Receiver.Alias) ?? throw new EnvelopeException(EfaturaStatus.AddressNotFound);
        if (profile.RegisterNumber != header.Receiver.RegisterNumber)
        {
            throw new EnvelopeException(EfaturaStatus.AuthorityNotChecked, $"{EfaturaStatus.NameOf(EfaturaStatus.AuthorityNotChecked)}: the post box {header.Receiver.Alias} is not the one of {header.Receiver.RegisterNumber}.");
        }

        item.AssignTenant(profile.TenantId);
        if (header.ElementType != ElementTypes.Invoice || content.Elements.Count == 0 || header.ElementCount != content.Elements.Count)
        {
            throw new EnvelopeException(EfaturaStatus.DocumentNotParsed, $"{EfaturaStatus.NameOf(EfaturaStatus.DocumentNotParsed)}: a SENDERENVELOPE holds INVOICE documents, and the count must be the number of documents.");
        }

        // every document passes, or the whole envelope is refused
        List<(System.Xml.Linq.XElement Document, UblInvoiceSummary Summary)> invoices = [];
        foreach (System.Xml.Linq.XElement document in content.Elements)
        {
            if (UblDocument.ElementTypeOf(document) != ElementTypes.Invoice)
            {
                throw new EnvelopeException(EfaturaStatus.DocumentNotParsed, $"{EfaturaStatus.NameOf(EfaturaStatus.DocumentNotParsed)}: a document is not an INVOICE.");
            }

            IReadOnlyList<string> problems = UblSchemas.Default.Validate(document);
            if (problems.Count > 0)
            {
                throw new EnvelopeException(EfaturaStatus.SchemaFailed, $"{EfaturaStatus.NameOf(EfaturaStatus.SchemaFailed)} => {string.Join(" | ", problems.Take(MaxProblemsInReason))}");
            }

            if (options.Value.RequireSignature && !UblDocument.HasSignature(document))
            {
                throw new EnvelopeException(EfaturaStatus.SignatureOwnerNotFound, $"{EfaturaStatus.NameOf(EfaturaStatus.SignatureOwnerNotFound)}: the invoice has no signature.");
            }

            UblInvoiceSummary summary = UblInvoiceReader.Read(document);
            if (summary.CustomerRegisterNumber != profile.RegisterNumber || summary.SupplierRegisterNumber != header.Sender.RegisterNumber)
            {
                throw new EnvelopeException(EfaturaStatus.AuthorityNotChecked, $"{EfaturaStatus.NameOf(EfaturaStatus.AuthorityNotChecked)}: the invoice {summary.Number} is not from {header.Sender.RegisterNumber} to {profile.RegisterNumber}.");
            }

            if (await inbound.AnyAcceptedInvoiceAsync(summary.Uuid, item.Id, cancellationToken).ConfigureAwait(false) || invoices.Any(known => known.Summary.Uuid == summary.Uuid))
            {
                throw new EnvelopeException(EfaturaStatus.InvoiceAlreadyKnown);
            }

            invoices.Add((document, summary));
        }

        // the envelope is taken: the invoice service is told of each invoice in it (the zip it is in is the only file: the service takes the invoice out of it), and the center is answered
        DateTimeOffset now = time.GetUtcNow();
        using IDisposable scope = tenant.Change(profile.TenantId);
        foreach ((System.Xml.Linq.XElement _, UblInvoiceSummary summary) in invoices)
        {
            await eventBus.PublishAsync(
                new PurchaseInvoiceReceivedEto(Guid.Parse(item.EnvelopeId), summary.Uuid, summary.Number, summary.SupplierRegisterNumber, summary.SupplierName, storage.Container, item.ObjectName, now),
                cancellationToken: cancellationToken).ConfigureAwait(false);
        }

        Guid? answer = await AnswerAsync(item, header, EfaturaStatus.Processed, EfaturaStatus.NameOf(EfaturaStatus.Processed), profile.TenantId, now, cancellationToken).ConfigureAwait(false);
        (System.Xml.Linq.XElement _, UblInvoiceSummary first) = invoices[0];
        item.AcceptInvoice(first.Uuid, first.Number, first.SupplierRegisterNumber, first.SupplierName, answer, now);
        await inbound.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Envelope {EnvelopeId} was taken: {InvoiceCount} invoices for {TenantCode}", item.EnvelopeId, invoices.Count, profile.TenantCode);
    }

    private async Task ApplySystemResponseAsync(InboundEnvelope item, EnvelopeContent content, CancellationToken cancellationToken)
    {
        EnvelopeHeader header = content.Header;
        EInvoiceProfile profile = EdxEInvoiceProfiles.ForSenderUnit(header.Receiver.Alias) ?? throw new EnvelopeException(EfaturaStatus.AddressNotFound);
        item.AssignTenant(profile.TenantId);
        if (header.ElementType != ElementTypes.ApplicationResponse || content.Elements.Count != 1 || header.ElementCount != 1)
        {
            throw new EnvelopeException(EfaturaStatus.DocumentNotParsed, $"{EfaturaStatus.NameOf(EfaturaStatus.DocumentNotParsed)}: a SYSTEMENVELOPE holds one APPLICATIONRESPONSE.");
        }

        IReadOnlyList<string> problems = UblSchemas.Default.Validate(content.Elements[0]);
        if (problems.Count > 0)
        {
            throw new EnvelopeException(EfaturaStatus.SchemaFailed, $"{EfaturaStatus.NameOf(EfaturaStatus.SchemaFailed)} => {string.Join(" | ", problems.Take(MaxProblemsInReason))}");
        }

        SystemResponse response = SystemResponse.FromElement(content.Elements[0]);
        DateTimeOffset now = time.GetUtcNow();
        using IDisposable scope = tenant.Change(profile.TenantId);
        OutboundEnvelope? envelope = await outbound.FindByEnvelopeIdAsync(response.ReferenceEnvelopeId, cancellationToken).ConfigureAwait(false);
        if (envelope is null || envelope.Kind != OutboundKind.Invoice)
        {
            item.Ignore(response.ReferenceEnvelopeId, EfaturaStatus.Processed, "The system response is about an envelope this service did not send.", now);
            await inbound.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
            return;
        }

        // the center's word is the status of the envelope; the word of a post box (the center forwards it) is whether it took the invoice in
        if (header.Sender.RegisterNumber == options.Value.RegisterNumber)
        {
            await status.ApplyCenterStatusAsync(envelope, response.ResultCode, response.ResultDescription, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await status.ApplyReceiverAnswerAsync(envelope, response.ResultCode, response.ResultDescription, cancellationToken).ConfigureAwait(false);
        }

        item.Applied(response.ReferenceEnvelopeId, now);
        await inbound.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
    }

    private async Task RefuseAsync(InboundEnvelope item, EnvelopeHeader? header, int code, string description, CancellationToken cancellationToken)
    {
        DateTimeOffset now = time.GetUtcNow();
        Guid? answer = null;

        // a system response is never answered; an invoice envelope is, when the post box knows whose it is (an alias no tenant owns is answered by nobody)
        if (header is { DocumentType: EnvelopeTypes.Sender } && item.TenantId != Guid.Empty)
        {
            using IDisposable scope = tenant.Change(item.TenantId);
            answer = await AnswerAsync(item, header, code, description, item.TenantId, now, cancellationToken).ConfigureAwait(false);
        }

        if (header is { DocumentType: EnvelopeTypes.System })
        {
            item.Ignore(item.ReferenceEnvelopeId, code, description, now);
        }
        else
        {
            item.Refuse(code, description, answer, now);
        }

        await inbound.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Envelope {EnvelopeId} was refused: {StatusCode} {StatusDescription}", item.EnvelopeId, code, description);
    }

    // the system response of the post box to the center: from the address the envelope was sent to, to the address it came from (what the guides say), kept and waiting to be sent
    private async Task<Guid?> AnswerAsync(InboundEnvelope item, EnvelopeHeader header, int code, string description, Guid tenantId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (!options.Value.SendSystemResponses)
        {
            return null;
        }

        SystemResponse response = new(item.EnvelopeId, header.DocumentType, header.CreatedAt, code, description, now.UtcDateTime, header.Receiver, header.Sender);
        (string envelopeId, byte[] _, byte[] zip) = response.ToEnvelope();
        string objectName = storage.SystemOutboundName(tenantId, now, envelopeId);
        await storage.SaveAsync(objectName, zip, "application/zip", cancellationToken).ConfigureAwait(false);
        OutboundEnvelope envelope = OutboundEnvelope.ForSystemResponse(envelopeId, objectName, zip.Length, header.Receiver, header.Sender, item.EnvelopeId, now);
        await outbound.AddAsync(envelope, cancellationToken).ConfigureAwait(false);
        return envelope.Id;
    }
}
