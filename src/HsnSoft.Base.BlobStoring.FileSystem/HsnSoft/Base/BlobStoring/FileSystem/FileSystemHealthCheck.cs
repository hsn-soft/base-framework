using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace HsnSoft.Base.BlobStoring.FileSystem;

/// <summary>Ready when the root is there and a file can be written in it and removed.</summary>
internal sealed class FileSystemHealthCheck(FileSystemBlobStore store) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        string probe = Path.Combine(store.RootPath, $"{BlobPaths.TemporaryPrefix}probe-{Guid.NewGuid():N}.tmp");
        try
        {
            await File.WriteAllBytesAsync(probe, [1], cancellationToken).ConfigureAwait(false);
            File.Delete(probe);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            return HealthCheckResult.Unhealthy("The root of the blob store cannot be written.", exception);
        }
    }
}
