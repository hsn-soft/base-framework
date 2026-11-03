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
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleDomain.Exceptions;
using Hsx.IdentityService.Domain.AppRoleDomain.Repositories;
using Hsx.IdentityService.Domain.AppRoleSubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;
using Hsx.IdentityService.Domain.SubscriptionDomain.Entities;
using Hsx.IdentityService.Domain.SubscriptionDomain.Repositories;

namespace Hsx.IdentityService.Application.Services;

public sealed class AppRoleAppService(
    IRepository<AppRole, Guid> roles,
    IRepository<AppRoleSubscription, Guid> roleSubscriptions,
    IRepository<AppUserRole, Guid> userRoles,
    IRepository<Subscription, Guid> subscriptions,
    ISubscriptionManager subscriptionManager,
    IPermissionGrantRepository grants,
    PermissionCacheSynchronizer synchronizer,
    IServiceProvider services) : ApplicationServiceBase(services), IAppRoleAppService
{
    public async Task<AppRoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await roles.FindAsync<AppRoleDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw AppRoleNotFound.For(id);

    public async Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync(GetAppRolesPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<AppRoleDto> page = await roles.GetPagedListAsync<AppRoleDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.Name), AppRoleSorting.Fields, AppRoleSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<AppRoleDto> CreateAsync(AppRoleCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        string name = input.Name!.Trim();
        string normalized = StringHelper.Normalize(name);
        Guid? tenantId = CurrentTenant.Id;
        if (await roles.AnyAsync(role => role.NormalizedName == normalized && role.TenantId == tenantId, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
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
        await roles.AddAsync(created, cancellationToken).ConfigureAwait(false);
        await grants.GrantStartingSetAsync(created, cancellationToken).ConfigureAwait(false);   // a Company or Customer role reads the data of its products from the start; any other starts with nothing
        await synchronizer.SyncRoleAsync(created, cancellationToken).ConfigureAwait(false);   // so the role is known to the store, with its grants
        await subscriptionManager.LinkRoleAsync(created.Id, cancellationToken).ConfigureAwait(false);   // the sales of the tenant that its type matches
        return await GetAsync(created.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(AppRoleUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        AppRole role = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        string name = input.Name!.Trim();
        string normalized = StringHelper.Normalize(name);
        Guid id = role.Id;
        Guid tenantId = role.TenantId;
        if (await roles.AnyAsync(other => other.NormalizedName == normalized && other.TenantId == tenantId && other.Id != id, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
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
    }

    public async Task<List<Guid>> GetSubscriptionIdsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        AppRole role = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        Guid roleId = role.Id;
        return [.. (await roleSubscriptions.GetListAsync(new ListQueryOptions<AppRoleSubscription> { Filter = link => link.RoleId == roleId }, cancellationToken).ConfigureAwait(false)).Select(link => link.SubscriptionId)];
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        AppRole role = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        Guid roleId = role.Id;
        if (role.IsStatic || await userRoles.AnyAsync(link => link.RoleId == roleId, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new AppRoleNotDeletableException(role.Name);
        }

        await roleSubscriptions.DeleteWhereAsync(link => link.RoleId == roleId, cancellationToken).ConfigureAwait(false);
        await roles.DeleteAsync(role, cancellationToken).ConfigureAwait(false);
        await synchronizer.RemoveRoleAsync(role.TenantId, role.Name, cancellationToken).ConfigureAwait(false);
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
