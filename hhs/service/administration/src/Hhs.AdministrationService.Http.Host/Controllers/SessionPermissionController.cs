using System.Net;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using Hhs.AdministrationService.Controllers.Base;
using HsnSoft.Base;
using HsnSoft.Base.Users;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.AdministrationService.Controllers;

[Route("api/administration-service/v1/commercial/session-permissions")]
public sealed class SessionPermissionController : BaseServiceController
{
    private readonly ICurrentUser _currentUser;
    private readonly ISessionPermissionAppService _sessionPermissionAppService;

    public SessionPermissionController(IServiceProvider provider,
        ISessionPermissionAppService sessionPermissionAppService,
        ICurrentUser currentUser
    ) : base(provider)
    {
        _sessionPermissionAppService = sessionPermissionAppService;
        _currentUser = currentUser;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<SessionPermissionsDto> GetSessionPermissionsAsync()
    {
        var filter = new GetSessionPermissionsFilter
        {
            ClientKey = "", // current client silindi
            RoleKeys = _currentUser?.RoleKeys,
            UserKey = _currentUser?.UserName
        };

        if (string.IsNullOrWhiteSpace(filter.ClientKey) && string.IsNullOrWhiteSpace(filter.UserKey) && filter.RoleKeys is not { Length: > 0 })
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        return await _sessionPermissionAppService.GetSessionPermissionsAsync(filter);
    }
}