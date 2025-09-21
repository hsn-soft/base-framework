using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Test.Api.Application.Contracts;
using JetBrains.Annotations;

namespace HsnSoft.Base.Test.Api.Application.Services;

public interface IEfCoreUserService
{
    Task<UserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default);

    [ItemCanBeNull]
    Task<UserDto> FindFirstUserAsync(GetOrderedUserFilterDto input, CancellationToken cancellationToken = default);

    Task<List<UserDto>> GetUserListAsync(GetOrderedUserFilterDto input, CancellationToken cancellationToken = default);

    Task<List<UserSearchResultDto>> SearchUserListAsync(SearchOrderedFilterDto input, CancellationToken cancellationToken = default);

    Task<long> GetUserCountAsync(GetUserFilterDto input, CancellationToken cancellationToken = default);

    Task<PagedQueryResult<UserDto>> GetPagingUsersAsync(GetPagingUserFilterDto input, CancellationToken cancellationToken = default);

    Task<int> InsertUserAsync(CreateUserDto input, CancellationToken cancellationToken = default);
    Task<int> UpdateUserAsync(UpdateUserDto input, CancellationToken cancellationToken = default);
    Task<int> DeleteUserAsync(DeleteUserDto input, CancellationToken cancellationToken = default);
    Task<int> UnitOfWorkTestAsync(CancellationToken cancellationToken = default);
}