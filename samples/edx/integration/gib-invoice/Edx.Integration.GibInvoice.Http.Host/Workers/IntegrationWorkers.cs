using Edx.Integration.GibInvoice.Application.Options;
using Edx.Integration.GibInvoice.Application.Services;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Entities;
using Edx.Integration.GibInvoice.Domain.EnvelopeDomain.Repositories;
using HsnSoft.Base.Hosting;
using HsnSoft.Base.MultiTenancy;
using Microsoft.Extensions.Options;

namespace Edx.Integration.GibInvoice.Http.Host.Workers;

/// <summary>
/// What every worker of the service does the same way: it works in a scope of its own, as the system tenant (it takes the envelopes of every tenant; a worker has no request to take the tenant from), and leaves it to the services it calls to act as the tenant of an envelope when they say something
/// to the rest of the system.
/// </summary>
public abstract class SystemWorker(IServiceScopeFactory scopes, TimeSpan interval, ILogger logger) : PeriodicBackgroundService(interval, logger)
{
    protected sealed override async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopes.CreateScope();
        using IDisposable system = scope.ServiceProvider.GetRequiredService<ICurrentTenant>().Change(null, isSystemTenant: true);
        await WorkAsync(scope.ServiceProvider, cancellationToken).ConfigureAwait(false);
    }

    protected abstract Task WorkAsync(IServiceProvider services, CancellationToken cancellationToken);
}

/// <summary>Hands the envelopes that wait to the center, the oldest first (<see cref="OutboundSender"/>).</summary>
public sealed class OutboundSendWorker(IServiceScopeFactory scopes, IOptions<GibCenterOptions> options, ILogger<OutboundSendWorker> logger)
    : SystemWorker(scopes, TimeSpan.FromSeconds(options.Value.WorkerIntervalSeconds), logger)
{
    protected override async Task WorkAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        IOutboundEnvelopeRepository envelopes = services.GetRequiredService<IOutboundEnvelopeRepository>();
        OutboundSender sender = services.GetRequiredService<OutboundSender>();
        foreach (OutboundEnvelope envelope in await envelopes.GetDueToSendAsync(DateTimeOffset.UtcNow, options.Value.WorkerBatchSize, cancellationToken).ConfigureAwait(false))
        {
            await sender.SendAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>Asks the center what became of the envelopes it took and has said nothing of for a while (<see cref="OutboundStatusPoller"/>).</summary>
public sealed class OutboundStatusWorker(IServiceScopeFactory scopes, IOptions<GibCenterOptions> options, ILogger<OutboundStatusWorker> logger)
    : SystemWorker(scopes, TimeSpan.FromSeconds(options.Value.WorkerIntervalSeconds), logger)
{
    protected override async Task WorkAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        IOutboundEnvelopeRepository envelopes = services.GetRequiredService<IOutboundEnvelopeRepository>();
        OutboundStatusPoller poller = services.GetRequiredService<OutboundStatusPoller>();
        DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-options.Value.StatusPollAfterSeconds);
        foreach (OutboundEnvelope envelope in await envelopes.GetAwaitingStatusAsync(before, options.Value.WorkerBatchSize, cancellationToken).ConfigureAwait(false))
        {
            await poller.PollAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>Checks the envelopes the center delivered to the post box, and acts on them (<see cref="InboundProcessor"/>).</summary>
/// <summary>Asks the center what became of the invoice envelopes this post box took, until it ends them (<see cref="InboundStatusPoller"/>).</summary>
public sealed class InboundStatusWorker(IServiceScopeFactory scopes, IOptions<GibCenterOptions> options, ILogger<InboundStatusWorker> logger)
    : SystemWorker(scopes, TimeSpan.FromSeconds(options.Value.WorkerIntervalSeconds), logger)
{
    protected override async Task WorkAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        IInboundEnvelopeRepository envelopes = services.GetRequiredService<IInboundEnvelopeRepository>();
        InboundStatusPoller poller = services.GetRequiredService<InboundStatusPoller>();
        DateTimeOffset before = DateTimeOffset.UtcNow.AddSeconds(-options.Value.StatusPollAfterSeconds);
        foreach (InboundEnvelope envelope in await envelopes.GetAwaitingCenterStatusAsync(before, options.Value.WorkerBatchSize, cancellationToken).ConfigureAwait(false))
        {
            await poller.PollAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
    }
}

public sealed class InboundProcessWorker(IServiceScopeFactory scopes, IOptions<GibCenterOptions> options, ILogger<InboundProcessWorker> logger)
    : SystemWorker(scopes, TimeSpan.FromSeconds(options.Value.WorkerIntervalSeconds), logger)
{
    protected override async Task WorkAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        IInboundEnvelopeRepository envelopes = services.GetRequiredService<IInboundEnvelopeRepository>();
        InboundProcessor processor = services.GetRequiredService<InboundProcessor>();
        foreach (InboundEnvelope envelope in await envelopes.GetQueuedAsync(options.Value.WorkerBatchSize, cancellationToken).ConfigureAwait(false))
        {
            await processor.ProcessAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>
/// Reads the list of the registered users from the center (<see cref="UserDirectory"/>), at the start and then as often as the settings say. It looks every half minute whether it is time, so a center that is not up yet (or is down) is tried again soon and not after a whole refresh period.
/// </summary>
public sealed class UserDirectoryWorker(IServiceScopeFactory scopes, IOptions<GibCenterOptions> options, TimeProvider time, ILogger<UserDirectoryWorker> logger)
    : SystemWorker(scopes, TimeSpan.FromSeconds(30), logger)
{
    private DateTimeOffset _lastRead = DateTimeOffset.MinValue;

    protected override async Task WorkAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        if (time.GetUtcNow() - _lastRead < TimeSpan.FromMinutes(options.Value.UserListRefreshMinutes))
        {
            return;
        }

        await services.GetRequiredService<UserDirectory>().SyncAsync(cancellationToken).ConfigureAwait(false);
        _lastRead = time.GetUtcNow();
    }
}
