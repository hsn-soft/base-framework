using HsnSoft.Base;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Services;
using Hsx.IdentityService.Domain.AppRoleDomain.Consts;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleDomain.Repositories;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Repositories;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Exceptions;
using Hsx.IdentityService.Domain.TenantDomain.Repositories;
using Hsx.Shared.Contracts.Events.Identity;

namespace Hsx.IdentityService.Application.Services;

/// <summary>
/// Makes a reseller: its tenant, the administrator role of that tenant with its starting grants (the permissions and the constraints with their values), and the first administrator with a temporary password the system makes. All of it is one unit of work, so
/// there is a reseller with its administrator or nothing; the event that tells the administrator its password is published after the unit has ended. A row of a tenant gets its tenant from the current tenant, so what belongs to the new tenant is saved inside a
/// scope of it.
/// </summary>
public sealed class TenantManagementAppService(
    ITenantRepository tenants,
    IAppRoleRepository roles,
    IAppUserRoleRepository userRoles,
    RoleStartingGrants startingGrants,
    NewUserFactory factory,
    ICurrentTenant currentTenant,
    IUnitOfWork unitOfWork,
    PermissionCacheSynchronizer synchronizer,
    IEventBus eventBus) : ITenantManagementAppService
{
    /// <summary>Makes a reseller, under no tenant, with its administrator role and its first administrator.</summary>
    /// <exception cref="BaseHttpException">The caller is not of the system tenant (403).</exception>
    /// <exception cref="TenantNameDuplicateException">The name is taken, by a deleted tenant too: the paths of the tree name it.</exception>
    public async Task<ProvisionedTenantDto> CreateResellerAsync(CreateResellerDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);
        if (!currentTenant.IsSystemTenant)
        {
            throw new BaseHttpException(403, "Only the system tenant makes a reseller.").WithKey("tenant_system_only");
        }

        string name = input.Name!.Trim().ToLowerInvariant();
        string normalizedName = StringHelper.Normalize(name);
        (ProvisionedTenantDto made, UserCreatedEto adminCreated) = await unitOfWork.ExecuteAsync(async token =>
        {
            if (await tenants.AnyAsync(tenant => tenant.NormalizedName == normalizedName, true, token).ConfigureAwait(false))
            {
                throw new TenantNameDuplicateException(name);
            }

            Tenant tenant = Tenant.Create(TenantTypes.Reseller, input.Title!.Trim(), name);
            await tenants.AddAsync(tenant, token).ConfigureAwait(false);
            using (currentTenant.Change(tenant.Id))
            {
                // the administrator of a reseller sees all of its sales: a Tenant role
                AppRole role = AppRole.Create(AppRoleConsts.TenantAdmin, AppRoleType.Tenant, isStatic: true);
                await roles.AddAsync(role, token).ConfigureAwait(false);
                await startingGrants.GrantAsync(role, token).ConfigureAwait(false);

                MadeUser user = await factory.MakeAsync(tenant.Id, new NewUserInput(input.AdminUser!.UserName!, input.AdminUser.DisplayName!, input.AdminUser.Email!, input.AdminUser.NotificationType, input.AdminUser.PhoneNumber, input.AdminUser.LanguageCode), token).ConfigureAwait(false);
                await userRoles.AddAsync(AppUserRole.Create(user.User.Id, role.Id), token).ConfigureAwait(false);
                return (new ProvisionedTenantDto { TenantId = tenant.Id, RoleId = role.Id, UserId = user.User.Id }, user.Event);
            }
        }, cancellationToken).ConfigureAwait(false);

        await synchronizer.SyncRoleAsync(made.RoleId, made.TenantId, AppRoleConsts.TenantAdmin, cancellationToken).ConfigureAwait(false);   // the store the services read learns the administrator role at once
        await eventBus.PublishAsync(adminCreated, cancellationToken: cancellationToken).ConfigureAwait(false);
        return made;
    }
}
