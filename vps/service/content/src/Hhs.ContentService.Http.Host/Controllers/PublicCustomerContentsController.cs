using System.Globalization;
using System.Net;
using Hhs.ContentService.Application;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos.Submits;
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
    public async Task<GetOrCreateCustomerContentResponseDto> GetOrCreateAsync([FromBody] GetOrCreateCustomerContentRequestDto input, CancellationToken cancellationToken = default)
    {
        // Check domain reference is correct
        CheckDomain(ref input);

        return await customerContentPublicAppService.GetOrCreateAsync(input, cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("create-ad-result")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task CreateAdResultAsync([FromBody] CreateContentAdResultDto input, CancellationToken cancellationToken = default)
    {
        // CheckUserAgent();

        await customerContentPublicAppService.CreateAdResultAsync(input, cancellationToken);
    }

    #region Private methods

    [NonAction]
    private void CheckDomain(ref GetOrCreateCustomerContentRequestDto input)
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