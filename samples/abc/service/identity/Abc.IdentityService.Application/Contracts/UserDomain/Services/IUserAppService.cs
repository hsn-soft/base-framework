using Abc.IdentityService.Application.Contracts.Dtos;
using Abc.IdentityService.Application.Contracts.UserDomain.Dtos;
using Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Filters;
using Abc.IdentityService.Application.Contracts.UserDomain.Dtos.Submits;

namespace Abc.IdentityService.Application.Contracts.UserDomain.Services;

/// <summary>What can be done with the users. The controller only calls these; the rules and the data access are behind them.</summary>
public interface IUserAppService
{
    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such user.</exception>
    Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PagedDataResultDto<UserDto>> GetPagedListAsync(GetUsersPaged pagedInput, CancellationToken cancellationToken = default);

    /// <exception cref="Abc.IdentityService.Domain.UserDomain.Exceptions.UserUserNameDuplicateException">The user name is taken.</exception>
    Task<UserDto> CreateAsync(UserCreateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such user.</exception>
    Task UpdateAsync(UserUpdateDto input, CancellationToken cancellationToken = default);

    /// <exception cref="HsnSoft.Base.NotFoundException">There is no such user.</exception>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
