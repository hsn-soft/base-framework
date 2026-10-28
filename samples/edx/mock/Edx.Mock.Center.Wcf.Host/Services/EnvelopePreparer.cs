using Edx.Mock.Center.Domain.Envelopes;
using Edx.Mock.Center.Domain.Participants;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Envelope;
using Microsoft.Extensions.Options;

namespace Edx.Mock.Center.Wcf.Host.Services;

/// <summary>
/// What follows the analysis (integration guide, 4.4.1). An invoice or an application response gets a **system response** to its sender, which says what the analysis found (taken, 1200, or refused with the code); one that was taken is then delivered, as it came, to the address of the receiver. A
/// **system envelope** (the answer of the one that processed an envelope) changes the status of the envelope it is about (1300 when it took it in, 1230 when it did not) and is then **forwarded as it is** to the address it is for; a system envelope that was refused is not answered.
/// </summary>
public sealed class EnvelopePreparer(IEnvelopeQueueRepository queue, IParticipantRepository participants, EnvelopeStorage storage, TimeProvider time, IOptions<CenterOptions> options, ILogger<EnvelopePreparer> logger)
{
    public async Task PrepareAsync(EnvelopeQueueItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        DateTimeOffset now = time.GetUtcNow();
        bool accepted = item.StatusCode == EfaturaStatus.Processed;
        if (item.DocumentType == EnvelopeTypes.System)
        {
            if (accepted)
            {
                await ApplyAnswerAsync(item, cancellationToken).ConfigureAwait(false);
            }

            item.Prepared(deliver: accepted, now);
            await queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
            return;
        }

        // the sender is told what the analysis found; a sender the center cannot place (or cannot address) is not
        Participant? sender = item.SenderParticipantId is { } senderId ? await participants.FindAsync(senderId, cancellationToken).ConfigureAwait(false) : null;
        if (options.Value.SendSystemResponses && sender is not null && item.SenderAlias is not null && sender.FindAlias(item.SenderAlias) is not null)
        {
            await RespondAsync(item, sender, now, cancellationToken).ConfigureAwait(false);
        }
        else
        {
            logger.LogInformation("Envelope {EnvelopeId}: no system envelope goes to the sender (it asks for the status, or the center cannot address it)", item.EnvelopeId);
        }

        item.Prepared(deliver: accepted && item.DocumentType is EnvelopeTypes.Sender or EnvelopeTypes.PostBox, now);
        await queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
    }

    private async Task RespondAsync(EnvelopeQueueItem item, Participant sender, DateTimeOffset now, CancellationToken cancellationToken)
    {
        EnvelopeParty to = new(item.SenderAlias!, sender.RegisterNumber, sender.Title);
        SystemResponse response = new(
            item.EnvelopeId, item.DocumentType ?? EnvelopeTypes.Sender, item.EnvelopeCreatedAt ?? item.ReceivedAt.UtcDateTime, item.StatusCode, item.StatusDescription, now.UtcDateTime, storage.CenterParty, to);
        (string envelopeId, byte[] xml, byte[] zip) = response.ToEnvelope();
        EnvelopeHeader header = EnvelopeReader.ReadXml(xml).Header;

        Guid id = Guid.NewGuid();
        string objectName = storage.CenterName(id, envelopeId, now);
        await storage.SaveAsync(objectName, zip, cancellationToken).ConfigureAwait(false);
        await queue.AddAsync(EnvelopeQueueItem.Respond(id, envelopeId, objectName, zip.Length, header, sender, item.EnvelopeId, now), cancellationToken).ConfigureAwait(false);
        logger.LogInformation("A system response ({StatusCode}) for envelope {EnvelopeId} is queued for {Alias}", item.StatusCode, item.EnvelopeId, to.Alias);
    }

    // the one that processed a document answered about it: that document's status follows
    private async Task ApplyAnswerAsync(EnvelopeQueueItem answer, CancellationToken cancellationToken)
    {
        byte[]? zip = await storage.ReadAsync(answer.ObjectName, cancellationToken).ConfigureAwait(false);
        if (zip is null)
        {
            logger.LogWarning("The system envelope {EnvelopeId} is not in the store", answer.EnvelopeId);
            return;
        }

        SystemResponse response = SystemResponse.FromElement(EnvelopeReader.ReadZip(zip).Elements[0]);
        EnvelopeQueueItem? referenced = await queue.FindByEnvelopeIdAsync(response.ReferenceEnvelopeId, cancellationToken).ConfigureAwait(false);
        if (referenced is null)
        {
            logger.LogWarning("The system envelope {EnvelopeId} answers about {ReferenceEnvelopeId}, which the center does not have", answer.EnvelopeId, response.ReferenceEnvelopeId);
            return;
        }

        referenced.Answered(response.ResultCode, response.ResultDescription);
        await queue.UpdateAsync(referenced, cancellationToken).ConfigureAwait(false);
        logger.LogInformation("Envelope {EnvelopeId} was answered: {StatusCode}", referenced.EnvelopeId, referenced.StatusCode);
    }
}
