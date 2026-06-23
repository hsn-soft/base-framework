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

public class SynchServicePermissionConstraintStoreBackgroundService
    : BaseSingleThreadBackgroundService<SynchServicePermissionConstraintStoreBackgroundService>, IBaseThreadBackgroundService
{
    private readonly IServiceScopeFactory _serviceScopeFactory;
    private readonly CancellationTokenSource _tokenSource;

    public SynchServicePermissionConstraintStoreBackgroundService(IFrameworkLogger logger,
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
        var cachePermissionConstraintAssignmentRepository = scope.ServiceProvider.GetRequiredService<ICachePermissionConstraintAssignmentRepository>();
        var permissionConstraintStore = scope.ServiceProvider.GetRequiredService<IPermissionConstraintStore>();
        var dataFilter = scope.ServiceProvider.GetRequiredService<IDataFilter>();

        using (dataFilter.Disable<IMultiTenant>()) // disable tenant filter
        {
            using (dataFilter.Disable<IScopeSubscription>()) // disable scope key filter
            {
                // get microservice endpoint permission keys
                var servicePermissionConstraintKeys = await servicePermissionProvider.GetPermissionConstraintKeysAsync();
                servicePermissionConstraintKeys ??= [];

                // get user or role permissions equal microservice permission keys
                var cachePermissionConstraints = await cachePermissionConstraintAssignmentRepository.GetPermissionsAsync(servicePermissionConstraintKeys);
                cachePermissionConstraints ??= [];

                await permissionConstraintStore.SetAllConstraints(cachePermissionConstraints.Select(x
                    => new PermissionConstraintAssignment
                    {
                        Constraint = x.Constraint, ProviderName = x.ProviderName, ProviderKey = x.ProviderKey, Value = x.Value,
                    }));

                Logger.LogInformation("{WorkerName} | PermissionConstraint store successfully updated [{CachePermissionsCount}]", nameof(SynchServicePermissionConstraintStoreBackgroundService), cachePermissionConstraints.Count);
            }
        }
    }

    private void TriggerOperation(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            if (BackgroundServiceFlags.SkipWaitPeriodForSynchPermissionServiceStore)
            {
                Logger.LogWarning("{WorkerName} | Skip Wait Period", nameof(SynchServicePermissionConstraintStoreBackgroundService));
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