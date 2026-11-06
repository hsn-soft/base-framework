using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Kafka;

/// <summary>
/// Asks the brokers for their metadata with <b>one</b> admin client that lives as long as the host (a probe runs every few seconds on every pod: a client of its own for each would connect to the brokers each time, and the brokers would show a connection
/// per probe). The client reconnects by itself when a broker goes away, so a check made after the brokers came back is healthy again. The answer of the brokers is waited for on a thread of the pool, so a probe that is cancelled does not wait for it.
/// </summary>
internal sealed class KafkaHealthCheck(IOptions<KafkaOptions> options, TimeSpan? metadataTimeout = null) : IHealthCheck, IDisposable
{
    private static readonly TimeSpan s_defaultMetadataTimeout = TimeSpan.FromSeconds(3);

    private readonly TimeSpan _metadataTimeout = metadataTimeout ?? s_defaultMetadataTimeout;

    private readonly KafkaOptions _options = options.Value;
    private readonly Lock _lock = new();
    private IAdminClient? _admin;
    private bool _disposed;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            IAdminClient admin = Admin();
            await Task.Run(() => admin.GetMetadata(_metadataTimeout), CancellationToken.None).WaitAsync(cancellationToken).ConfigureAwait(false);
            return HealthCheckResult.Healthy();
        }
        catch (KafkaException exception)
        {
            return HealthCheckResult.Unhealthy("cannot reach the brokers", exception);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            _disposed = true;
            _admin?.Dispose();
            _admin = null;
        }
    }

    private IAdminClient Admin()
    {
        lock (_lock)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _admin ??= new AdminClientBuilder(_options.Apply(new AdminClientConfig())).Build();
        }
    }
}
