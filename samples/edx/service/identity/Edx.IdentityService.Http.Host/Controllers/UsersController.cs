using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Submits;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IdentityService.Http.Host.Controllers;

/// <summary>The users: a customer's user sees and manages its own tenant's, a user of the system tenant all. Every action only calls the application service.</summary>
[Route("api/identity-service/v1/commercial/users")]
public sealed class UsersController(IUserAppService users) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.Users.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => users.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.Users.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<UserDto>> GetPagedListAsync([FromBody] GetUsersPaged pagedInput, CancellationToken cancellationToken = default)
        => users.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.Users.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] UserCreateDto input, CancellationToken cancellationToken = default)
    {
        UserDto created = await users.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.Users.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] UserUpdateDto input, CancellationToken cancellationToken = default)
    {
        await users.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(EdxEndpointPermissions.Identity.Users.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await users.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
