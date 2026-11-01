using System.Linq.Expressions;
using HsnSoft.Base;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.TenantDomain.Services;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Entities;
using Hsx.IdentityService.Domain.TenantDomain.Exceptions;
using Hsx.IdentityService.Domain.TenantDomain.Repositories;
using Hsx.Shared.Helper;

namespace Hsx.IdentityService.Application.Services;

public sealed class TenantAppService(IRepository<Tenant, Guid> tenants, ITenantRepository tree, IRepository<AppUser, Guid> users, IServiceProvider services) : ApplicationServiceBase(services), ITenantAppService
{
    public async Task<TenantDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Guid[] visible = Visible();
        return (CurrentTenant.IsSystemTenant || visible.Contains(id) ? await tenants.FindAsync<TenantDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) : null) ?? throw TenantNotFound.For(id);
    }

    public async Task<PagedDataResultDto<TenantDto>> GetPagedListAsync(GetTenantsPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<TenantDto> page = await tenants.GetPagedListAsync<TenantDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.Name, pagedInput.TenantType, pagedInput.ParentId), TenantSorting.Fields, TenantSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<TenantDto> CreateAsync(TenantCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Tenant? parent = null;
        if (input.ParentId is { } parentId)
        {
            parent = await FindVisibleAsync(parentId, cancellationToken).ConfigureAwait(false);
        }
        else if (!CurrentTenant.IsSystemTenant)
        {
            // only the system tenant makes a tenant at the root of the tree
            throw new BaseHttpException(403, "Only the system tenant makes a tenant at the root.").WithKey("tenant_root_forbidden");
        }

        string name = input.Name!.Trim().ToLowerInvariant();
        if (await tenants.AnyAsync(tenant => tenant.NormalizedName == StringHelper.Normalize(name), ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new TenantNameDuplicateException(name);
        }

        Tenant created = Tenant.Create(null, input.TenantType!.Value, input.Title!, name, parent);
        await tenants.AddAsync(created, cancellationToken).ConfigureAwait(false);
        return await GetAsync(created.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(TenantUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Tenant tenant = await FindVisibleAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        tenant.Update(input.Title!, input.IsActive);
        await tenants.UpdateAsync(tenant, cancellationToken).ConfigureAwait(false);
    }

    public async Task MoveAsync(TenantMoveDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Tenant tenant = await FindVisibleAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        Tenant? newParent = input.NewParentId is { } parentId ? await FindVisibleAsync(parentId, cancellationToken).ConfigureAwait(false) : null;
        if (newParent is null && !CurrentTenant.IsSystemTenant)
        {
            throw new BaseHttpException(403, "Only the system tenant puts a tenant at the root.").WithKey("tenant_root_forbidden");
        }

        await tree.MoveAsync(tenant, newParent, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Tenant tenant = await FindVisibleAsync(id, cancellationToken).ConfigureAwait(false);
        if (await tree.HasChildrenAsync(id, cancellationToken).ConfigureAwait(false) || await users.AnyAsync(user => user.TenantId == id, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new TenantNotEmptyException(id);
        }

        await tenants.DeleteAsync(tenant, cancellationToken).ConfigureAwait(false);
    }

    // the tenants of the caller's token (itself and the ones under it); the system tenant sees them all
    private Guid[] Visible() => [.. CurrentTenant.AllowedTenantIds];

    private async Task<Tenant> FindVisibleAsync(Guid id, CancellationToken cancellationToken)
        => (CurrentTenant.IsSystemTenant || Visible().Contains(id) ? await tenants.FindAsync(id, cancellationToken).ConfigureAwait(false) : null) ?? throw TenantNotFound.For(id);

    private Expression<Func<Tenant, bool>> Filter(string? searchText, string? name, TenantTypes? tenantType, Guid? parentId)
    {
        string? search = Normalized(searchText);
        string? nameText = Normalized(name);
        Guid[] visible = Visible();
        bool system = CurrentTenant.IsSystemTenant;
        return new FilterBuilder<Tenant>()
            .And(system ? null : tenant => visible.Contains(tenant.Id))
            .And(search is null ? null : tenant => tenant.NormalizedName.Contains(search) || tenant.Title.Contains(searchText!.Trim()))
            .And(nameText is null ? null : tenant => tenant.NormalizedName.Contains(nameText))
            .And(tenantType is null ? null : tenant => tenant.TenantType == tenantType.Value)
            .And(parentId is null ? null : tenant => tenant.ParentId == parentId.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
