using System.Linq.Expressions;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Submits;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Exceptions;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using HsnSoft.Base.Application;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Microsoft.Extensions.Logging;

namespace Edx.IdentityService.Application.Services;

public sealed class RoleAppService(IRepository<Role, Guid> roles, IUserRepository users, PermissionCacheSynchronizer synchronizer, ICurrentTenant tenant, IServiceProvider services)
    : ApplicationService(services), IRoleAppService
{
    public async Task<List<RoleDto>> GetFilterListAsync(GetRolesFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        IReadOnlyList<Role> found = await roles.GetListAsync(filterInput.ToListOptions(Filter(filterInput), IdentitySorting.Roles, IdentitySorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. found.Select(ToDto)];
    }

    public async Task<RoleDto> CreateAsync(RoleCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Guid tenantId = tenant.Id ?? throw new InvalidOperationException("A role is made in a tenant: the request has none.");
        string normalized = StringHelper.Normalize(input.Name);
        if (await roles.AnyAsync(role => role.TenantId == tenantId && role.NormalizedName == normalized, ignoreFilters: false, cancellationToken).ConfigureAwait(false))
        {
            throw new RoleNameDuplicateException(input.Name.Trim());
        }

        Role created = Role.Create(input.Name);
        await roles.AddAsync(created, cancellationToken).ConfigureAwait(false);

        // it has no grants yet: an entry with nothing, so a service finds the role known
        await synchronizer.SyncRoleAsync(created, cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("Role {CreatedRoleId} ({CreatedRoleName}) was made", created.Id, created.Name);
        return ToDto(created);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Role role = await roles.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw RoleNotFound.For(id);
        if (await users.AnyUserInRoleAsync(role.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new RoleInUseException(role.Name);
        }

        await roles.DeleteAsync(role, cancellationToken).ConfigureAwait(false);
        await synchronizer.RemoveRoleAsync(role, cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("Role {DeletedRoleId} ({DeletedRoleName}) was deleted", role.Id, role.Name);
    }

    private static Expression<Func<Role, bool>> Filter(GetRolesFilter input)
    {
        string? name = string.IsNullOrWhiteSpace(input.Name) ? null : StringHelper.Normalize(input.Name);
        return new FilterBuilder<Role>().And(name is null ? null : role => role.NormalizedName.Contains(name)).Build();
    }

    private static RoleDto ToDto(Role role) => new() { Id = role.Id, Name = role.Name, TenantId = role.TenantId, CreatedAt = role.CreatedAt, CreatedBy = role.CreatedBy };
}
