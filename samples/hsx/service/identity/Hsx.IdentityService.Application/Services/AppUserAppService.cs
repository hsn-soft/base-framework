using System.Linq.Expressions;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleDomain.Exceptions;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Exceptions;
using Hsx.IdentityService.Domain.AppUserDomain.Repositories;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Repositories;

namespace Hsx.IdentityService.Application.Services;

public sealed class AppUserAppService(IRepository<AppUser, Guid> users, IRepository<AppUserRole, Guid> userRoles, IRepository<AppRole, Guid> roles, IPasswordPolicyProvider policies, IServiceProvider services) : ApplicationServiceBase(services), IAppUserAppService
{
    public async Task<AppUserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await users.FindAsync<AppUserDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw AppUserNotFound.For(id);

    public async Task<PagedDataResultDto<AppUserDto>> GetPagedListAsync(GetAppUsersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<AppUserDto> page = await users.GetPagedListAsync<AppUserDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.UserName, pagedInput.Email, pagedInput.IsActive), AppUserSorting.Fields, AppUserSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<AppUserDto> CreateAsync(AppUserCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        // the email signs in, so it is unique in the whole system, whichever tenant the other user is of
        string email = input.Email!.Trim();
        string normalizedEmail = StringHelper.Normalize(email);
        if (await users.AnyAsync(user => user.NormalizedEmail == normalizedEmail, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new AppUserEmailDuplicateException(email);
        }

        // a user name is unique in a tenant: the user is made in the tenant of the caller
        string userName = input.UserName!.Trim();
        string normalizedName = StringHelper.Normalize(userName);
        Guid? tenantId = CurrentTenant.Id;
        if (await users.AnyAsync(user => user.NormalizedUserName == normalizedName && user.TenantId == tenantId, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new AppUserUserNameDuplicateException(userName);
        }

        await EnsurePasswordAsync(tenantId ?? throw new InvalidOperationException("A user is made in a tenant: the caller has none."), input.Password!, cancellationToken).ConfigureAwait(false);
        AppUser created = AppUser.Create(userName, email, input.DisplayName!, input.Password!, input.LanguageCode);
        await users.AddAsync(created, cancellationToken).ConfigureAwait(false);
        return await GetAsync(created.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task UpdateAsync(AppUserUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        AppUser user = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        string email = input.Email!.Trim();
        string normalizedEmail = StringHelper.Normalize(email);
        Guid id = user.Id;
        if (await users.AnyAsync(other => other.NormalizedEmail == normalizedEmail && other.Id != id, ignoreFilters: true, cancellationToken).ConfigureAwait(false))
        {
            throw new AppUserEmailDuplicateException(email);
        }

        user.Update(email, input.DisplayName!, input.LanguageCode, input.IsActive);
        await users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task SetPasswordAsync(AppUserSetPasswordDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        AppUser user = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        await EnsurePasswordAsync(user.TenantId, input.Password!, cancellationToken).ConfigureAwait(false);
        user.SetPassword(input.Password!);
        await users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task<List<Guid>> GetRoleIdsAsync(Guid id, CancellationToken cancellationToken = default)
    {
        AppUser user = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        Guid userId = user.Id;
        return [.. (await userRoles.GetListAsync(new ListQueryOptions<AppUserRole> { Filter = link => link.UserId == userId }, cancellationToken).ConfigureAwait(false)).Select(link => link.RoleId)];
    }

    public async Task SetRolesAsync(AppUserSetRolesDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        AppUser user = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        Guid userId = user.Id;
        Guid tenantId = user.TenantId;
        HashSet<Guid> wanted = [.. input.RoleIds!];

        // only the roles of the tenant of the user are given: a role of another tenant is not found
        foreach (Guid roleId in wanted)
        {
            if (!await roles.AnyAsync(role => role.Id == roleId && role.TenantId == tenantId, ignoreFilters: false, cancellationToken).ConfigureAwait(false))
            {
                throw AppRoleNotFound.For(roleId);
            }
        }

        IReadOnlyList<AppUserRole> current = await userRoles.GetListAsync(new ListQueryOptions<AppUserRole> { Filter = link => link.UserId == userId }, cancellationToken).ConfigureAwait(false);
        await userRoles.DeleteManyAsync(current.Where(link => !wanted.Contains(link.RoleId)), cancellationToken).ConfigureAwait(false);
        HashSet<Guid> have = [.. current.Select(link => link.RoleId)];
        await userRoles.AddManyAsync(wanted.Where(roleId => !have.Contains(roleId)).Select(roleId => AppUserRole.Create(userId, roleId)), cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        AppUser user = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        Guid userId = user.Id;
        await userRoles.DeleteWhereAsync(link => link.UserId == userId, cancellationToken).ConfigureAwait(false);
        await users.DeleteAsync(user, cancellationToken).ConfigureAwait(false);
    }

    // the password keeps the rules of the tenant of the user, not the ones of the caller
    private async Task EnsurePasswordAsync(Guid tenantId, string password, CancellationToken cancellationToken)
    {
        IReadOnlyList<string> broken = (await policies.GetRulesAsync(tenantId, cancellationToken).ConfigureAwait(false)).Check(password);
        if (broken.Count > 0)
        {
            throw new PasswordPolicyViolationException(broken);
        }
    }

    private async Task<AppUser> FindAsync(Guid id, CancellationToken cancellationToken)
        => await users.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw AppUserNotFound.For(id);

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<AppUser, bool>> Filter(string? searchText, string? userName, string? email, bool? isActive)
    {
        string? search = Normalized(searchText);
        string? nameText = Normalized(userName);
        string? emailText = Normalized(email);
        return new FilterBuilder<AppUser>()
            .And(search is null ? null : user => user.NormalizedUserName.Contains(search) || user.NormalizedEmail.Contains(search) || user.DisplayName.Contains(searchText!.Trim()))
            .And(nameText is null ? null : user => user.NormalizedUserName.Contains(nameText))
            .And(emailText is null ? null : user => user.NormalizedEmail.Contains(emailText))
            .And(isActive is null ? null : user => user.IsActive == isActive.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
