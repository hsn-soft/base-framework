using AutoMapper;
using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Reflection;
using HsnSoft.Base.Test.Api.Application.Contracts;
using HsnSoft.Base.Test.Api.Domain.Consts;
using HsnSoft.Base.Test.Api.Domain.Entities;
using HsnSoft.Base.Test.Api.Domain.Repositories;

namespace HsnSoft.Base.Test.Api.Application.Services;

public class MongoUserService(IMongoUserRepository userRepository, IMapper mapper) : IMongoUserService
{
    public Task<UserDto> GetUserByIdAsync(Guid id, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<UserDto> FindFirstUserAsync(GetOrderedUserFilterDto input, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<List<UserDto>> GetUserListAsync(GetOrderedUserFilterDto input, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public Task<List<UserSearchResultDto>> SearchUserListAsync(SearchOrderedFilterDto input, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<long> GetUserCountAsync(GetUserFilterDto input, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public async Task<PaginationResult<UserDto>> GetPagingUsersAsync(GetPagingUserFilterDto input, CancellationToken cancellationToken = default)
    {
        input ??= new GetPagingUserFilterDto();
        if (input.MaxAge.HasValue) input.MaxAge = input.MaxAge.Value + 1;

        var filter = new FilterBuilder<User>()
            .And(!string.IsNullOrWhiteSpace(input.FirstName) ? u => u.FirstName.Contains(input.FirstName) : null)
            .And(!string.IsNullOrWhiteSpace(input.LastName) ? u => u.LastName.Contains(input.LastName) : null)
            .And(input.MinAge is > 0 ? u => u.Age >= input.MinAge.Value : null)
            .And(input.MaxAge is > 0 ? u => u.Age < input.MaxAge.Value : null)
            .Build();

        var result = await userRepository.GetPageListAsync(
            selector: u => new UserDto { Id = u.Id, FullName = u.FirstName + " " + u.LastName, Email = u.Email },
            options: new PaginationQueryOptions<User>
            {
                Filter = filter,
                OrderByDynamic = string.IsNullOrWhiteSpace(input.OrderByText)
                    ? UserConsts.GetDefaultSorting()
                    : input.OrderByText,
                PageNumber = input.PageNumber ?? 1,
                PageSize = input.PageSize ?? 10
            }, cancellationToken: cancellationToken);

        return result;
    }

    public Task<int> InsertUserAsync(CreateUserDto input, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<int> UpdateUserAsync(UpdateUserDto input, CancellationToken cancellationToken = default) => throw new NotImplementedException();

    public Task<int> DeleteUserAsync(DeleteUserDto input, CancellationToken cancellationToken = default) => throw new NotImplementedException();
}