using Hsx.IdentityService.Application.Contracts.AppUserDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Dtos.Submits;
using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

[Route("api/identity-service/v1/commercial/my-account")]
public sealed class MyAccountController(IMyAccountAppService myAccount) : BaseServiceController
{
    [HttpPost("get")]
    public Task<AppUserDto> GetAsync(CancellationToken cancellationToken = default)
        => myAccount.GetAsync(cancellationToken);

    [HttpPut("password")]
    public Task<PasswordChangedDto> ChangePasswordAsync([FromBody] ChangePasswordDto input, CancellationToken cancellationToken = default)
        => myAccount.ChangePasswordAsync(input, cancellationToken);
}
