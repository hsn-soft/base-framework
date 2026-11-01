using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MyAccountDomain.Services;
using Hsx.IdentityService.Http.Host.Controllers.Base;
using Microsoft.AspNetCore.Mvc;

namespace Hsx.IdentityService.Http.Host.Controllers;

[Route("api/identity-service/v1/commercial/my-account")]
public sealed class MyAccountController(IMyAccountAppService myAccountAppService) : BaseServiceController
{
    /// <summary>Changes the password of the caller (it gives the current one).</summary>
    [HttpPut("password")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public async Task<IActionResult> ChangePasswordAsync([FromBody] ChangePasswordDto input, CancellationToken cancellationToken = default)
    {
        await myAccountAppService.ChangePasswordAsync(input, cancellationToken);
        return NoContent();
    }
}
