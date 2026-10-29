using System.Linq.Expressions;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using HsnSoft.Base.Application;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Reflection;

namespace Edx.IdentityService.Application.Services;

/// <summary>The tenants. A tenant is what the tenants are, so no tenant filter is on it: this service is what holds a customer to its own row.</summary>
public sealed class TenantAppService(IRepository<Tenant, Guid> tenants, ICurrentTenant currentTenant, IServiceProvider services) : ApplicationService(services), ITenantAppService
{
    public async Task<List<TenantDto>> GetFilterListAsync(GetTenantsFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        IReadOnlyList<Tenant> found = await tenants.GetListAsync(filterInput.ToListOptions(Filter(filterInput), IdentitySorting.Tenants, IdentitySorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. found.Select(tenant => new TenantDto { Id = tenant.Id, Code = tenant.Code, Name = tenant.Name, IsActive = tenant.IsActive })];
    }

    // a user of the system tenant sees every tenant; any other sees the one it belongs to
    private Expression<Func<Tenant, bool>> Filter(GetTenantsFilter input)
    {
        Guid? own = currentTenant.IsSystemTenant ? null : currentTenant.Id ?? Guid.Empty;
        string? search = string.IsNullOrWhiteSpace(input.SearchText) ? null : input.SearchText.Trim().ToLowerInvariant();
        return new FilterBuilder<Tenant>()
            .And(own is null ? null : tenant => tenant.Id == own.Value)
            .And(search is null ? null : tenant => tenant.Code.Contains(search))
            .Build();
    }
}
