using HsnSoft.Base.Application.Dtos;
using Xyz.AuthServer.Application.Contracts.UserDomain.Dtos;
using Xyz.AuthServer.Application.Contracts.UserDomain.Dtos.Filters;
using Xyz.AuthServer.Application.Contracts.UserDomain.Dtos.Submits;

namespace Xyz.AuthServer.Application.Contracts.UserDomain.Services;

/// <summary>What can be done with the users. The controller only calls these; the rules and the data access are behind them.</summary>
public interface IUserAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such user.</exception>
    Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>A page of the users that match the filters, with how many match in all.</summary>
    Task<PagedDataResultDto<UserDto>> GetPagedListAsync(GetUsersPaged pagedInput, CancellationToken cancellationToken = default);

    /// <summary>All the users that match the filters (at most <c>Limit</c>), not paged.</summary>
    Task<List<UserDto>> GetFilterListAsync(GetUsersFilter filterInput, CancellationToken cancellationToken = default);

    /// <summary>The users that have the text in their user name or email, shown briefly.</summary>
    Task<List<UserSearchDto>> GetSearchListAsync(GetUsersSearch searchInput, CancellationToken cancellationToken = default);

    /// <exception cref="Xyz.AuthServer.Domain.UserDomain.Exceptions.UserUserNameDuplicateException">The user name is taken.</exception>
    Task<UserDto> CreateAsync(UserCreateDto input, CancellationToken cancellationToken = default);

    /// <summary>Whether nobody has this user name yet (names are compared without regard to case): what a sign-up form asks before it submits.</summary>
    Task<UserNameAvailabilityDto> CheckUserNameAsync(UserNameCheckDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such user.</exception>
    Task UpdateAsync(UserUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such user.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
