using System.Security.Principal;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Data;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using Hhs.AdministrationService.Domain.PermissionDomain.Entities;
using HsnSoft.Base.Security.Claims;

namespace Hhs.AdministrationService.Application.Services;

public sealed class MenuPermissionAppService : ApplicationServiceBase, IMenuPermissionAppService
{
    private readonly ICurrentPrincipalAccessor _principalAccessor;

    public MenuPermissionAppService(IServiceProvider provider, ICurrentPrincipalAccessor principalAccessor) : base(provider)
    {
        _principalAccessor = principalAccessor ?? throw new ArgumentNullException(nameof(principalAccessor), "MenuPermissionAppService principalAccessor is null");
    }

    // public async Task<List<MenuMapDto>> GetAllMenuListAsync(GetMenuMapsFilter filter)
    // {
    //     filter ??= new GetMenuMapsFilter();
    //
    //     var items = filter.ClientMenuType is not null
    //         ? StaticData.MenuMapDatas.Where(x => x.ClientMenuType.Equals(filter.ClientMenuType)).ToList()
    //         : StaticData.MenuMapDatas.ToList();
    //
    //     if (filter.ShowSystemMenus.HasValue && !filter.ShowSystemMenus.Value)
    //     {
    //         items = items.Where(x => x.OnlyAccessSystemUsers == false).ToList();
    //     }
    //
    //     return Mapper.Map<List<MenuMap>, List<MenuMapDto>>(items);
    // }

    public async Task<List<MenuMapDto>> GetNodeMenuListAsync(GetMenuMapsFilter filter)
    {
        filter ??= new GetMenuMapsFilter();

        var items = StaticData.MenuMapDatas.Where(x => x.ClientId.Equals(_principalAccessor.Principal?.FindClientId())).ToList();

        if (filter.ClientMenuType is not null)
        {
            items = StaticData.MenuMapDatas.Where(x => x.ClientMenuType.Equals(filter.ClientMenuType)).ToList();
        }

        if (filter.ShowSystemMenus.HasValue && !filter.ShowSystemMenus.Value)
        {
            items = items.Where(x => x.OnlyAccessSystemUsers == false).ToList();
        }

        return Mapper.Map<List<MenuMap>, List<MenuMapDto>>(items);
    }
}