using System.Globalization;
using Edx.Mock.Center.Domain.Envelopes;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Envelope;
using Edx.Shared.Efatura.Server;

namespace Edx.Mock.Center.Wcf.Host.Services;

/// <summary>
/// What <c>sendDocument</c> and <c>getApplicationResponse</c> do. An envelope is handed over with its name (<c>{uuid}.zip</c>) and its MD5: the name has to be a uuid and a zip, the hash has to be the zip's, and the uuid must not be in the center already; then the zip is kept
/// and the envelope is queued (1000). Nothing else is checked here: the analysis is the worker's, so the answer is fast and a sender learns the rest by asking.
/// </summary>
public sealed class EnvelopeIntake(IEnvelopeQueueRepository queue, EnvelopeStorage storage, TimeProvider time, ILogger<EnvelopeIntake> logger)
{
    /// <summary>Queues the envelope; returns the message of the center (<c>ZARF KUYRUGA EKLENDI</c>).</summary>
    /// <exception cref="EFaturaFaultException">The center refuses the call (2000 the hash, 2001 the uuid is there, 2002 the zip could not be kept, 2003 it could not be queued, 2005 a parameter is missing, 2006 the name).</exception>
    public async Task<string> ReceiveAsync(string? fileName, byte[]? content, string? hash, CancellationToken cancellationToken = default)
    {
        string envelopeId = SendDocumentRules.Check(fileName, content, hash);
        byte[] zip = content!;

        if (await queue.FindByEnvelopeIdAsync(envelopeId, cancellationToken).ConfigureAwait(false) is not null)
        {
            throw new EFaturaFaultException(EfaturaStatus.EnvelopeAlreadyThere);
        }

        DateTimeOffset now = time.GetUtcNow();
        Guid id = Guid.NewGuid();
        string objectName = storage.ReceivedName(id, envelopeId, now);
        bool saved = false;
        try
        {
            await storage.SaveAsync(objectName, zip, cancellationToken).ConfigureAwait(false);
            saved = true;
            if (!await queue.TryAddAsync(EnvelopeQueueItem.Receive(id, envelopeId, objectName, zip.Length, now), cancellationToken).ConfigureAwait(false))
            {
                // another call with the same uuid was first
                await storage.DeleteAsync(objectName, CancellationToken.None).ConfigureAwait(false);
                throw new EFaturaFaultException(EfaturaStatus.EnvelopeAlreadyThere);
            }
        }
        catch (Exception exception) when (exception is not EFaturaFaultException and not OperationCanceledException)
        {
            logger.LogError(exception, "Envelope {EnvelopeId} could not be queued", envelopeId);
            throw new EFaturaFaultException(saved ? EfaturaStatus.NotQueued : EfaturaStatus.NotArchived, $"{EfaturaStatus.NameOf(saved ? EfaturaStatus.NotQueued : EfaturaStatus.NotArchived)}:{envelopeId}");
        }

        logger.LogInformation("Envelope {EnvelopeId} was queued ({ContentLength} bytes)", envelopeId, zip.Length);
        return EfaturaStatus.NameOf(EfaturaStatus.Queued);
    }

    /// <summary>The system envelope (XML) that says what became of the envelope: its status code and text.</summary>
    /// <exception cref="EFaturaFaultException">The uuid is not one (2006), the center has no such envelope (2004).</exception>
    public async Task<string> QueryAsync(string? instanceIdentifier, CancellationToken cancellationToken = default)
    {
        string envelopeId = SendDocumentRules.CheckInstance(instanceIdentifier);

        EnvelopeQueueItem item = await queue.FindByEnvelopeIdAsync(envelopeId, cancellationToken).ConfigureAwait(false) ?? throw new EFaturaFaultException(EfaturaStatus.EnvelopeNotFound);
        DateTimeOffset now = time.GetUtcNow();

        // the answer goes to whoever the envelope was from, when the center knows that; else it is not addressed
        EnvelopeParty center = storage.CenterParty;
        EnvelopeParty receiver = item.SenderAlias is not null ? new EnvelopeParty(item.SenderAlias, item.SenderRegisterNumber ?? string.Empty, item.SenderTitle ?? string.Empty) : center;
        SystemResponse response = new(item.EnvelopeId, item.DocumentType ?? EnvelopeTypes.Sender, item.ReceivedAt.UtcDateTime, item.StatusCode, item.StatusDescription, now.UtcDateTime, center, receiver);
        (_, byte[] xml, _) = response.ToEnvelope();
        logger.LogInformation("Envelope {EnvelopeId} was asked about: {StatusCode}", item.EnvelopeId, item.StatusCode.ToString(CultureInfo.InvariantCulture));
        return System.Text.Encoding.UTF8.GetString(xml);
    }
}
