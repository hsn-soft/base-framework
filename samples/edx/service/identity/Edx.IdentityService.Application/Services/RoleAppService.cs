using System.Linq.Expressions;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using HsnSoft.Base.Application;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;

namespace Edx.IdentityService.Application.Services;

public sealed class RoleAppService(IRepository<Role, Guid> roles, IServiceProvider services) : ApplicationService(services), IRoleAppService
{
    public async Task<List<RoleDto>> GetFilterListAsync(GetRolesFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        IReadOnlyList<Role> found = await roles.GetListAsync(filterInput.ToListOptions(Filter(filterInput), IdentitySorting.Roles, IdentitySorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. found.Select(role => new RoleDto { Id = role.Id, Name = role.Name, TenantId = role.TenantId, CreatedAt = role.CreatedAt, CreatedBy = role.CreatedBy })];
    }

    private static Expression<Func<Role, bool>> Filter(GetRolesFilter input)
    {
        string? name = string.IsNullOrWhiteSpace(input.Name) ? null : StringHelper.Normalize(input.Name);
        return new FilterBuilder<Role>().And(name is null ? null : role => role.NormalizedName.Contains(name)).Build();
    }
}
