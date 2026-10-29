using Edx.Mock.Center.Domain.Envelopes;
using Edx.Mock.Center.Wcf.Host.Services;
using HsnSoft.Base.Hosting;
using Microsoft.Extensions.Options;

namespace Edx.Mock.Center.Wcf.Host.Workers;

/// <summary>Analyzes the envelopes that were queued (<see cref="EnvelopeAnalyzer"/>), the oldest first.</summary>
public sealed class AnalyzeWorker(IServiceScopeFactory scopes, IOptions<CenterOptions> options, ILogger<AnalyzeWorker> logger)
    : PeriodicBackgroundService(TimeSpan.FromSeconds(options.Value.WorkerIntervalSeconds), logger)
{
    protected override async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopes.CreateScope();
        IEnvelopeQueueRepository queue = scope.ServiceProvider.GetRequiredService<IEnvelopeQueueRepository>();
        EnvelopeAnalyzer analyzer = scope.ServiceProvider.GetRequiredService<EnvelopeAnalyzer>();
        foreach (EnvelopeQueueItem item in await queue.GetByStageAsync(EnvelopeStage.ToAnalyze, options.Value.WorkerBatchSize, cancellationToken).ConfigureAwait(false))
        {
            await analyzer.AnalyzeAsync(item, cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>Prepares what the analysis left: the system responses to the senders and the delivery of the envelopes that were taken (<see cref="EnvelopePreparer"/>).</summary>
public sealed class PrepareWorker(IServiceScopeFactory scopes, IOptions<CenterOptions> options, ILogger<PrepareWorker> logger)
    : PeriodicBackgroundService(TimeSpan.FromSeconds(options.Value.WorkerIntervalSeconds), logger)
{
    protected override async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopes.CreateScope();
        IEnvelopeQueueRepository queue = scope.ServiceProvider.GetRequiredService<IEnvelopeQueueRepository>();
        EnvelopePreparer preparer = scope.ServiceProvider.GetRequiredService<EnvelopePreparer>();
        foreach (EnvelopeQueueItem item in await queue.GetByStageAsync(EnvelopeStage.Analyzed, options.Value.WorkerBatchSize, cancellationToken).ConfigureAwait(false))
        {
            await preparer.PrepareAsync(item, cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>Delivers the envelopes whose time has come, and tries again those that could not be delivered (<see cref="EnvelopeDeliverer"/>).</summary>
public sealed class DeliveryWorker(IServiceScopeFactory scopes, IOptions<CenterOptions> options, ILogger<DeliveryWorker> logger)
    : PeriodicBackgroundService(TimeSpan.FromSeconds(options.Value.WorkerIntervalSeconds), logger)
{
    protected override async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopes.CreateScope();
        IEnvelopeQueueRepository queue = scope.ServiceProvider.GetRequiredService<IEnvelopeQueueRepository>();
        EnvelopeDeliverer deliverer = scope.ServiceProvider.GetRequiredService<EnvelopeDeliverer>();
        foreach (EnvelopeQueueItem item in await queue.GetDueForDeliveryAsync(DateTimeOffset.UtcNow, options.Value.WorkerBatchSize, cancellationToken).ConfigureAwait(false))
        {
            await deliverer.DeliverAsync(item, cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>Asks the post boxes about the envelopes they have not answered for a while (<see cref="AnswerPoller"/>).</summary>
public sealed class AnswerPollWorker(IServiceScopeFactory scopes, IOptions<CenterOptions> options, TimeProvider time, ILogger<AnswerPollWorker> logger)
    : PeriodicBackgroundService(TimeSpan.FromSeconds(options.Value.WorkerIntervalSeconds), logger)
{
    protected override async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        using IServiceScope scope = scopes.CreateScope();
        IEnvelopeQueueRepository queue = scope.ServiceProvider.GetRequiredService<IEnvelopeQueueRepository>();
        AnswerPoller poller = scope.ServiceProvider.GetRequiredService<AnswerPoller>();
        DateTimeOffset before = time.GetUtcNow().AddSeconds(-options.Value.AnswerPollAfterSeconds);
        foreach (EnvelopeQueueItem item in await queue.GetAwaitingAnswerAsync(before, options.Value.WorkerBatchSize, cancellationToken).ConfigureAwait(false))
        {
            await poller.PollAsync(item, cancellationToken).ConfigureAwait(false);
        }
    }
}
