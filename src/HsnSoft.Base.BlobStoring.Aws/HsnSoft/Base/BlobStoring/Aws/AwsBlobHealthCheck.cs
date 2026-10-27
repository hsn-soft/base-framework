using System.Threading;
using System.Threading.Tasks;
using Amazon.S3;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HsnSoft.Base.BlobStoring.Aws;

internal sealed class AwsBlobHealthCheck(IAmazonS3 client) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await client.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
        return HealthCheckResult.Healthy();
    }
}
