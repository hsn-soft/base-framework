using Edx.Integration.GibInvoice.Application.Options;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;
using Edx.Shared.Contracts.Events.Integration;
using Edx.Shared.Efatura;
using Edx.Shared.Efatura.Client;
using Edx.Shared.Efatura.Envelope;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Edx.Integration.GibInvoice.Application.Services;

/// <summary>
/// Asks the center what became of an envelope that came in at this post box (with the uuid of the envelope, as the sender does for its own): the center says 1220 while it waits for our answer and 1300 once it has it. A newer status is kept on the envelope, with its history, and told to the invoice service
/// (<see cref="PurchaseEnvelopeStatusEto"/>) so the purchase invoices of the envelope show it. It runs for the tenant of the envelope.
/// </summary>
public sealed class InboundStatusPoller(IInboundEnvelopeRepository envelopes, IEventBus eventBus, ICurrentTenant tenant, TimeProvider time, IOptions<GibCenterOptions> options, ILogger<InboundStatusPoller> logger)
{
    public async Task PollAsync(InboundEnvelope envelope, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(envelope);

        DateTimeOffset now = time.GetUtcNow();
        using EFaturaClient client = new(new Uri(options.Value.Endpoint), options.Value.MaxMessageSize);
        EFaturaResult result = await client.QueryAsync(envelope.EnvelopeId).ConfigureAwait(false);
        if (!result.Accepted || string.IsNullOrEmpty(result.ApplicationResponse))
        {
            logger.LogInformation("The center has no news of envelope {EnvelopeId} ({Code} {Message})", envelope.EnvelopeId, result.Code, result.Message);
            envelope.CenterChecked(now);
            await envelopes.UpdateAsync(envelope, cancellationToken).ConfigureAwait(false);
            return;
        }

        try
        {
            SystemResponse response = SystemResponse.FromElement(EnvelopeReader.ReadXml(System.Text.Encoding.UTF8.GetBytes(result.ApplicationResponse)).Elements[0]);
            if (response.ReferenceEnvelopeId != envelope.EnvelopeId)
            {
                throw new EnvelopeException(EfaturaStatus.DocumentNotParsed);
            }

            bool news = envelope.ApplyCenterStatus(response.ResultCode, response.ResultDescription, now);
            await envelopes.UpdateAsync(envelope, cancellationToken).ConfigureAwait(false);
            if (news)
            {
                using IDisposable scope = tenant.Change(envelope.TenantId);
                await eventBus.PublishAsync(new PurchaseEnvelopeStatusEto(Guid.Parse(envelope.EnvelopeId), response.ResultCode, envelope.CenterStatusDescription ?? string.Empty, now), cancellationToken: cancellationToken).ConfigureAwait(false);
                logger.LogInformation("Envelope {EnvelopeId} is {StatusCode} at the center", envelope.EnvelopeId, response.ResultCode);
            }
        }
        catch (EnvelopeException exception)
        {
            logger.LogWarning("The answer of the center about envelope {EnvelopeId} is not a system response ({Code})", envelope.EnvelopeId, exception.Code);
            envelope.CenterChecked(now);
            await envelopes.UpdateAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
    }
}
