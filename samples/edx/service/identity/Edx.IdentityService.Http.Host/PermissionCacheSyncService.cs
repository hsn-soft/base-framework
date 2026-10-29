using Edx.IdentityService.Application.Services;
using HsnSoft.Base.Hosting;

namespace Edx.IdentityService.Http.Host;

/// <summary>
/// Writes the grants of every role and user to the store the services read, when the service starts and then every <c>Permissions:SyncMinutes</c> (5). A change is written at once by the endpoint that made it; this is what makes it right
/// again after a cache that was emptied or restarted, or a write that failed, and what fills it the first time.
/// </summary>
public sealed class PermissionCacheSyncService(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<PermissionCacheSyncService> logger)
    : PeriodicBackgroundService(TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("Permissions:SyncMinutes", 5))), logger)
{
    protected override async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        AsyncServiceScope scope = scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            await scope.ServiceProvider.GetRequiredService<PermissionCacheSynchronizer>().SyncAllAsync(cancellationToken).ConfigureAwait(false);
        }

        logger.LogInformation("The grants of every role and user were written to the permission store");
    }
}
