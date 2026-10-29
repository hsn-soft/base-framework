using Edx.Mock.Center.Domain.Envelopes;
using Edx.Mock.Center.Domain.Participants;
using Edx.Shared.Efatura.Client;
using Microsoft.Extensions.Options;

namespace Edx.Mock.Center.Wcf.Host.Services;

/// <summary>
/// Delivers an envelope to the address it was sent to: the alias of the receiver names the service of its integrator, and the zip goes to it with the same <c>sendDocument</c> the center offers. A service that took it is the end of the delivery; one that could not be reached is tried again after a
/// wait, until the retries are used up (1215); one that refused (a fault) is not tried again.
/// </summary>
public sealed class EnvelopeDeliverer(IEnvelopeQueueRepository queue, IParticipantRepository participants, EnvelopeStorage storage, TimeProvider time, IOptions<CenterOptions> options, ILogger<EnvelopeDeliverer> logger)
{
    public async Task DeliverAsync(EnvelopeQueueItem item, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(item);

        DateTimeOffset now = time.GetUtcNow();
        string? endpoint = await EndpointAsync(item, cancellationToken).ConfigureAwait(false);
        byte[]? zip = endpoint is null ? null : await storage.ReadAsync(item.ObjectName, cancellationToken).ConfigureAwait(false);
        if (endpoint is null || zip is null)
        {
            item.DeliveryFailed(endpoint is null ? "The receiver has no such address." : "The envelope is not in the store.", now, now, retry: false);
        }
        else
        {
            using EFaturaClient client = new(new Uri(endpoint), options.Value.MaxReceivedMessageSize);
            EFaturaResult result = await client.SendAsync(item.EnvelopeId, zip).ConfigureAwait(false);
            if (result.Accepted)
            {
                item.Delivered(now);
                logger.LogInformation("Envelope {EnvelopeId} was delivered to {Endpoint}", item.EnvelopeId, endpoint);
            }
            else
            {
                string error = result.Code == 0 ? result.Message ?? "unknown" : $"{result.Code}:{result.Message}";
                // the first try and then up to ResendLimit more (the GİB center: four, two hours apart)
                bool retry = result.CanRetry && item.SendCount < options.Value.ResendLimit;
                item.DeliveryFailed(error, now, now.AddSeconds(options.Value.ResendWaitSeconds), retry);
                logger.LogWarning("Envelope {EnvelopeId} was not delivered to {Endpoint} ({Error}); {Next}", item.EnvelopeId, endpoint, error, retry ? "it will be tried again" : "it is given up");
            }
        }

        await queue.UpdateAsync(item, cancellationToken).ConfigureAwait(false);
    }

    private async Task<string?> EndpointAsync(EnvelopeQueueItem item, CancellationToken cancellationToken)
    {
        if (item.ReceiverParticipantId is not { } receiverId || item.ReceiverAlias is null)
        {
            return null;
        }

        Participant? receiver = await participants.FindAsync(receiverId, cancellationToken).ConfigureAwait(false);
        return receiver?.FindAlias(item.ReceiverAlias)?.Endpoint;
    }
}
