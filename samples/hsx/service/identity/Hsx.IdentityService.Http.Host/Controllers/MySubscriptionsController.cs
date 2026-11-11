using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MySubscriptionDomain.Services;
using Hsx.IdentityService.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Controllers;

[Route("api/identity-service/v1/commercial/me/subscriptions")]
public sealed class MySubscriptionsController(IMySubscriptionAppService mySubscriptions) : BaseServiceController
{
    [HttpPost("get")]
    public Task<ListDataResultDto<MySubscriptionDto>> GetAsync(CancellationToken cancellationToken = default)
        => mySubscriptions.GetMineAsync(cancellationToken);
}
