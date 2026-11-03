using Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

/// <summary>The subscriptions whose data the caller may see (what its token's scope keys name): the selector of the customers and products of a client. Any signed-in user may ask; it needs no permission.</summary>
[Route("api/identity-service/v1/commercial/me/subscriptions")]
public sealed class MySubscriptionsController(IMySubscriptionAppService mySubscriptions) : BaseServiceController
{
    [HttpPost("get")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<List<MySubscriptionDto>> GetAsync(CancellationToken cancellationToken = default)
        => mySubscriptions.GetMineAsync(cancellationToken);
}
