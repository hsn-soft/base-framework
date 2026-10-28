using Edx.Mock.Center.Domain.Envelopes;
using Edx.Mock.Center.Domain.Participants;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Client;
using Edx.Shared.Efatura.Envelope;
using Microsoft.Extensions.Options;

namespace Edx.Mock.Center.Wcf.Host.Services;

/// <summary>
/// The center does not wait for ever for the post box to answer. An envelope that was delivered and is still 1220 (the post box has not sent its system response) is asked about, after a while, with the <c>getApplicationResponse</c> of the post box (integrators serve it too, for this):
/// the system envelope it gives back changes the status of the envelope (1300 or 1230) and is then forwarded to the sender, as if the post box had sent it.
/// </summary>
public sealed class AnswerPoller(IEnvelopeQueueRepository queue, IParticipantRepository participants, EnvelopeStorage storage, TimeProvider time, IOptions<CenterOptions> options, ILogger<AnswerPoller> logger)
{
    public async Task PollAsync(EnvelopeQueueItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        DateTimeOffset now = time.GetUtcNow();
        Participant? receiver = item.ReceiverParticipantId is { } id ? await participants.FindAsync(id, cancellationToken).ConfigureAwait(false) : null;
        string? endpoint = item.ReceiverAlias is null ? null : receiver?.FindAlias(item.ReceiverAlias)?.Endpoint;
        if (endpoint is null)
        {
            item.Polled(now);
            await queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
            return;
        }

        using EFaturaClient client = new(new Uri(endpoint), options.Value.MaxReceivedMessageSize);
        EFaturaResult result = await client.QueryAsync(item.EnvelopeId).ConfigureAwait(false);
        if (!result.Accepted || string.IsNullOrEmpty(result.ApplicationResponse))
        {
            logger.LogInformation("The post box of envelope {EnvelopeId} has no answer yet ({Code} {Message})", item.EnvelopeId, result.Code, result.Message);
            item.Polled(now);
            await queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            byte[] xml = System.Text.Encoding.UTF8.GetBytes(result.ApplicationResponse);
            EnvelopeContent answer = EnvelopeReader.ReadXml(xml);
            SystemResponse response = SystemResponse.FromElement(answer.Elements[0]);
            if (answer.Header.DocumentType != EnvelopeTypes.System || response.ReferenceEnvelopeId != item.EnvelopeId)
            {
                throw new EnvelopeException(EfaturaStatus.DocumentNotParsed);
            }

            // the post box is still working on the envelope (queued, being analyzed): that is no answer, it is asked again after the same wait
            if (response.ResultCode is EfaturaStatus.Queued or EfaturaStatus.Processing)
            {
                logger.LogInformation("The post box of envelope {EnvelopeId} is still working on it ({Code})", item.EnvelopeId, response.ResultCode);
                item.Polled(now);
                await queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
                return;
            }

            item.Answered(response.ResultCode, response.ResultDescription);
            await queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
            // the sender asks for the status; the post box's answer is pushed to it as a system envelope only when the center is set to send them
            if (options.Value.SendSystemResponses)
            {
                await ForwardAsync(item, answer, xml, now, cancellationToken).ConfigureAwait(false);
            }

            logger.LogInformation("Envelope {EnvelopeId} was answered by asking its post box: {StatusCode}", item.EnvelopeId, item.StatusCode);
        }
        catch (EnvelopeException exception)
        {
            logger.LogWarning("The answer of the post box about envelope {EnvelopeId} is not a system response ({Code})", item.EnvelopeId, exception.Code);
            item.Polled(now);
            await queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
        }
    }

    // the answer goes to the sender, as the post box made it (unless the post box sent it by itself meanwhile)
    private async Task ForwardAsync(EnvelopeQueueItem item, EnvelopeContent answer, byte[] xml, DateTimeOffset now, CancellationToken cancellationToken)
    {
        if (await queue.FindByEnvelopeIdAsync(answer.Header.InstanceIdentifier, cancellationToken).ConfigureAwait(false) is not null || item.SenderParticipantId is not { } senderId)
        {
            return;
        }

        Participant? sender = await participants.FindAsync(senderId, cancellationToken).ConfigureAwait(false);
        if (sender is null)
        {
            return;
        }

        byte[] zip = EnvelopeBuilder.Zip(answer.Header.InstanceIdentifier, xml);
        Guid id = Guid.NewGuid();
        string objectName = storage.CenterName(id, answer.Header.InstanceIdentifier, now);
        await storage.SaveAsync(objectName, zip, cancellationToken).ConfigureAwait(false);
        await queue.AddAsync(EnvelopeQueueItem.Respond(id, answer.Header.InstanceIdentifier, objectName, zip.Length, answer.Header, sender, item.EnvelopeId, now), cancellationToken).ConfigureAwait(false);
    }
}
