using Edx.Integration.GibInvoice.Application.Options;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;
using Edx.Shared.Efatura.Client;
using Edx.Shared.Efatura.Envelope;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Edx.Integration.GibInvoice.Application.Services;

/// <summary>
/// A sender unit may ask the center what became of an envelope (<c>getApplicationResponse</c>), and does when it has heard nothing for a while: the center sends its system responses as it goes, but a system response that was lost, or an envelope the center is still waiting on, is found out
/// this way. The answer is a system envelope with the status code of the envelope in the center; it is applied as any news of the center is.
/// </summary>
public sealed class OutboundStatusPoller(IOutboundEnvelopeRepository envelopes, OutboundStatusService status, TimeProvider time, IOptions<GibCenterOptions> options, ILogger<OutboundStatusPoller> logger)
{
    public async Task PollAsync(OutboundEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        using EFaturaClient client = new(new Uri(options.Value.Endpoint), options.Value.MaxMessageSize);
        EFaturaResult result = await client.QueryAsync(envelope.EnvelopeId).ConfigureAwait(false);
        if (!result.Accepted || string.IsNullOrEmpty(result.ApplicationResponse))
        {
            logger.LogInformation("The center has no news of envelope {EnvelopeId} ({Code} {Message})", envelope.EnvelopeId, result.Code, result.Message);
            await Touch(envelope, cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            SystemResponse response = SystemResponse.FromElement(EnvelopeReader.ReadXml(System.Text.Encoding.UTF8.GetBytes(result.ApplicationResponse)).Elements[0]);
            if (response.ReferenceEnvelopeId != envelope.EnvelopeId)
            {
                throw new EnvelopeException(Edx.Shared.Efatura.EfaturaStatus.DocumentNotParsed);
            }

            await status.ApplyCenterStatusAsync(envelope, response.ResultCode, response.ResultDescription, cancellationToken).ConfigureAwait(false);
            if (envelope.State == OutboundState.Sent)
            {
                await Touch(envelope, cancellationToken).ConfigureAwait(false);
            }
        }
        catch (EnvelopeException exception)
        {
            logger.LogWarning("The answer of the center about envelope {EnvelopeId} is not a system response ({Code})", envelope.EnvelopeId, exception.Code);
            await Touch(envelope, cancellationToken).ConfigureAwait(false);
        }
    }

    // nothing new: it is asked again after the same wait
    private async Task Touch(OutboundEnvelope envelope, CancellationToken cancellationToken)
    {
        envelope.Polled(time.GetUtcNow());
        await envelopes.UpdateAsync(envelope, cancellationToken).ConfigureAwait(false);
    }
}
