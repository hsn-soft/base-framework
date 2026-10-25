using HsnSoft.Base.Localization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Xyz.AuthServer.Application.Contracts.UserDomain.Dtos;
using Xyz.AuthServer.Application.Contracts.UserDomain.Dtos.Submits;
using Xyz.AuthServer.Application.Contracts.UserDomain.Services;
using Xyz.AuthServer.Controllers.Base;

namespace Xyz.AuthServer.Controllers;

/// <summary>
/// What the public gateway may reach (the <c>public</c> segment of the route): open to everyone, so no token. The commercial gateway maps to the
/// <c>commercial</c> controllers and never reaches this one.
/// </summary>
[AllowAnonymous]
[Route("api/auth-server/v1/public/users")]
public sealed class PublicUsersController(IUserAppService userAppService) : BaseServiceController
{
    /// <summary>A greeting in the language of the request: the <c>Accept-Language</c> header (there is no token here), else English. It shows a translated text in a response body.</summary>
    [HttpGet("welcome")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public WelcomeDto Welcome() => new() { Message = L["welcome"].Value, Language = HttpContext.RequestServices.GetRequiredService<ILanguageAccessor>().Current };

    [HttpPost("check-user-name")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public Task<UserNameAvailabilityDto> CheckUserNameAsync([FromBody] UserNameCheckDto input, CancellationToken cancellationToken = default)
        => userAppService.CheckUserNameAsync(input, cancellationToken);
}
