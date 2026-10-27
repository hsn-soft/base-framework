using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos.Filters;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Http.Host.Controllers.Base;
using Edx.Shared.Helper;
using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Edx.IdentityService.Http.Host.Controllers;

/// <summary>The users: a customer's user sees its own tenant's, a user of the system tenant sees all. Every action only calls the application service.</summary>
[Route("api/identity-service/v1/commercial/users")]
public sealed class UsersController(IUserAppService users) : BaseServiceController
{
    [PermissionAuthorize(EdxEndpointPermissions.Identity.Users.Get)]
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<UserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => users.GetAsync(id, cancellationToken);

    [PermissionAuthorize(EdxEndpointPermissions.Identity.Users.PagedList)]
    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<UserDto>> GetPagedListAsync([FromBody] GetUsersPaged pagedInput, CancellationToken cancellationToken = default)
        => users.GetPagedListAsync(pagedInput, cancellationToken);
}
