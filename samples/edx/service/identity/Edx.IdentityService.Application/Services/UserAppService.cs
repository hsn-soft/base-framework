using System.Linq.Expressions;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Submits;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Exceptions;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.Shared.Helper;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.MultiTenancy;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Microsoft.Extensions.Logging;

namespace Edx.IdentityService.Application.Services;

public sealed class UserAppService(IUserRepository users, IRepository<Role, Guid> roles, IPermissionChecker permissions, ICurrentTenant tenant, IServiceProvider services) : ApplicationService(services), IUserAppService
{
    // which tenant's users are read is not decided here: the tenant filter of the context does it (a customer's own, all for the system tenant)
    public async Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await ToDtoAsync(await FindAsync(id, cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    public async Task<PagedDataResultDto<UserDto>> GetPagedListAsync(GetUsersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<User> page = await users.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput), IdentitySorting.Users, IdentitySorting.Default), cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> roleNames = await users.GetRoleNamesAsync([.. page.Items.Select(user => user.Id)], cancellationToken).ConfigureAwait(false);
        bool email = await CanSeeEmailAsync(cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, user => ToDto(user, roleNames, email));
    }

    public async Task<UserDto> CreateAsync(UserCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Guid tenantId = tenant.Id ?? throw new InvalidOperationException("A user is made in a tenant: the request has none.");
        string normalized = StringHelper.Normalize(input.UserName);
        if (await users.AnyAsync(user => user.TenantId == tenantId && user.NormalizedUserName == normalized, ignoreFilters: false, cancellationToken).ConfigureAwait(false))
        {
            throw new UserNameDuplicateException(input.UserName.Trim());
        }

        List<Role> chosen = await RolesOfAsync(input.RoleIds, tenantId, cancellationToken).ConfigureAwait(false);
        User created = User.Create(input.UserName, input.Email, input.DisplayName, input.Password);
        await users.AddAsync(created, cancellationToken).ConfigureAwait(false);
        await users.SetRolesAsync(created, chosen, cancellationToken).ConfigureAwait(false);
        Logger.LogInformation("User {CreatedUserId} ({CreatedUserName}) was made with {RoleCount} roles", created.Id, created.UserName, chosen.Count);
        return await ToDtoAsync(created, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        User user = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        List<Role> chosen = await RolesOfAsync(input.RoleIds, user.TenantId, cancellationToken).ConfigureAwait(false);
        user.Update(input.Email, input.DisplayName, input.IsActive);
        await users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        await users.SetRolesAsync(user, chosen, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        User user = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (user.Id == CurrentUser.Id)
        {
            throw new UserDeletesItselfException();
        }

        await users.DeleteAsync(user, cancellationToken).ConfigureAwait(false);
    }

    private async Task<User> FindAsync(Guid id, CancellationToken cancellationToken)
        => await users.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw UserNotFound.For(id);

    // the roles a user is given have to be the roles of its own tenant (a caller of the system tenant sees every tenant's roles, and may not mix them)
    private async Task<List<Role>> RolesOfAsync(IEnumerable<Guid> roleIds, Guid tenantId, CancellationToken cancellationToken)
    {
        List<Role> found = [];
        foreach (Guid roleId in roleIds.Distinct())
        {
            Role role = await roles.FindAsync(roleId, cancellationToken).ConfigureAwait(false) ?? throw RoleNotFound.For(roleId);
            found.Add(role.TenantId == tenantId ? role : throw RoleNotFound.For(roleId));
        }

        return found;
    }

    private static Expression<Func<User, bool>> Filter(GetUsersPaged input)
    {
        string? name = Normalized(input.UserName);
        string? email = Normalized(input.Email);
        string? search = Normalized(input.SearchText);
        return new FilterBuilder<User>()
            .And(name is null ? null : user => user.NormalizedUserName.Contains(name))
            .And(email is null ? null : user => user.NormalizedEmail.Contains(email))
            .And(search is null ? null : user => user.NormalizedUserName.Contains(search) || user.NormalizedEmail.Contains(search))
            .And(input.IsActive is null ? null : user => user.IsActive == input.IsActive.Value)
            .Build();
    }

    // an email is personal: it is shown to who was granted the operation, and empty to everyone else
    private Task<bool> CanSeeEmailAsync(CancellationToken cancellationToken) => permissions.IsGrantedAsync(EdxOperationPermissions.Identity.UserEmailView, cancellationToken);

    private async Task<UserDto> ToDtoAsync(User user, CancellationToken cancellationToken)
    {
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> roleNames = await users.GetRoleNamesAsync([user.Id], cancellationToken).ConfigureAwait(false);
        return ToDto(user, roleNames, await CanSeeEmailAsync(cancellationToken).ConfigureAwait(false));
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static UserDto ToDto(User user, IReadOnlyDictionary<Guid, IReadOnlyList<string>> roles, bool canSeeEmail) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = canSeeEmail ? user.Email : string.Empty,
        DisplayName = user.DisplayName,
        IsActive = user.IsActive,
        Roles = roles.TryGetValue(user.Id, out IReadOnlyList<string>? names) ? names : [],
        TenantId = user.TenantId,
        CreatedAt = user.CreatedAt,
        CreatedBy = user.CreatedBy,
    };
}
