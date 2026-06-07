using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;
using HsnSoft.Base.EventBus;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;

public interface ISessionPermissionAppService : IEventApplicationService
{
    Task<SessionPermissionsDto> GetSessionPermissionsAsync(GetSessionPermissionsFilter filter);
}