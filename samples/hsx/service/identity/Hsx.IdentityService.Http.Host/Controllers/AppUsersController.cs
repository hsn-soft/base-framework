using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.AppUserDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

[Route("api/identity-service/v1/commercial/app-users")]
public sealed class AppUsersController(IAppUserAppService appUserAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<AppUserDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => appUserAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<AppUserDto>> GetPagedListAsync([FromBody] GetAppUsersPaged pagedInput, CancellationToken cancellationToken = default)
        => appUserAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] AppUserCreateDto input, CancellationToken cancellationToken = default)
    {
        AppUserDto created = await appUserAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] AppUserUpdateDto input, CancellationToken cancellationToken = default)
    {
        await appUserAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetPasswordAsync([FromBody] AppUserSetPasswordDto input, CancellationToken cancellationToken = default)
    {
        await appUserAppService.SetPasswordAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpPost("roles/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<Guid>> GetRoleIdsAsync(Guid id, CancellationToken cancellationToken = default)
        => appUserAppService.GetRoleIdsAsync(id, cancellationToken);

    [HttpPut("roles")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetRolesAsync([FromBody] AppUserSetRolesDto input, CancellationToken cancellationToken = default)
    {
        await appUserAppService.SetRolesAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await appUserAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
