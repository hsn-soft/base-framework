using Edx.Mock.Center.Domain.Envelopes;
using Edx.Mock.Center.Domain.Participants;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Envelope;
using Edx.Shared.Efatura.Ubl;
using Microsoft.Extensions.Options;

namespace Edx.Mock.Center.Wcf.Host.Services;

/// <summary>
/// The analysis of a queued envelope, the checks the center makes (integration guide, section 4.4): the zip holds one XML (1110, 1130, 1131, 1132); it is an envelope of the versions the center knows (1140, 1141, 1111, 1133, 1142, 1143); the sender and the receiver are registered (1170) and
/// both addresses are said (1180); the sender may send from its alias (a sender unit for an invoice, 1171; a post box for an application response, 1172) and the receiver has the address it is sent to (1181); the documents are of the kind the envelope says and follow their **XML schema** (1160);
/// they carry a signature (**present, not verified**, 1161); an invoice was not sent before (1163). An envelope that passes is 1200; one that does not is refused with the code, and the sender is told. A document that fails makes the whole envelope fail.
/// </summary>
public sealed class EnvelopeAnalyzer(IEnvelopeQueueRepository queue, IParticipantRepository participants, EnvelopeStorage storage, IOptions<CenterOptions> options, ILogger<EnvelopeAnalyzer> logger)
{
    private const int MaxProblemsInDescription = 3;

    public async Task AnalyzeAsync(EnvelopeQueueItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        item.StartAnalysis();
        await queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);

        EnvelopeHeader? header = null;
        Participant? sender = null;
        Participant? receiver = null;
        try
        {
            byte[] zip = await storage.ReadAsync(item.ObjectName, cancellationToken).ConfigureAwait(false) ?? throw new EnvelopeException(EfaturaStatus.NotCopiedFromArchive);
            EnvelopeContent content = EnvelopeReader.ReadZip(zip);
            header = content.Header;

            if (!string.Equals(header.InstanceIdentifier, item.EnvelopeId, StringComparison.Ordinal))
            {
                throw new EnvelopeException(EfaturaStatus.EnvelopeIdAndZipNameDiffer);
            }

            if (string.IsNullOrEmpty(header.Sender.RegisterNumber) || string.IsNullOrEmpty(header.Receiver.RegisterNumber))
            {
                throw new EnvelopeException(EfaturaStatus.AuthorityNotChecked);
            }

            sender = await participants.FindByRegisterNumberAsync(header.Sender.RegisterNumber, cancellationToken).ConfigureAwait(false);
            receiver = await participants.FindByRegisterNumberAsync(header.Receiver.RegisterNumber, cancellationToken).ConfigureAwait(false);
            if (sender is null || receiver is null)
            {
                throw new EnvelopeException(EfaturaStatus.AuthorityNotChecked);
            }

            if (string.IsNullOrEmpty(header.Sender.Alias) || string.IsNullOrEmpty(header.Receiver.Alias))
            {
                throw new EnvelopeException(EfaturaStatus.AddressNotChecked);
            }

            string expectedElementType = CheckAddresses(header, sender, receiver);
            List<string> keys = await CheckDocumentsAsync(item, content, sender, expectedElementType, cancellationToken).ConfigureAwait(false);
            if (header.DocumentType == EnvelopeTypes.System)
            {
                item.ReferTo(SystemResponse.FromElement(content.Elements[0]).ReferenceEnvelopeId);
            }

            item.Accept(header, sender, receiver, keys, content.Elements.Count);
        }
        catch (EnvelopeException exception)
        {
            item.Refuse(exception.Code, exception.Message, header, sender, receiver);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogError(exception, "Envelope {EnvelopeId} could not be analyzed", item.EnvelopeId);
            item.Refuse(EfaturaStatus.SystemError, EfaturaStatus.NameOf(EfaturaStatus.SystemError), header, sender, receiver);
        }

        await queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Envelope {EnvelopeId} was analyzed: {StatusCode} {StatusDescription}", item.EnvelopeId, item.StatusCode, item.StatusDescription);
    }

    // the sender may send from its alias, the receiver has the address; returns the kind of document the envelope has to carry
    private static string CheckAddresses(EnvelopeHeader header, Participant sender, Participant receiver)
    {
        switch (header.DocumentType)
        {
            case EnvelopeTypes.Sender:
                Require(sender.FindAlias(header.Sender.Alias, AliasType.SenderUnit), EfaturaStatus.SenderUnitNotAuthorized);
                Require(receiver.FindAlias(header.Receiver.Alias, AliasType.PostBox), EfaturaStatus.AddressNotFound);
                return ElementTypes.Invoice;
            case EnvelopeTypes.PostBox:
                // an application response goes from the post box of the receiver of an invoice to the sender unit of its sender
                Require(sender.FindAlias(header.Sender.Alias, AliasType.PostBox), EfaturaStatus.PostBoxNotAuthorized);
                Require(receiver.FindAlias(header.Receiver.Alias, AliasType.SenderUnit), EfaturaStatus.AddressNotFound);
                return ElementTypes.ApplicationResponse;
            case EnvelopeTypes.System:
                // whoever processed an envelope answers it, from the address it was sent to, to the address it came from
                Require(sender.FindAlias(header.Sender.Alias), EfaturaStatus.AuthorityNotChecked);
                Require(receiver.FindAlias(header.Receiver.Alias), EfaturaStatus.AddressNotFound);
                return ElementTypes.ApplicationResponse;
            default:
                throw new EnvelopeException(EfaturaStatus.DocumentNotParsed, $"{EfaturaStatus.NameOf(EfaturaStatus.DocumentNotParsed)}: the envelope type '{header.DocumentType}' is not handled by this center (only invoices, application responses and system responses).");
        }
    }

    private async Task<List<string>> CheckDocumentsAsync(EnvelopeQueueItem item, EnvelopeContent content, Participant sender, string expectedElementType, CancellationToken cancellationToken)
    {
        EnvelopeHeader header = content.Header;
        if (!string.Equals(header.ElementType, expectedElementType, StringComparison.Ordinal) || content.Elements.Count == 0 || header.ElementCount != content.Elements.Count)
        {
            throw new EnvelopeException(EfaturaStatus.DocumentNotParsed, $"{EfaturaStatus.NameOf(EfaturaStatus.DocumentNotParsed)}: a {header.DocumentType} holds {expectedElementType} documents, and the count must be the number of documents.");
        }

        if (content.Elements.Count > options.Value.MaxDocumentsPerEnvelope)
        {
            throw new EnvelopeException(EfaturaStatus.DocumentNotParsed, $"{EfaturaStatus.NameOf(EfaturaStatus.DocumentNotParsed)}: an envelope holds at most {options.Value.MaxDocumentsPerEnvelope} documents, this one has {content.Elements.Count}.");
        }

        UblSchemas schemas = UblSchemas.Default;
        List<string> keys = [];
        foreach (System.Xml.Linq.XElement document in content.Elements)
        {
            if (UblDocument.ElementTypeOf(document) != expectedElementType)
            {
                throw new EnvelopeException(EfaturaStatus.DocumentNotParsed, $"{EfaturaStatus.NameOf(EfaturaStatus.DocumentNotParsed)}: a document is not a {expectedElementType}.");
            }

            IReadOnlyList<string> problems = schemas.Validate(document);
            if (problems.Count > 0)
            {
                throw new EnvelopeException(EfaturaStatus.SchemaFailed, $"{EfaturaStatus.NameOf(EfaturaStatus.SchemaFailed)} => {string.Join(" | ", problems.Take(MaxProblemsInDescription))}");
            }

            // an envelope is the post: every invoice in it is for the post box the envelope is addressed to (the receiver of the envelope is the customer of each invoice)
            if (header.DocumentType == EnvelopeTypes.Sender)
            {
                CheckReceiver(document, header);
            }

            // a system response needs no signature (the guides: signing it is not required); an invoice and an application response do
            if (header.DocumentType != EnvelopeTypes.System && options.Value.RequireSignature && !UblDocument.HasSignature(document))
            {
                throw new EnvelopeException(EfaturaStatus.SignatureOwnerNotFound, $"{EfaturaStatus.NameOf(EfaturaStatus.SignatureOwnerNotFound)}: the document has no signature.");
            }

            if (header.DocumentType != EnvelopeTypes.System)
            {
                keys.AddRange(Keys(document, sender.RegisterNumber, expectedElementType));
            }
        }

        // an invoice (or an answer) that an envelope the center still holds already has is not taken twice; one that failed on its way (1215, 1230) may be sent again
        if (keys.Count > 0 && await queue.AnyHoldingDocumentAsync(keys, item.Id, cancellationToken).ConfigureAwait(false))
        {
            bool invoice = expectedElementType == ElementTypes.Invoice;
            throw new EnvelopeException(invoice ? EfaturaStatus.InvoiceAlreadyKnown : EfaturaStatus.DocumentAlreadyKnown);
        }

        return keys;
    }

    private static void CheckReceiver(System.Xml.Linq.XElement invoice, EnvelopeHeader header)
    {
        string customer;
        try
        {
            customer = UblInvoiceReader.Read(invoice).CustomerRegisterNumber;
        }
        catch (UblInvoiceException exception)
        {
            throw new EnvelopeException(EfaturaStatus.DocumentNotParsed, $"{EfaturaStatus.NameOf(EfaturaStatus.DocumentNotParsed)}: {exception.Message}");
        }

        if (!string.Equals(customer, header.Receiver.RegisterNumber, StringComparison.Ordinal))
        {
            throw new EnvelopeException(EfaturaStatus.AddressNotFound, $"{EfaturaStatus.NameOf(EfaturaStatus.AddressNotFound)}: an invoice of the envelope is for {customer}, the envelope is for {header.Receiver.RegisterNumber} (an envelope holds the invoices of one post box).");
        }
    }

    private static IEnumerable<string> Keys(System.Xml.Linq.XElement document, string senderRegisterNumber, string elementType)
    {
        string kind = elementType == ElementTypes.Invoice ? "invoice" : "response";
        if (UblDocument.Id(document) is { Length: > 0 } id)
        {
            yield return $"{kind}:{senderRegisterNumber}:{id}";
        }

        if (UblDocument.Uuid(document) is { Length: > 0 } uuid)
        {
            yield return $"{kind}:{uuid.ToUpperInvariant()}";
        }
    }

    private static void Require(ParticipantAlias? alias, int code)
    {
        if (alias is null)
        {
            throw new EnvelopeException(code);
        }
    }
}
