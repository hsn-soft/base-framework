using Hhs.Shared.Contracts;
using Hhs.Shared.Contracts.Cache;
using HsnSoft.Base.AspNetCore.Hosting.Worker;
using HsnSoft.Base.Authorization.Permissions.Store;
using HsnSoft.Base.Data;
using HsnSoft.Base.Logging.Abstracts;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Subscribe;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Hhs.Shared.Hosting.Microservices.Workers;

public class SynchServicePermissionStoreBackgroundService
    : BaseSingleThreadBackgroundService<SynchServicePermissionStoreBackgroundService>, IBaseThreadBackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CancellationTokenSource _tokenSource;

    public SynchServicePermissionStoreBackgroundService(IFrameworkLogger logger,
        IServiceScopeFactory serviceScopeFactory,
        IOptions<MicroserviceHostingSettings> settings
    ) : base(logger, waitPeriodSeconds: settings?.Value.CachePermissionsUpdateSeconds ?? 3600, waitContinuousThread: false)
    {
        _serviceScopeFactory = serviceScopeFactory ?? throw new ArgumentNullException(nameof(serviceScopeFactory));

        _tokenSource = new CancellationTokenSource();
        var triggerFlagController = new Thread(() => TriggerOperation(_tokenSource.Token));
        triggerFlagController.Start();
    }

    public override async Task OperationAsync(CancellationToken cancellationToken)
    {
        await Task.Delay(50, cancellationToken);

        using var scope = _serviceScopeFactory.CreateScope();
        var servicePermissionProvider = scope.ServiceProvider.GetRequiredService<IServicePermissionProvider>();
        var cachePermissionAssignmentRepository = scope.ServiceProvider.GetRequiredService<ICachePermissionAssignmentRepository>();
        var permissionStore = scope.ServiceProvider.GetRequiredService<IPermissionStore>();
        var dataFilter = scope.ServiceProvider.GetRequiredService<IDataFilter>();

        using (dataFilter.Disable<IMultiTenant>()) // disable tenant filter
        {
            using (dataFilter.Disable<IScopeSubscription>()) // disable scope key filter
            {
                // get microservice endpoint permission keys
                var servicePermissionKeys = await servicePermissionProvider.GetPermissionKeysAsync();
                servicePermissionKeys ??= [];

                // get user or role permissions equal microservice permission keys
                var cachePermissions = await cachePermissionAssignmentRepository.GetPermissionsAsync(servicePermissionKeys);
                cachePermissions ??= [];

                await permissionStore.SetAllPermissions(cachePermissions.Select(x
                    => new PermissionAssignment { Permission = x.Permission, ProviderName = x.ProviderName, ProviderKey = x.ProviderKey }));

                Logger.LogInformation("{WorkerName} | Permission store successfully updated [{CachePermissionsCount}]", nameof(SynchServicePermissionStoreBackgroundService), cachePermissions.Count);
            }
        }
    }

    private void TriggerOperation(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (BackgroundServiceFlags.SkipWaitPeriodForSynchPermissionServiceStore)
            {
                Logger.LogWarning("{WorkerName} | Skip Wait Period", nameof(SynchServicePermissionStoreBackgroundService));
                BackgroundServiceFlags.SkipWaitPeriodForSynchPermissionServiceStore = false;

                // Skip operation wait period
                SkipOperationWaitPeriod();
            }

            Thread.Sleep(1000);
        }
    }

    public override void Dispose()
    {
        _tokenSource.Cancel();
        _tokenSource.Dispose();
        base.Dispose();
    }
}