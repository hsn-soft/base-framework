using HsnSoft.Base.Application.Dtos;
using HsnSoft.Base.Authorization;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Filters;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.SubscriptionDomain.Services;
using Hsx.IdentityService.Controllers.Base;
using Hsx.Shared.Contracts.Permissions;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Controllers;

[Route("api/identity-service/v1/commercial/subscriptions")]
public sealed class SubscriptionsController(ISubscriptionAppService subscriptions) : BaseServiceController
{
    [HttpPost("get/{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Subscriptions.Read)]
    public Task<SubscriptionDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => subscriptions.GetAsync(id, cancellationToken);

    [HttpPost("paged-list")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Subscriptions.Read)]
    public Task<PagedDataResultDto<SubscriptionDto>> GetPagedListAsync([FromBody] GetSubscriptionsPagedDto pagedInput, CancellationToken cancellationToken = default)
        => subscriptions.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filters")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Subscriptions.Read)]
    public Task<SubscriptionFiltersDto> GetFiltersAsync(CancellationToken cancellationToken = default)
        => subscriptions.GetFiltersAsync(cancellationToken);

    [HttpPost("create")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Subscriptions.Create)]
    public Task<SubscriptionDto> CreateAsync([FromBody] SubscriptionCreateDto input, CancellationToken cancellationToken = default)
        => subscriptions.CreateAsync(input, cancellationToken);

    [HttpPut]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Subscriptions.Update)]
    public Task<SubscriptionDto> UpdateAsync([FromBody] SubscriptionUpdateDto input, CancellationToken cancellationToken = default)
        => subscriptions.UpdateAsync(input, cancellationToken);

    [HttpPut("transfer")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Subscriptions.Transfer)]
    public Task<SubscriptionDto> TransferAsync([FromBody] SubscriptionTransferDto input, CancellationToken cancellationToken = default)
        => subscriptions.TransferAsync(input, cancellationToken);

    [HttpDelete("{id:guid}")]
    [PermissionAuthorize(IdentityPermissions.Endpoints.Subscriptions.Delete)]
    public Task<SubscriptionDto> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        => subscriptions.DeleteAsync(id, cancellationToken);
}
