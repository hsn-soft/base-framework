using System.Linq.Expressions;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Exceptions;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using Edx.Shared.Helper;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;

namespace Edx.IdentityService.Application.Services;

public sealed class UserAppService(IUserRepository users, IPermissionChecker permissions, IServiceProvider services) : ApplicationService(services), IUserAppService
{
    // which tenant's users are read is not decided here: the tenant filter of the context does it (a customer's own, all for the system tenant)
    public async Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        User user = await users.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw UserNotFound.For(id);
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> roles = await users.GetRoleNamesAsync([user.Id], cancellationToken).ConfigureAwait(false);
        return ToDto(user, roles, await CanSeeEmailAsync(cancellationToken).ConfigureAwait(false));
    }

    public async Task<PagedDataResultDto<UserDto>> GetPagedListAsync(GetUsersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<User> page = await users.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput), IdentitySorting.Users, IdentitySorting.Default), cancellationToken).ConfigureAwait(false);
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> roles = await users.GetRoleNamesAsync([.. page.Items.Select(user => user.Id)], cancellationToken).ConfigureAwait(false);
        bool email = await CanSeeEmailAsync(cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, user => ToDto(user, roles, email));
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
