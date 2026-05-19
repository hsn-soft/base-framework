using System.Net;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using Hhs.AdministrationService.Controllers.Base;
using HsnSoft.Base;
using HsnSoft.Base.Clients;
using HsnSoft.Base.Users;
using Microsoft.AspNetCore.Mvc;

namespace Hhs.AdministrationService.Controllers;

[Route("api/administration-service/v1/commercial/session-permissions")]
public sealed class SessionPermissionController : BaseServiceController
{
    private readonly ICurrentClient _currentClient;
    private readonly ICurrentUser _currentUser;
    private readonly ISessionPermissionAppService _sessionPermissionAppService;

    public SessionPermissionController(IServiceProvider provider,
        ISessionPermissionAppService sessionPermissionAppService,
        ICurrentClient currentClient,
        ICurrentUser currentUser
    ) : base(provider)
    {
        _sessionPermissionAppService = sessionPermissionAppService;
        _currentClient = currentClient;
        _currentUser = currentUser;
    }

    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<SessionPermissionsDto> GetSessionPermissionsAsync()
    {
        var filter = new GetSessionPermissionsFilter
        {
            ClientKey = _currentClient?.Id,
            RoleKeys = _currentUser?.Roles,
            UserKey = _currentUser?.UserName
        };

        if (string.IsNullOrWhiteSpace(filter.ClientKey) && string.IsNullOrWhiteSpace(filter.UserKey) && filter.RoleKeys is not { Length: > 0 })
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        return await _sessionPermissionAppService.GetSessionPermissionsAsync(filter);
    }
}