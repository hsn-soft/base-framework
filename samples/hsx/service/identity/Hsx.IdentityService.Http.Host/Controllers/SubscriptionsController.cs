using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Hsx.Shared.Helper;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

/// <summary>The subscriptions: every action only calls the application service, which holds the rules.</summary>
[Route("api/identity-service/v1/commercial/subscriptions")]
public sealed class SubscriptionsController(ISubscriptionAppService subscriptionAppService) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(HsxEndpointPermissions.Identity.Subscriptions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<SubscriptionDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => subscriptionAppService.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.Subscriptions.Read)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<PagedDataResultDto<SubscriptionDto>> GetPagedListAsync([FromBody] GetSubscriptionsPaged pagedInput, CancellationToken cancellationToken = default)
        => subscriptionAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("create")]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.Subscriptions.Create)]
    [ProducesResponseType(StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateAsync([FromBody] SubscriptionCreateDto input, CancellationToken cancellationToken = default)
    {
        SubscriptionDto created = await subscriptionAppService.CreateAsync(input, cancellationToken);
        return CreatedAtAction(nameof(GetAsync), new { id = created.Id }, created);
    }

    [HttpPut]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.Subscriptions.Update)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> UpdateAsync([FromBody] SubscriptionUpdateDto input, CancellationToken cancellationToken = default)
    {
        await subscriptionAppService.UpdateAsync(input, cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id:guid}")]

    [PermissionAuthorize(HsxEndpointPermissions.Identity.Subscriptions.Delete)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await subscriptionAppService.DeleteAsync(id, cancellationToken);
        return NoContent();
    }
}
