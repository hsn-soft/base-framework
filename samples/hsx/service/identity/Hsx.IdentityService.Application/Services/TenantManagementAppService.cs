using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantManagementDomain.Services;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Exceptions;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Exceptions;
using Hsx.IdentityService.Domain.TenantDomain.Repositories;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Services;

public sealed class TenantManagementAppService(
    ITenantProvisioner provisioner,
    IRepository<Tenant, Guid> tenants,
    IRepository<AppUser, Guid> users,
    PermissionCacheSynchronizer synchronizer,
    IServiceProvider services) : ApplicationServiceBase(services), ITenantManagementAppService
{
    public async Task<ProvisionedTenantDto> CreateResellerAsync(CreateResellerDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        EnsureSystemTenant();

        string name = input.Name!.Trim().ToLowerInvariant();
        await EnsureTenantNameFreeAsync(name, cancellationToken).ConfigureAwait(false);
        AdminUserSpec? admin = await AdminAsync(input.AdminUser, cancellationToken).ConfigureAwait(false);

        ProvisionedTenant made = await provisioner.CreateResellerAsync(input.Title!.Trim(), name, admin, cancellationToken).ConfigureAwait(false);
        return await ToDtoAsync(made, cancellationToken).ConfigureAwait(false);
    }

    // a tenant name is taken by a deleted tenant too: the paths of the tree name it
    private async Task EnsureTenantNameFreeAsync(string name, CancellationToken cancellationToken)
    {
        string normalized = StringHelper.Normalize(name);
        if (await tenants.AnyAsync(tenant => tenant.NormalizedName == normalized, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new TenantNameDuplicateException(name);
        }
    }

    // the first administrator is checked here, before anything is written: its email is the sign-in of the whole system, and the password keeps the rules a new tenant starts with
    private async Task<AdminUserSpec?> AdminAsync(AdminUserInputDto? input, CancellationToken cancellationToken)
    {
        if (input is null)
        {
            return null;
        }

        string email = input.Email!.Trim();
        string normalizedEmail = StringHelper.Normalize(email);
        if (await users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new AppUserEmailDuplicateException(email);
        }

        IReadOnlyList<string> broken = PasswordRules.Default.Check(input.Password!);
        return broken.Count > 0
            ? throw new PasswordPolicyViolationException(broken)
            : new AdminUserSpec(input.UserName!.Trim(), email, input.DisplayName!.Trim(), input.Password!, input.LanguageCode);
    }

    private async Task<ProvisionedTenantDto> ToDtoAsync(ProvisionedTenant made, CancellationToken cancellationToken)
    {
        // the store the services read learns the administrator role of the new tenant at once
        await synchronizer.SyncRoleAsync(made.RoleId, made.TenantId, HsxRoles.Admin, cancellationToken).ConfigureAwait(false);
        return new ProvisionedTenantDto { TenantId = made.TenantId, RoleId = made.RoleId, UserId = made.UserId };
    }
}
