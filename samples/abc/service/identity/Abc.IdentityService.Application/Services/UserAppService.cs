using Abc.IdentityService.Application.Contracts.Dtos;
using Abc.IdentityService.Application.Contracts.UserDomain.Dtos;
using Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Filters;
using Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Submits;
using Abc.IdentityService.Application.Contracts.UserDomain.Services;
using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.Domain.UserDomain.Exceptions;
using Abc.IdentityService.Domain.UserDomain.Repositories;

namespace Abc.IdentityService.Application.Services;

public sealed class UserAppService(IUserRepository users) : IUserAppService
{
    public async Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await FindAsync(id, cancellationToken).ConfigureAwait(false));

    public async Task<PagedDataResultDto<UserDto>> GetPagedListAsync(GetUsersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        UserFilter filter = new(pagedInput.UserName, pagedInput.Email, pagedInput.IsActive);
        IReadOnlyList<User> page = await users.GetListAsync(filter, pagedInput.Skip, pagedInput.Take, cancellationToken).ConfigureAwait(false);
        int total = await users.CountAsync(filter, cancellationToken).ConfigureAwait(false);
        return new PagedDataResultDto<UserDto> { Items = [.. page.Select(ToDto)], TotalCount = total, Skip = pagedInput.Skip, Take = pagedInput.Take };
    }

    public async Task<List<UserDto>> GetFilterListAsync(GetUsersFilter filterInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(filterInput);

        UserFilter filter = new(filterInput.UserName, filterInput.Email, filterInput.IsActive);
        IReadOnlyList<User> all = await users.GetListAsync(filter, 0, filterInput.Limit, cancellationToken).ConfigureAwait(false);
        return [.. all.Select(ToDto)];
    }

    public async Task<List<UserSearchDto>> GetSearchListAsync(GetUsersSearch searchInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(searchInput);

        IReadOnlyList<User> found = await users.SearchAsync(searchInput.SearchText, searchInput.Limit, cancellationToken).ConfigureAwait(false);
        return [.. found.Select(user => new UserSearchDto { Id = user.Id, UserName = user.UserName, Email = user.Email })];
    }

    public async Task<UserDto> CreateAsync(UserCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        string userName = input.UserName.Trim();
        if (await users.UserNameExistsAsync(userName, null, cancellationToken).ConfigureAwait(false))
        {
            throw new UserUserNameDuplicateException(userName);
        }

        User user = User.Create(userName, input.Email, input.DisplayName, input.Password, input.Roles);
        await users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToDto(user);
    }

    public async Task<UserNameAvailabilityDto> CheckUserNameAsync(UserNameCheckDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        string userName = input.UserName.Trim();
        bool taken = await users.UserNameExistsAsync(userName, null, cancellationToken).ConfigureAwait(false);
        return new UserNameAvailabilityDto { UserName = userName, IsAvailable = !taken };
    }

    public async Task UpdateAsync(UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        User user = await FindAsync(input.Id, cancellationToken).ConfigureAwait(false);
        user.Update(input.Email, input.DisplayName, input.IsActive, input.Roles);
        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        User user = await FindAsync(id, cancellationToken).ConfigureAwait(false);
        users.Remove(user);
        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    private async Task<User> FindAsync(Guid id, CancellationToken cancellationToken)
        => await users.FindAsync(id, cancellationToken).ConfigureAwait(false) ?? throw UserNotFound.For(id);

    private static UserDto ToDto(User user) => new()
    {
        Id = user.Id,
        UserName = user.UserName,
        Email = user.Email,
        DisplayName = user.DisplayName,
        Roles = user.Roles,
        IsActive = user.IsActive,
        TenantId = user.TenantId,
        CreatedAt = user.CreatedAt,
        CreatedBy = user.CreatedBy,
        UpdatedAt = user.UpdatedAt,
        UpdatedBy = user.UpdatedBy,
    };
}
