using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;

namespace Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;

public interface IMenuPermissionAppService
{
    // Task<List<MenuMapDto>> GetAllMenuListAsync(GetMenuMapsFilter filter);
    Task<List<MenuMapDto>> GetNodeMenuListAsync(GetMenuMapsFilter filter);
}