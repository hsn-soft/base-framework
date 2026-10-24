using Abc.IdentityService.Application.Contracts.Dtos;
using Abc.IdentityService.Application.Contracts.UserDomain.Dtos;
using Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Filters;
using Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Submits;
using Abc.IdentityService.Application.Contracts.UserDomain.Services;
using Abc.IdentityService.Domain.UserDomain.Entities;
using Abc.IdentityService.Domain.UserDomain.Exceptions;
using Abc.IdentityService.Domain.UserDomain.Repositories;

namespace Abc.IdentityService.Application.Services;

public sealed class UserAppService(IUserRepository users, TimeProvider clock) : IUserAppService
{
    public async Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => ToDto(await FindAsync(id, cancellationToken).ConfigureAwait(false));

    public async Task<PagedDataResultDto<UserDto>> GetPagedListAsync(GetUsersPaged pagedInput, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(pagedInput);

        IReadOnlyList<User> page = await users.GetPagedListAsync(pagedInput.Skip, pagedInput.Take, pagedInput.IsActive, cancellationToken).ConfigureAwait(false);
        int total = await users.CountAsync(pagedInput.IsActive, cancellationToken).ConfigureAwait(false);
        return new PagedDataResultDto<UserDto> { Items = [.. page.Select(ToDto)], TotalCount = total, Skip = pagedInput.Skip, Take = pagedInput.Take };
    }

    public async Task<UserDto> CreateAsync(UserCreateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        string userName = input.UserName.Trim();
        if (await users.UserNameExistsAsync(userName, null, cancellationToken).ConfigureAwait(false))
        {
            throw new UserUserNameDuplicateException(userName);
        }

        User user = User.Create(userName, input.Email, input.DisplayName, clock.GetUtcNow());
        await users.AddAsync(user, cancellationToken).ConfigureAwait(false);
        await users.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return ToDto(user);
    }

    public async Task UpdateAsync(UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(input);

        User user = await FindAsync(input.Id, cancellationToken).ConfigureAwait(false);
        user.Update(input.Email, input.DisplayName, input.IsActive, clock.GetUtcNow());
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
        IsActive = user.IsActive,
        CreatedAt = user.CreatedAt,
        UpdatedAt = user.UpdatedAt,
    };
}
