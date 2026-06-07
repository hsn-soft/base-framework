using System.Globalization;
using System.Net;
using Hhs.ContentService.Application;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos;
using Hhs.ContentService.Application.Contracts.ContentDomain.Dtos.Filters;
using Hhs.ContentService.Application.Contracts.ContentDomain.Interfaces;
using Hhs.ContentService.Controllers.Base;
using Hhs.Shared.Hosting.Extensions;
using Hhs.Shared.Hosting.Helpers;
using HsnSoft.Base;
using HsnSoft.Base.Application.Dtos;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.ContentService.Controllers;

[Route("api/content-service/v1/commercial/contents")] // TODO: Public gateway e göre ayarlanacak
public sealed class ContentsController : BaseServiceController
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IAppContentPublicAppService _appContentPublicAppService;
    private readonly IAppContentAppService _appContentAppService;
    private readonly IWebHostEnvironment _environment;

    public ContentsController(IServiceProvider provider, IAppContentPublicAppService appContentPublicAppService, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment environment, IAppContentAppService appContentAppService) : base(provider)
    {
        _appContentPublicAppService = appContentPublicAppService;
        _httpContextAccessor = httpContextAccessor;
        _environment = environment;
        _appContentAppService = appContentAppService;
    }

    [HttpGet("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<AppContentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
        => await _appContentAppService.GetAsync(id, cancellationToken);

    // [Authorize(ContentServicePermissions.AppContents.PageView)]
    [HttpPost("paged-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<PagedDataResultDto<AppContentDto>> GetPagedListAsync([FromBody] GetAppContentsPaged pagedInput, CancellationToken cancellationToken = default)
        => await _appContentAppService.GetPagedListAsync(pagedInput, cancellationToken);

    [HttpPost("filter-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<AppContentDto>> GetFilterListAsync([FromBody] GetAppContentsFilter filterInput, CancellationToken cancellationToken = default)
        => await _appContentAppService.GetFilterListAsync(filterInput, cancellationToken);

    [HttpPost("search-list")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<List<AppContentSearchDto>> GetSearchListAsync([FromBody] GetAppContentsSearch searchInput, CancellationToken cancellationToken = default)
        => await _appContentAppService.GetSearchListAsync(searchInput, cancellationToken);

    #region Public Content Endpoints

    [AllowAnonymous]
    [HttpPost("get-or-create")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<GetOrCreateAppContentResponseDto> GetOrCreateAsync([FromBody] GetOrCreateAppContentRequestDto input, CancellationToken cancellationToken = default)
    {
        // Check domain reference is correct
        CheckDomain(ref input);

        return await _appContentPublicAppService.GetOrCreateAsync(input, cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("get-or-create-test")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<GetOrCreateAppContentResponseDto> GetOrCreateTestAsync([FromBody] GetOrCreateAppContentRequestDto input, CancellationToken cancellationToken = default)
    {
        if (_environment.IsHostProduction())
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
        return await _appContentPublicAppService.GetOrCreateAsync(input, cancellationToken);
    }

    [AllowAnonymous]
    [HttpPost("create-ad-result")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task CreateAdResultAsync([FromBody] CreateContentAdResultDto input, CancellationToken cancellationToken = default)
    {
        // CheckUserAgent();

        // await _appContentPublicAppService.CreateAdResultAsync(input, cancellationToken);
    }

    [NonAction]
    private void CheckDomain(ref GetOrCreateAppContentRequestDto input)
    {
        if (input.CustomerId == null || string.IsNullOrWhiteSpace(input.DomainName) || string.IsNullOrWhiteSpace(input.ContentKey))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        string originHostAddress = _httpContextAccessor?.HttpContext?.Request.Headers.Origin.ToString();
        if (string.IsNullOrWhiteSpace(originHostAddress) || originHostAddress == "null")
        {
            originHostAddress = _httpContextAccessor?.HttpContext?.Request.Headers.Referer.ToString();
            if (string.IsNullOrWhiteSpace(originHostAddress) || originHostAddress == "null")
            {
                if (_environment.IsHostProduction())
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
            if (input.DomainName.Equals("file://") && !_environment.IsHostProduction())
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
        var agentDetails = UserAgentProvider.GetUserAgentDetails(_httpContextAccessor?.HttpContext?.Request.Headers.UserAgent.ToString() ?? "");
        if (_environment.IsHostProduction() && agentDetails is { deviceType: "Unknown" or "Application" } and not { engine: "Postman" })
        {
            throw new BaseHttpException((int)HttpStatusCode.Forbidden, L[ApplicationErrorCodes.IncompatibleDomainAddress]);
        }
    }

    #endregion
}