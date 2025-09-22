using HsnSoft.Base.Domain.Models;
using HsnSoft.Base.Test.Api.Application.Contracts;
using HsnSoft.Base.Test.Api.Application.Services;
using HsnSoft.Base.Users;
using Microsoft.AspNetCore.Mvc;

namespace HsnSoft.Base.Test.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class EfCoreUsersController(IEfCoreUserService userService, ICurrentUser currentUser) : ControllerBase
{
    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<UserDto> GetUserAsync(Guid id) => await userService.GetUserByIdAsync(id);

    [HttpPost("find-first-user")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<UserDto> FindFirstUserAsync([FromBody] GetOrderedUserFilterDto input, CancellationToken cancellationToken)
    {
        return await userService.FindFirstUserAsync(input, cancellationToken);
    }

    [HttpPost("get-user-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<UserDto>> GetUserListAsync([FromBody] GetOrderedUserFilterDto input, CancellationToken cancellationToken)
    {
        return await userService.GetUserListAsync(input, cancellationToken);
    }

    [HttpPost("search-user-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<UserSearchResultDto>> SearchUserListAsync([FromBody] SearchOrderedFilterDto input, CancellationToken cancellationToken)
    {
        return await userService.SearchUserListAsync(input, cancellationToken);
    }

    [HttpPost("get-user-count")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<long> GetUserCountAsync([FromBody] GetUserFilterDto input, CancellationToken cancellationToken)
    {
        return await userService.GetUserCountAsync(input, cancellationToken);
    }

    [HttpPost("get-paging-users")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<PagedQueryResult<UserDto>> GetPagingUsersAsync([FromBody] GetPagingUserFilterDto input, CancellationToken cancellationToken)
    {
        return await userService.GetPagingUsersAsync(input, cancellationToken);
    }

    [HttpPost("create-user")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<int> InsertUserAsync([FromBody] CreateUserDto input, CancellationToken cancellationToken)
    {
        var test = currentUser.Id;
        return await userService.InsertUserAsync(input, cancellationToken);
    }

    [HttpPost("update-user")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<int> UpdateUserAsync([FromBody] UpdateUserDto input, CancellationToken cancellationToken)
    {
        return await userService.UpdateUserAsync(input, cancellationToken);
    }

    [HttpPost("delete-user")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<int> DeleteUserAsync([FromBody] DeleteUserDto input, CancellationToken cancellationToken)
    {
        return await userService.DeleteUserAsync(input, cancellationToken);
    }

    [HttpPost("unit-of-work")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<int> UnitOfWorkTestAsync(CancellationToken cancellationToken)
    {
        return await userService.UnitOfWorkTestAsync(cancellationToken);
    }
}