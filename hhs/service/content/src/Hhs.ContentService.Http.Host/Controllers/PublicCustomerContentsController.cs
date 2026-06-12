using System.Globalization;
using System.Net;
using Hhs.ContentService.Application;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Controllers.Base;
using Hhs.Shared.Hosting.Extensions;
using Hhs.Shared.Hosting.Helpers;
using HsnSoft.Base;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.ContentService.Controllers;

[Route("api/content-service/v1/commercial/contents")] // TODO: Public gateway e göre ayarlanacak v1/public/contents
public sealed class PublicCustomerContentsController(
    IServiceProvider provider,
    ICustomerContentPublicAppService customerContentPublicAppService,
    IHttpContextAccessor httpContextAccessor,
    IWebHostEnvironment environment
) : BaseServiceController(provider)
{
    [AllowAnonymous]
    [HttpPost("get-or-create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<GetOrCreateAppContentResponseDto> GetOrCreateAsync([FromBody] GetOrCreateAppContentRequestDto input, CancellationToken cancellationToken = default)
    {
        // Check domain reference is correct
        CheckDomain(ref input);

        return await customerContentPublicAppService.GetOrCreateAsync(input, cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("get-or-create-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<GetOrCreateAppContentResponseDto> GetOrCreateTestAsync([FromBody] GetOrCreateAppContentRequestDto input, CancellationToken cancellationToken = default)
    {
        if (environment.IsHostProduction())
        {
            throw new BaseHttpException((int)HttpStatusCode.Forbidden);
        }

        if (input == null) throw new InvalidOperationException();

        var testUrl = new Uri(input.ContentKey ?? throw new InvalidOperationException());
        input.DomainName = testUrl.Host.ToLower(new CultureInfo("en-US"));
        input.ContentKey = testUrl.AbsolutePath.ToLower(new CultureInfo("en-US"));

        // return new GetOrCreateAppContentResponseDto
        // {
        //     ContentType = AppContentPublicType.APP_CONTENT,
        //     ContentId = Guid.Parse("fafc0397-6814-42f4-8d4f-98c91f90cfb5"),
        //     ContentStatus = AppContentPublicStatus.READY,
        //     ContentVideoUrl = "https://37fbec7b9db24b68a10856cbfd6aa9ca.b-cdn.net/5faa674f-2a71-4ed8-916c-dc2ac53faa7f.mp4"
        // };
        return await customerContentPublicAppService.GetOrCreateAsync(input, cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("create-ad-result")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task CreateAdResultAsync([FromBody] CreateContentAdResultDto input, CancellationToken cancellationToken = default)
    {
        // CheckUserAgent();

        // await _appContentPublicAppService.CreateAdResultAsync(input, cancellationToken);
    }

    #region Private methods

    [NonAction]
    private void CheckDomain(ref GetOrCreateAppContentRequestDto input)
    {
        if (input.CustomerId == null || string.IsNullOrWhiteSpace(input.DomainName) || string.IsNullOrWhiteSpace(input.ContentKey))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        string originHostAddress = httpContextAccessor?.HttpContext?.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(originHostAddress) || originHostAddress == "null")
        {
            originHostAddress = httpContextAccessor?.HttpContext?.Request.Headers.Referer.ToString();
            if (string.IsNullOrWhiteSpace(originHostAddress) || originHostAddress == "null")
            {
                if (environment.IsHostProduction())
                {
                    throw new BaseHttpException((int)HttpStatusCode.BadRequest, L[ApplicationErrorCodes.UnknownOriginAddress]);
                }

                originHostAddress = "http://localhost:1234";
            }
        }

        Uri originHostUri;
        try
        {
            originHostUri = new Uri(originHostAddress);
            Logger.LogDebug("OriginHost: {OriginHost}", originHostUri.Host);
        }
        catch (Exception)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest, L[ApplicationErrorCodes.InvalidOriginAddress]);
        }

        Uri domainHostUri;
        try
        {
            if (input.DomainName.Equals("file://") && !environment.IsHostProduction())
            {
                input.DomainName = "http://localhost:1234";
            }

            domainHostUri = new Uri(input.DomainName);
            Logger.LogDebug("InputHost: {InputHost}", domainHostUri.Host);
        }
        catch (Exception)
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest, L[ApplicationErrorCodes.InvalidDomainAddress]);
        }

        if (!string.Equals(originHostUri.Host, domainHostUri.Host, StringComparison.CurrentCultureIgnoreCase))
        {
            throw new BaseHttpException((int)HttpStatusCode.Forbidden, L[ApplicationErrorCodes.IncompatibleDomainAddress]);
        }

        CheckUserAgent();

        input.DomainName = domainHostUri.Host.ToLower(new CultureInfo("en-US"));
        input.ContentKey = input.ContentKey.ToLower(new CultureInfo("en-US"));
    }

    [NonAction]
    private void CheckUserAgent()
    {
        var agentDetails = UserAgentProvider.GetUserAgentDetails(httpContextAccessor?.HttpContext?.Request.Headers.UserAgent.ToString() ?? "");
        if (environment.IsHostProduction() && agentDetails is { deviceType: "Unknown" or "Application" } and not { engine: "Postman" })
        {
            throw new BaseHttpException((int)HttpStatusCode.Forbidden, L[ApplicationErrorCodes.IncompatibleDomainAddress]);
        }
    }

    #endregion
}