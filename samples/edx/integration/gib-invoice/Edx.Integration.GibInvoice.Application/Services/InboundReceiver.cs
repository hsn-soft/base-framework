using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Envelope;
using Edx.Shared.Efatura.Server;
using Edx.Shared.Helper;
using Microsoft.Extensions.Logging;

namespace Edx.Integration.GibInvoice.Application.Services;

/// <summary>
/// What the post box does when the center calls it. <c>sendDocument</c>: the name, the MD5 and the uuid are checked (2006, 2000, 2001), the zip is kept and the envelope is queued (1000); the checks of the envelope are the worker's, so the center is answered fast. <c>getApplicationResponse</c>: what the post
/// box says of the envelope so far, as a system envelope (the center asks for the envelopes it has had no system response for).
/// </summary>
public sealed class InboundReceiver(IInboundEnvelopeRepository envelopes, EnvelopeStorage storage, TimeProvider time, ILogger<InboundReceiver> logger)
{
    /// <summary>Queues the envelope; returns the message of the post box (<c>ZARF KUYRUGA EKLENDI</c>).</summary>
    /// <exception cref="EFaturaFaultException">The call is refused (2000 the hash, 2001 the uuid is there, 2002 the zip could not be kept, 2003 it could not be queued, 2005 a parameter is missing, 2006 the name).</exception>
    public async Task<string> ReceiveAsync(string? fileName, byte[]? content, string? hash, CancellationToken cancellationToken = default)
    {
        string envelopeId = SendDocumentRules.Check(fileName, content, hash);
        byte[] zip = content!;

        if (await envelopes.FindByEnvelopeIdAsync(envelopeId, cancellationToken).ConfigureAwait(false) is not null)
        {
            throw new EFaturaFaultException(EfaturaStatus.EnvelopeAlreadyThere);
        }

        DateTimeOffset now = time.GetUtcNow();
        (Guid? tenantId, string? documentType) = Owner(zip);
        string objectName = documentType switch
        {
            EnvelopeTypes.Sender => storage.PurchaseName(tenantId, now, envelopeId),
            EnvelopeTypes.PostBox => storage.SalesAnswerName(tenantId, now, envelopeId),
            _ => storage.SystemInboundName(tenantId, now, envelopeId),
        };
        bool saved = false;
        try
        {
            await storage.SaveAsync(objectName, zip, "application/zip", cancellationToken).ConfigureAwait(false);
            saved = true;
            if (!await envelopes.TryAddAsync(InboundEnvelope.Receive(envelopeId, objectName, zip.Length, now), cancellationToken).ConfigureAwait(false))
            {
                throw new EFaturaFaultException(EfaturaStatus.EnvelopeAlreadyThere);
            }
        }
        catch (Exception exception) when (exception is not EFaturaFaultException and not OperationCanceledException)
        {
            logger.LogError(exception, "Envelope {EnvelopeId} could not be queued", envelopeId);
            throw new EFaturaFaultException(saved ? EfaturaStatus.NotQueued : EfaturaStatus.NotArchived, $"{EfaturaStatus.NameOf(saved ? EfaturaStatus.NotQueued : EfaturaStatus.NotArchived)}:{envelopeId}");
        }

        logger.LogInformation("Envelope {EnvelopeId} came in from the center ({ContentLength} bytes)", envelopeId, zip.Length);
        return EfaturaStatus.NameOf(EfaturaStatus.Queued);
    }

    // the tenant the envelope is for (it owns the post box, or the sender unit, the envelope is addressed to) and what kind of envelope it is, when the header can be read; the checks come later, here it only decides the folder
    private static (Guid? TenantId, string? DocumentType) Owner(byte[] zip)
    {
        try
        {
            EnvelopeHeader header = EnvelopeReader.ReadZip(zip).Header;
            return ((EdxEInvoiceProfiles.ForPostBox(header.Receiver.Alias) ?? EdxEInvoiceProfiles.ForSenderUnit(header.Receiver.Alias))?.TenantId, header.DocumentType);
        }
        catch (EnvelopeException)
        {
            return (null, null);
        }
    }

    /// <summary>The system envelope (XML) that says what the post box made of the envelope: its status code and text.</summary>
    /// <exception cref="EFaturaFaultException">The uuid is not one (2006), the post box has no such envelope (2004).</exception>
    public async Task<string> QueryAsync(string? instanceIdentifier, CancellationToken cancellationToken = default)
    {
        string envelopeId = SendDocumentRules.CheckInstance(instanceIdentifier);
        InboundEnvelope item = await envelopes.FindByEnvelopeIdAsync(envelopeId, cancellationToken).ConfigureAwait(false) ?? throw new EFaturaFaultException(EfaturaStatus.EnvelopeNotFound);

        // the answer is from the address the envelope was sent to, to the address it came from; before the envelope was read nobody knows them
        EnvelopeParty from = new(item.ReceiverAlias ?? "urn:mail:unknown", item.ReceiverRegisterNumber ?? "0000000000", item.ReceiverTitle ?? "-");
        EnvelopeParty to = new(item.SenderAlias ?? "urn:mail:unknown", item.SenderRegisterNumber ?? "0000000000", item.SenderTitle ?? "-");
        DateTime now = time.GetUtcNow().UtcDateTime;
        SystemResponse response = new(item.EnvelopeId, item.DocumentType ?? EnvelopeTypes.Sender, item.EnvelopeCreatedAt ?? item.ReceivedAt.UtcDateTime, item.StatusCode, item.StatusDescription, now, from, to);
        (_, byte[] xml, _) = response.ToEnvelope();
        return System.Text.Encoding.UTF8.GetString(xml);
    }
}
