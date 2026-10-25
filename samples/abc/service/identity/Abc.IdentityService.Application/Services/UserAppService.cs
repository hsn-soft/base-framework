using System.Linq.Expressions;
using Abc.IdentityService.Application.Contracts.UserDomain.Dtos;
using Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Filters;
using Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Submits;
using Abc.IdentityService.Application.Contracts.UserDomain.Services;
using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.Domain.UserDomain.Exceptions;
using Abc.IdentityService.Domain.UserDomain.Repositories;
using HsnSoft.Base.Application;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Domain.Repositories;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Text;

namespace Abc.IdentityService.Application.Services;

public sealed class UserAppService(IRepository<User, Guid> users, IServiceProvider services) : ApplicationServiceBase(services), IUserAppService
{
    public async Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await FindAsync(id, cancellationToken).ConfigureAwait(false));

    public async Task<PagedDataResultDto<UserDto>> GetPagedListAsync(GetUsersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        PagedQueryResult<User> page = await users.GetPagedListAsync(
            pagedInput.ToPagedOptions(Filter(pagedInput.SearchText, pagedInput.UserName, pagedInput.Email, pagedInput.IsActive), UserSorting.Fields, UserSorting.Default), cancellationToken).ConfigureAwait(false);
        return page.ToPagedDto(pagedInput, ToDto);
    }

    public async Task<List<UserDto>> GetFilterListAsync(GetUsersFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        IReadOnlyList<User> all = await users.GetListAsync(
            filterInput.ToListOptions(Filter(null, filterInput.UserName, filterInput.Email, filterInput.IsActive), UserSorting.Fields, UserSorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. all.Select(ToDto)];
    }

    public async Task<List<UserSearchDto>> GetSearchListAsync(GetUsersSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        IReadOnlyList<User> found = await users.GetListAsync(
            searchInput.ToListOptions(Filter(searchInput.SearchText, null, null, null), UserSorting.Fields, UserSorting.Default), cancellationToken).ConfigureAwait(false);
        return [.. found.Select(user => new UserSearchDto { Id = user.Id, UserName = user.UserName, Email = user.Email })];
    }

    public async Task<UserDto> CreateAsync(UserCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        string userName = input.UserName.Trim();
        if (await IsTakenAsync(userName, cancellationToken).ConfigureAwait(false))
        {
            throw new UserUserNameDuplicateException(userName);
        }

        User user = User.Create(userName, input.Email, input.DisplayName, input.Password, input.Roles, input.Language);
        await users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        return ToDto(user);
    }

    public async Task<UserNameAvailabilityDto> CheckUserNameAsync(UserNameCheckDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        string userName = input.UserName.Trim();
        return new UserNameAvailabilityDto { UserName = userName, IsAvailable = !await IsTakenAsync(userName, cancellationToken).ConfigureAwait(false) };
    }

    public async Task UpdateAsync(UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        User user = await FindAsync(input.Id, cancellationToken).ConfigureAwait(false);
        user.Update(input.Email, input.DisplayName, input.IsActive, input.Roles, input.Language);
        await users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        User user = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        await users.DeleteAsync(user, cancellationToken).ConfigureAwait(false);
    }

    private async Task<User> FindAsync(Guid id, CancellationToken cancellationToken)
        => await users.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw UserNotFound.For(id);

    // a user name is unique in the whole system: a deleted user's name and another tenant's are taken too
    private Task<bool> IsTakenAsync(string userName, CancellationToken cancellationToken)
    {
        string normalized = StringHelper.Normalize(userName);
        return users.AnyAsync(user => user.NormalizedUserName == normalized, ignoreFilters: true, cancellationToken);
    }

    // what the client gave narrows the list; what it left out does not
    private static Expression<Func<User, bool>> Filter(string? searchText, string? userName, string? email, bool? isActive)
    {
        string? search = Normalized(searchText);
        string? nameText = Normalized(userName);
        string? emailText = Normalized(email);
        return new FilterBuilder<User>()
            .And(search is null ? null : user => user.NormalizedUserName.Contains(search) || user.NormalizedEmail.Contains(search))
            .And(nameText is null ? null : user => user.NormalizedUserName.Contains(nameText))
            .And(emailText is null ? null : user => user.NormalizedEmail.Contains(emailText))
            .And(isActive is null ? null : user => user.IsActive == isActive.Value)
            .Build();
    }

    private static string? Normalized(string? text) => string.IsNullOrWhiteSpace(text) ? null : StringHelper.Normalize(text);

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        DisplayName = user.DisplayName,
        Language = user.Language,
        Roles = user.Roles,
        IsActive = user.IsActive,
        TenantId = user.TenantId,
        CreatedAt = user.CreatedAt,
        CreatedBy = user.CreatedBy,
        UpdatedAt = user.UpdatedAt,
        UpdatedBy = user.UpdatedBy,
    };
}
