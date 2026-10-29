using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Minio;

namespace HsnSoft.Base.BlobStoring.Minio;

internal sealed class MinioHealthCheck(IMinioClient client) : IHealthCheck
{
    private readonly IMinioClient _client = client;

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await _client.ListBucketsAsync(cancellationToken).ConfigureAwait(false);
        return HealthCheckResult.Healthy();
    }
}
