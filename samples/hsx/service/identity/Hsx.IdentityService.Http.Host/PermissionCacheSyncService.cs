using HsnSoft.Base.Hosting;
using Hsx.IdentityService.Application.Services;

namespace Hsx.IdentityService.Http.Host;

/// <summary>
/// Writes the grants of every role to the store the services read, when the service starts and then every <c>Permissions:SyncMinutes</c> (5). A change is written at once by the endpoint that made it; this makes right a store that was emptied or restarted,
/// or a write that failed, and fills it the first time.
/// </summary>
public sealed partial class PermissionCacheSyncService(IServiceScopeFactory scopes, IConfiguration configuration, ILogger<PermissionCacheSyncService> logger)
    : PeriodicBackgroundService(TimeSpan.FromMinutes(Math.Max(1, configuration.GetValue("Permissions:SyncMinutes", 5))), logger)
{
    protected override async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        AsyncServiceScope scope = scopes.CreateAsyncScope();
        await using (scope.ConfigureAwait(false))
        {
            await scope.ServiceProvider.GetRequiredService<PermissionCacheSynchronizer>().SyncAllAsync(cancellationToken).ConfigureAwait(false);
        }

        LogSynced(logger);
    }

    [LoggerMessage(EventId = 6101, EventName = "PERMISSION_STORE_SYNCED", Level = LogLevel.Information, Message = "The grants of every role were written to the permission store")]
    private static partial void LogSynced(ILogger logger);
}
