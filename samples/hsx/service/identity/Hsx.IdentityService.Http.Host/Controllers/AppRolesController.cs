using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.AppRoleDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

[Route("api/identity-service/v1/commercial/app-roles")]
public sealed class AppRolesController(IAppRoleAppService appRoleAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<AppRoleDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => appRoleAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<AppRoleDto>> GetPagedListAsync([FromBody] GetAppRolesPaged pagedInput, CancellationToken cancellationToken = default)
        => appRoleAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] AppRoleCreateDto input, CancellationToken cancellationToken = default)
    {
        AppRoleDto created = await appRoleAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] AppRoleUpdateDto input, CancellationToken cancellationToken = default)
    {
        await appRoleAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpPost("subscriptions/{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<Guid>> GetSubscriptionIdsAsync(Guid id, CancellationToken cancellationToken = default)
        => appRoleAppService.GetSubscriptionIdsAsync(id, cancellationToken);

    [HttpPut("subscriptions")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> SetSubscriptionsAsync([FromBody] AppRoleSetSubscriptionsDto input, CancellationToken cancellationToken = default)
    {
        await appRoleAppService.SetSubscriptionsAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await appRoleAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
