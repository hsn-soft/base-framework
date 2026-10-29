using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;

namespace HsnSoft.Base.EventBus.Kafka;

internal sealed class KafkaHealthCheck(IOptions<KafkaOptions> options) : IHealthCheck
{
    private readonly KafkaOptions _options = options.Value;

    public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            using IAdminClient admin = new AdminClientBuilder(_options.Apply(new AdminClientConfig())).Build();
            admin.GetMetadata(TimeSpan.FromSeconds(3));
            return Task.FromResult(HealthCheckResult.Healthy());
        }
        catch (KafkaException exception)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy("cannot reach the brokers", exception));
        }
    }
}
