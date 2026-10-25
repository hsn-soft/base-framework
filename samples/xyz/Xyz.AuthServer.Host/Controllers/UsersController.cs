using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Mvc;
using Xyz.AuthServer.Application.Contracts.UserDomain.Dtos;
using Xyz.AuthServer.Application.Contracts.UserDomain.Dtos.Filters;
using Xyz.AuthServer.Application.Contracts.UserDomain.Dtos.Submits;
using Xyz.AuthServer.Application.Contracts.UserDomain.Services;
using Xyz.AuthServer.Controllers.Base;

namespace Xyz.AuthServer.Controllers;

/// <summary>The users: every action only calls the application service, which holds the rules.</summary>
[Route("api/auth-server/v1/commercial/users")]
public sealed class UsersController(IUserAppService userAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => userAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<UserDto>> GetPagedListAsync([FromBody] GetUsersPaged pagedInput, CancellationToken cancellationToken = default)
        => userAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<UserDto>> GetFilterListAsync([FromBody] GetUsersFilter filterInput, CancellationToken cancellationToken = default)
        => userAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<UserSearchDto>> GetSearchListAsync([FromBody] GetUsersSearch searchInput, CancellationToken cancellationToken = default)
        => userAppService.GetSearchListAsync(searchInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] UserCreateDto input, CancellationToken cancellationToken = default)
    {
        UserDto created = await userAppService.CreateAsync(input, cancellationToken);
        Logger.LogInformation("User {CreatedUserId} ({CreatedUserName}) was created", created.Id, created.UserName);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        await userAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await userAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
