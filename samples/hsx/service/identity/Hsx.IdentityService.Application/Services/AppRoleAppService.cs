using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Services;
using Hsx.IdentityService.Domain.AppRoleDomain.Consts;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleDomain.Exceptions;
using Hsx.IdentityService.Domain.AppRoleDomain.Repositories;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Repositories;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;

namespace Hsx.IdentityService.Application.Services;

/// <summary>
/// The roles of a tenant. A role is made with its starting grants and its subscriptions in one unit of work (either all of it or none); the store the services read is written after the unit has ended, and every start of the host writes it again. A role that is
/// static or has users is not deleted.
/// </summary>
public sealed class AppRoleAppService(
    IAppRoleRepository roles,
    IAppRoleSubscriptionRepository roleSubscriptions,
    IRepository<AppUserRole, Guid> userRoles,
    IRepository<Subscription, Guid> subscriptions,
    IAppRolePermissionRepository rolePermissions,
    IAppRolePermissionConstraintRepository roleConstraints,
    SubscriptionAppService subscriptionService,
    RoleStartingGrants startingGrants,
    PermissionCacheSynchronizer synchronizer,
    IUnitOfWork unitOfWork,
    IServiceProvider services) : ApplicationServiceBase(services), IAppRoleAppService
{
    public async Task<AppRoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await roles.FindAsync<AppRoleDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw AppRoleNotFound.For(id);

    public async Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync(GetAppRolesPagedDto pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);
        PagedQueryResult<AppRoleDto> page = await roles.GetPagedListAsync<AppRoleDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.Name), AppRoleSorting.Fields, AppRoleSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<AppRoleDto> CreateAsync(AppRoleCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Guid tenantId = CurrentTenant.Id ?? throw AppRoleNotFound.For(Guid.Empty);
        string name = input.Name!.Trim();
        if (await roles.NameExistsAsync(tenantId, StringHelper.Normalize(name), null, cancellationToken).ConfigureAwait(false))
        {
            throw new AppRoleNameDuplicateException(name);
        }

        AppRoleType type = input.RoleType.GetValueOrDefault();
        // the roles of the system tenant are System roles and the roles of a reseller are not: the type says which data, and the system tenant sees all of it already
        if (CurrentTenant.IsSystemTenant != (type == AppRoleType.System))
        {
            throw new AppRoleTypeInvalidException(CurrentTenant.IsSystemTenant ? "The roles of the system tenant are System roles." : "A reseller has no System role.");
        }

        // a reseller names only a company or a customer that is in its own subscriptions (it knows no other): the subscriptions are isolated by their tenant
        if (type == AppRoleType.Company && !await subscriptions.AnyAsync(subscription => subscription.CompanyId == input.CompanyId, ignoreFilters: false, cancellationToken).ConfigureAwait(false))
        {
            throw new AppRoleTypeInvalidException("The company is not in a subscription of the tenant.");
        }

        if (type == AppRoleType.Customer && !await subscriptions.AnyAsync(subscription => subscription.CustomerId == input.CustomerId, ignoreFilters: false, cancellationToken).ConfigureAwait(false))
        {
            throw new AppRoleTypeInvalidException("The customer is not in a subscription of the tenant.");
        }

        AppRole created = AppRole.Create(name, type, input.CompanyId, input.CustomerId);
        await unitOfWork.ExecuteAsync(async token =>
        {
            await roles.AddAsync(created, token).ConfigureAwait(false);
            await startingGrants.GrantAsync(created, token).ConfigureAwait(false);          // a Company or Customer role reads the data of its products from the start; any other starts with nothing
            await subscriptionService.LinkRoleAsync(created.Id, token).ConfigureAwait(false);   // the sales of the tenant that its type matches
            return true;
        }, cancellationToken).ConfigureAwait(false);

        await synchronizer.SyncRoleAsync(created, cancellationToken).ConfigureAwait(false);   // so the role is known to the store, with its grants
        return await GetAsync(created.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppRoleDto> UpdateAsync(AppRoleUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        AppRole role = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        string name = input.Name!.Trim();
        if (await roles.NameExistsAsync(role.TenantId, StringHelper.Normalize(name), role.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new AppRoleNameDuplicateException(name);
        }

        string oldName = role.Name;
        role.Update(name);
        await roles.UpdateAsync(role, cancellationToken).ConfigureAwait(false);

        // the grants are found by the name of the role: the entry under the old name goes and the one under the new name is written
        if (!string.Equals(oldName, role.Name, StringComparison.Ordinal))
        {
            await synchronizer.RemoveRoleAsync(role.TenantId, oldName, cancellationToken).ConfigureAwait(false);
        }

        await synchronizer.SyncRoleAsync(role, cancellationToken).ConfigureAwait(false);
        return await GetAsync(role.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppRoleSubscriptionsDto> GetSubscriptionsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        AppRole role = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        return new AppRoleSubscriptionsDto { RoleId = role.Id, SubscriptionIds = [.. await roleSubscriptions.GetSubscriptionIdsOfRoleAsync(role.Id, cancellationToken).ConfigureAwait(false)] };
    }

    public async Task<AppRoleDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        AppRole role = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        Guid roleId = role.Id;
        if (role.IsStatic || await userRoles.AnyAsync(link => link.RoleId == roleId, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new AppRoleNotDeletableException(role.Name);
        }

        AppRoleDto deleted = await GetAsync(roleId, cancellationToken).ConfigureAwait(false);
        await unitOfWork.ExecuteAsync(async token =>
        {
            await roleSubscriptions.DeleteWhereAsync(link => link.RoleId == roleId, token).ConfigureAwait(false);
            await rolePermissions.DeleteWhereAsync(grant => grant.RoleId == roleId, token).ConfigureAwait(false);
            await roleConstraints.DeleteWhereAsync(grant => grant.RoleId == roleId, token).ConfigureAwait(false);
            await roles.DeleteAsync(role, token).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);

        await synchronizer.RemoveRoleAsync(role.TenantId, role.Name, cancellationToken).ConfigureAwait(false);
        return deleted;
    }

    private async Task<AppRole> FindAsync(Guid id, CancellationToken cancellationToken)
        => await roles.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw AppRoleNotFound.For(id);

    private static Expression<Func<AppRole, bool>> Filter(string? searchText, string? name)
    {
        string? search = Normalized(searchText);
        string? nameText = Normalized(name);
        return new FilterBuilder<AppRole>()
            .And(search is null ? null : role => role.NormalizedName.Contains(search))
            .And(nameText is null ? null : role => role.NormalizedName.Contains(nameText))
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
