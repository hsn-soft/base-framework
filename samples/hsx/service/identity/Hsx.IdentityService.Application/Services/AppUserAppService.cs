using System.Linq.Expressions;
using HsnSoft.Base;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.EventBus;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hsx.IdentityService.Application.Security;
using Hsx.IdentityService.Domain.AppRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppRoleDomain.Exceptions;
using Hsx.IdentityService.Domain.AppRoleDomain.Repositories;
using Hsx.IdentityService.Domain.AppUserDomain.Consts;
using Hsx.IdentityService.Domain.AppUserDomain.Entities;
using Hsx.IdentityService.Domain.AppUserDomain.Exceptions;
using Hsx.IdentityService.Domain.AppUserDomain.Repositories;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Entities;
using Hsx.IdentityService.Domain.AppUserRoleDomain.Repositories;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Entities;
using Hsx.IdentityService.Domain.AuthPasswordPolicyDomain.Repositories;
using Hsx.IdentityService.Domain.AuthRefreshTokenDomain.Repositories;
using Hsx.Shared.Contracts.Events.Identity;
using Hsx.Shared.Helper;
using Microsoft.Extensions.Options;

namespace Hsx.IdentityService.Application.Services;

/// <summary>
/// The users. A user is made with its roles in one unit of work, with a temporary password the system makes; the event that tells the user is published after the unit has ended (never inside it: a message is not a part of the transaction, and a rollback must not
/// have sent it). A change of the roles of a user, a new temporary password and a delete end its sessions (its refresh tokens are revoked in the same unit).
/// </summary>
public sealed class AppUserAppService(
    IAppUserRepository users,
    IAppRoleRepository roles,
    IAppUserRoleRepository userRoles,
    IAuthRefreshTokenRepository refreshTokens,
    IAuthPasswordPolicyRepository policies,
    NewUserFactory factory,
    IOptions<TemporaryPasswordOptions> options,
    TimeProvider time,
    IUnitOfWork unitOfWork,
    IEventBus eventBus,
    IServiceProvider services) : ApplicationServiceBase(services), IAppUserAppService
{
    public async Task<AppUserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await users.FindAsync<AppUserDto>(id, cancellationToken: cancellationToken).ConfigureAwait(false) ?? throw AppUserNotFound.For(id);

    public async Task<PagedDataResultDto<AppUserDto>> GetPagedListAsync(GetAppUsersPagedDto pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);
        PagedQueryResult<AppUserDto> page = await users.GetPagedListAsync<AppUserDto>(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.UserName, pagedInput.Email, pagedInput.IsActive), AppUserSorting.Fields, AppUserSorting.Default), cancellationToken: cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput);
    }

    public async Task<AppUserDto> CreateAsync(AppUserCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        Guid tenantId = CurrentTenant.Id ?? throw new NotFoundException("The caller has no tenant.").WithKey("tenant_not_found");
        List<Guid> roleIds = [.. (input.RoleIds ?? []).Distinct()];
        MadeUser made = await unitOfWork.ExecuteAsync(async token =>
        {
            foreach (Guid roleId in roleIds)
            {
                _ = await roles.FindAsync(roleId, token).ConfigureAwait(false) ?? throw AppRoleNotFound.For(roleId);
            }

            MadeUser result = await factory.MakeAsync(tenantId, new NewUserInput(input.UserName!, input.DisplayName!, input.Email!, input.NotificationType, input.PhoneNumber, input.LanguageCode), token).ConfigureAwait(false);
            await userRoles.AddManyAsync([.. roleIds.Select(roleId => AppUserRole.Create(result.User.Id, roleId))], token).ConfigureAwait(false);
            return result;
        }, cancellationToken).ConfigureAwait(false);

        await eventBus.PublishAsync(made.Event, cancellationToken: cancellationToken).ConfigureAwait(false);
        return await GetAsync(made.User.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppUserDto> UpdateAsync(AppUserUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        AppUser user = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        string email = input.Email!.Trim();
        if (await users.EmailExistsAsync(user.TenantId, StringHelper.Normalize(email), user.Id, cancellationToken).ConfigureAwait(false))
        {
            throw new AppUserEmailDuplicateException(email);
        }

        user.Update(email, input.DisplayName!, input.LanguageCode, input.IsActive, input.PhoneNumber, user.AvatarSuffixUrl);
        await users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
        return await GetAsync(user.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppUserRolesDto> GetRolesAsync(Guid id, CancellationToken cancellationToken = default)
    {
        AppUser user = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        return await RolesOfAsync(user.Id, cancellationToken).ConfigureAwait(false);
    }

    public async Task<AppUserRolesDto> SetRolesAsync(AppUserSetRolesDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        AppUser user = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        List<Guid> roleIds = [.. input.RoleIds!.Distinct()];
        foreach (Guid roleId in roleIds)
        {
            // a role of another tenant is not one of the user
            AppRole role = await roles.FindOfAnyTenantAsync(roleId, cancellationToken).ConfigureAwait(false) ?? throw AppRoleNotFound.For(roleId);
            if (role.TenantId != user.TenantId)
            {
                throw AppRoleNotFound.For(roleId);
            }
        }

        Guid userId = user.Id;
        await unitOfWork.ExecuteAsync(async token =>
        {
            await userRoles.DeleteWhereAsync(link => link.UserId == userId, token).ConfigureAwait(false);
            await userRoles.AddManyAsync([.. roleIds.Select(roleId => AppUserRole.Create(userId, roleId))], token).ConfigureAwait(false);
            await refreshTokens.RevokeForUsersAsync([userId], token).ConfigureAwait(false);   // the roles are the grants: the user signs in again to get them
            return true;
        }, cancellationToken).ConfigureAwait(false);

        return await RolesOfAsync(userId, cancellationToken).ConfigureAwait(false);
    }

    public async Task<TemporaryPasswordIssuedDto> IssueTemporaryPasswordAsync(IssueTemporaryPasswordDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        AppUser user = await FindAsync(input.Id.GetValueOrDefault(), cancellationToken).ConfigureAwait(false);
        if (input.NotificationType == NotificationTypes.Sms && string.IsNullOrWhiteSpace(user.PhoneNumber))
        {
            throw new AppUserContactMissingException("phone number");
        }

        PasswordRules rules = await policies.GetRulesAsync(user.TenantId, cancellationToken).ConfigureAwait(false);
        string password = options.Value.SamplePassword ?? TemporaryPasswordGenerator.Generate(rules);
        DateTimeOffset expiresAt = time.GetUtcNow().Add(options.Value.Lifetime);

        user.SetTemporaryPassword(password, expiresAt);
        await unitOfWork.ExecuteAsync(async token =>
        {
            await users.UpdateAsync(user, token).ConfigureAwait(false);
            await refreshTokens.RevokeForUsersAsync([user.Id], token).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);

        await eventBus.PublishAsync(new UserPasswordResetEto(user.Id, user.DisplayName, input.NotificationType, user.Email, user.PhoneNumber, user.LanguageCode, password, expiresAt), cancellationToken: cancellationToken).ConfigureAwait(false);
        return new TemporaryPasswordIssuedDto { UserId = user.Id, NotificationType = input.NotificationType, PasswordExpiresAt = expiresAt };
    }

    public async Task<AppUserDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        AppUser user = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        if (user.IsStatic)
        {
            throw new AppUserNotDeletableException(user.UserName);
        }

        AppUserDto deleted = await GetAsync(id, cancellationToken).ConfigureAwait(false);
        await unitOfWork.ExecuteAsync(async token =>
        {
            await userRoles.DeleteWhereAsync(link => link.UserId == id, token).ConfigureAwait(false);
            await refreshTokens.RevokeForUsersAsync([id], token).ConfigureAwait(false);
            await users.DeleteAsync(user, token).ConfigureAwait(false);
            return true;
        }, cancellationToken).ConfigureAwait(false);

        return deleted;
    }

    private async Task<AppUser> FindAsync(Guid id, CancellationToken cancellationToken)
        => await users.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw AppUserNotFound.For(id);

    private async Task<AppUserRolesDto> RolesOfAsync(Guid userId, CancellationToken cancellationToken)
        => new() { UserId = userId, RoleIds = [.. (await userRoles.GetListAsync(new ListQueryOptions<AppUserRole> { Filter = link => link.UserId == userId }, cancellationToken).ConfigureAwait(false)).Select(link => link.RoleId)] };

    private static Expression<Func<AppUser, bool>> Filter(string? searchText, string? userName, string? email, bool? isActive)
    {
        string? search = Normalized(searchText);
        string? userNameText = Normalized(userName);
        string? emailText = Normalized(email);
        return new FilterBuilder<AppUser>()
            .And(search is null ? null : user => user.NormalizedUserName.Contains(search) || user.NormalizedEmail.Contains(search))
            .And(userNameText is null ? null : user => user.NormalizedUserName.Contains(userNameText))
            .And(emailText is null ? null : user => user.NormalizedEmail.Contains(emailText))
            .And(isActive is null ? null : user => user.IsActive == isActive)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);
}
