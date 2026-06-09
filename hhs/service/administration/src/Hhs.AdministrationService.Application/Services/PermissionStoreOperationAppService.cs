using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using Hhs.AdministrationService.Domain.PermissionDomain.Repositories;
using Hhs.Shared.Contracts.Cache;
using Hhs.Shared.Contracts.Events;
using HsnSoft.Base.Authorization.Permissions.ValueProviders;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.AdministrationService.Application.Services;

public sealed class PermissionStoreOperationAppService(
    IServiceProvider provider,
    IAppRolePermissionRepository appRolePermissionRepository,
    IAppRolePermissionConstraintRepository appRolePermissionConstraintRepository,
    ICachePermissionAssignmentRepository cachePermissionAssignmentRepository,
    ICachePermissionConstraintAssignmentRepository cachePermissionConstraintAssignmentRepository
) : ApplicationServiceBase(provider), IPermissionStoreOperationAppService
{
    private readonly IFrameworkLogger _logger = provider.GetRequiredService<IFrameworkLogger>();

    private readonly IAppRolePermissionRepository _appRolePermissionRepository = appRolePermissionRepository ?? throw new ArgumentNullException(nameof(appRolePermissionRepository));
    private readonly IAppRolePermissionConstraintRepository _appRolePermissionConstraintRepository = appRolePermissionConstraintRepository ?? throw new ArgumentNullException(nameof(appRolePermissionConstraintRepository));
    private readonly ICachePermissionAssignmentRepository _cachePermissionAssignmentRepository = cachePermissionAssignmentRepository ?? throw new ArgumentNullException(nameof(cachePermissionAssignmentRepository));
    private readonly ICachePermissionConstraintAssignmentRepository _cachePermissionConstraintAssignmentRepository = cachePermissionConstraintAssignmentRepository ?? throw new ArgumentNullException(nameof(cachePermissionConstraintAssignmentRepository));

    public async Task SynchAllPermissionToCacheDbAsync()
    {
        #region AppRolePermissions to CacheDb

        var appRolePermissions = await _appRolePermissionRepository.GetListAsync(
            new ListQueryOptions<AppRolePermission> { IncludeEntity = q => q.Include(x => x.Permission) }
        );
        if (appRolePermissions is { Count: > 0 })
        {
            await _cachePermissionAssignmentRepository
                .SetPermissionsAsync(
                    appRolePermissions
                        .Select(x => new CachePermissionAssignment
                            (
                                permission: x.Permission.UniqueCode,
                                providerName: PermissionProviders.Role,
                                providerKey: x.AppRoleId.ToString("N")
                            )
                        ).ToList());
            _logger.LogInformation("PERMISSION CACHE DB | {OperationStatus} => [ {PermissionsCount} ]", "UPDATED", appRolePermissions.Count);
        }
        else
        {
            await _cachePermissionAssignmentRepository.ClearPermissionsAsync();
            _logger.LogInformation("PERMISSION CACHE DB | {OperationStatus}", "CLEARED");
        }

        #endregion

        #region AppRolePermissionConstraints to CacheDb

        var appRolePermissionConstraints = await _appRolePermissionConstraintRepository.GetListAsync(
            new ListQueryOptions<AppRolePermissionConstraint> { IncludeEntity = q => q.Include(x => x.Permission) }
        );
        if (appRolePermissionConstraints is { Count: > 0 })
        {
            await _cachePermissionConstraintAssignmentRepository
                .SetPermissionsAsync(
                    appRolePermissionConstraints
                        .Select(x => new CachePermissionConstraintAssignment
                            (
                                constraint: x.Permission.UniqueCode,
                                providerName: PermissionProviders.Role,
                                providerKey: x.AppRoleId.ToString("N"),
                                value: x.Value
                            )
                        ).ToList());
            _logger.LogInformation("PERMISSION CONSTRAINT CACHE DB | {OperationStatus} => [ {PermissionConstraintsCount} ]", "UPDATED", appRolePermissionConstraints.Count);
        }
        else
        {
            await _cachePermissionConstraintAssignmentRepository.ClearPermissionsAsync();
            _logger.LogInformation("PERMISSION CONSTRAINT CACHE DB | {OperationStatus}", "CLEARED");
        }

        #endregion

        // Publish Permission Grant Update Integration Event for other microservices skip it wait periods for synchronization
        await EventBus.PublishAsync(parentMessage: ParentIntegrationEvent, eventMessage: new CachePermissionGrantsChangedEto());
    }
}