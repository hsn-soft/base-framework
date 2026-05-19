using System.Net;
using Hhs.AdministrationService.Application.Contracts.Events;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Data;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using Hhs.AdministrationService.Domain.Enums;
using Hhs.AdministrationService.Domain.PermissionDomain.Repositories;
using HsnSoft.Base;

namespace Hhs.AdministrationService.Application.Services;

public sealed class RolePermissionAppService : ApplicationServiceBase, IRolePermissionAppService
{
    private readonly IPermissionGrantRepository _permissionGrantRepository;
    private readonly IMenuPermissionAppService _menuPermissionAppService;

    public RolePermissionAppService(IServiceProvider provider,
        IPermissionGrantRepository permissionGrantRepository,
        IMenuPermissionAppService menuPermissionAppService
    ) : base(provider)
    {
        _permissionGrantRepository = permissionGrantRepository;
        _menuPermissionAppService = menuPermissionAppService;
    }

    public async Task<RolePermissionsDto> GetRolePermissionsAsync(GetRolePermissionsFilter filter)
    {
        if (filter == null || string.IsNullOrWhiteSpace(filter.RoleUniqueName))
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var result = new RolePermissionsDto
        {
            RoleUniqueName = filter.RoleUniqueName,
            ChannelMenus = new List<RoleMenuChannelDto>()
        };

        var entityPermissions = await _permissionGrantRepository.GetSessionPermissionsAsync(roleKeys: new[] { filter.RoleUniqueName ?? string.Empty });
        if (entityPermissions == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.RequestTimeout);
        }

        // Get all role permissions
        var rolePermissions = entityPermissions.GroupBy(x => x.Name).Select(x => x.Key).ToList();

        // tenant not access system menus
        var menuFilter = new GetMenuMapsFilter();
        if ((CurrentUser.FindClaim("user_s")?.Value ?? "false") == "true")
        {
            menuFilter.ShowSystemMenus = true;
        }

        var entityMenuMaps = await _menuPermissionAppService.GetNodeMenuListAsync(menuFilter);
        if (entityMenuMaps == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.RequestTimeout);
        }

        foreach (var groupItem in entityMenuMaps.GroupBy(x => x.ClientMenuType))
        {
            var channelMenu = new RoleMenuChannelDto { Channel = groupItem.Key, MenuNodes = new List<RolePermissionNodeDto>() };

            // Get channel menu maps
            var channelMenuMaps = groupItem.ToList();
            if (channelMenuMaps is { Count: > 0 })
            {
                // Get root menus
                var channelRootMenus = channelMenuMaps.Where(x => x.ParentUniqueName is not { Length: > 0 }).ToList();
                if (channelRootMenus is { Count: > 0 })
                {
                    // Convert node tree
                    var channelMenuNodes = channelRootMenus.Select(x => ConvertToRolePermissionNodeDto(x, ref channelMenuMaps, ref rolePermissions)).ToList();

                    // Remove has no leaf node menu
                    var checkedChannelMenuNodes = ControlNodeMenuHasChildNodes(channelMenuNodes);

                    // Set parent grant values
                    var completedMenuNodes = checkedChannelMenuNodes.Select(SetParentGrantValues).ToList();

                    // set menu nodes
                    channelMenu.MenuNodes = completedMenuNodes;
                }
            }

            result.ChannelMenus.Add(channelMenu);
        }

        // Set tree key content for Front-End
        if (result.ChannelMenus is { Count: > 0 })
        {
            foreach (var chn in result.ChannelMenus.Where(ch => ch.MenuNodes is { Count: > 0 }))
            {
                chn.MenuNodes = await SetTreeUniqueKey(chn.MenuNodes, null);
            }
        }

        return result;
    }

    public async Task SetRolePermissionsAsync(RolePermissionsDto filter)
    {
        if (string.IsNullOrWhiteSpace(filter?.RoleUniqueName) || filter.ChannelMenus is not { Count: > 0 })
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var input = filter.ChannelMenus.Where(x => !string.IsNullOrWhiteSpace(x.Channel) && x.MenuNodes is { Count: > 0 }).ToList();
        if (input is not { Count: > 0 })
        {
            throw new BaseHttpException((int)HttpStatusCode.BadRequest);
        }

        var allChannelPermissionList = new List<string>();
        foreach (var channelMenu in input)
        {
            allChannelPermissionList.AddRange(ConvertToSetPermissionDto(channelMenu.MenuNodes));
        }

        var allPermissions = allChannelPermissionList.Distinct().ToList();

        await _permissionGrantRepository.SetManyAsync(
            names: allPermissions,
            providerName: "R",
            providerKey: filter.RoleUniqueName
        );

        // Synch Permission Cache Store
        await EventBus.PublishAsync(eventMessage: new SynchAllPermissionToCacheDbEto());
    }

    // Recursive tree method
    private RolePermissionNodeDto ConvertToRolePermissionNodeDto(MenuMapDto channelMenuMap, ref List<MenuMapDto> allChannelMenuMap, ref List<string> allRolePermissions)
    {
        if (channelMenuMap is null) return null;

        var menuNode = new RolePermissionNodeDto
        {
            MenuMapLocalizedName = L[channelMenuMap.UniqueName],
            Icon = channelMenuMap.Icon,
            OrderNumber = channelMenuMap.OrderNo,
            OrderHierarchy = channelMenuMap.Hierarchy,
            RequiredPermissionUniqueName = $"{channelMenuMap.UniqueName}{WebChannelMenu.LeafPermissionName}",
            HasLink = channelMenuMap.MapType is MenuMapType.Leaf,
            IsGranted = allRolePermissions.Contains($"{channelMenuMap.UniqueName}{WebChannelMenu.LeafPermissionName}"),
            Childs = new List<RolePermissionNodeDto>()
        };

        // Check and Add Child Menu
        var childNodes = allChannelMenuMap.Where(x => x.ParentUniqueName == channelMenuMap.UniqueName).ToList();
        if (childNodes is { Count: > 0 })
        {
            foreach (var child in childNodes.OrderBy(x => x.OrderNo))
            {
                menuNode.Childs.Add(ConvertToRolePermissionNodeDto(child, ref allChannelMenuMap, ref allRolePermissions));
            }
        }

        return menuNode;
    }

    // Recursive tree method
    private List<RolePermissionNodeDto> ControlNodeMenuHasChildNodes(List<RolePermissionNodeDto> menuNodeDtos)
    {
        var checkedNodes = new List<RolePermissionNodeDto>();

        if (menuNodeDtos is not { Count: > 0 }) return checkedNodes;

        foreach (var menuNodeDto in menuNodeDtos)
        {
            if (menuNodeDto.HasLink)
            {
                checkedNodes.Add(menuNodeDto);
                continue;
            }

            if (menuNodeDto.Childs is not { Count: > 0 }) continue;

            menuNodeDto.Childs = ControlNodeMenuHasChildNodes(menuNodeDto.Childs);
            if (menuNodeDto.Childs is { Count: > 0 }) checkedNodes.Add(menuNodeDto);
        }

        return checkedNodes;
    }

    // Recursive tree method
    private RolePermissionNodeDto SetParentGrantValues(RolePermissionNodeDto menuNodeDto)
    {
        if (menuNodeDto is null) return null;

        if (menuNodeDto.IsGranted) return menuNodeDto;

        if (menuNodeDto.Childs is not { Count: > 0 }) return menuNodeDto;

        menuNodeDto.Childs = menuNodeDto.Childs.Select(SetParentGrantValues).ToList();

        menuNodeDto.IsGranted = menuNodeDto.Childs.All(x => x.IsGranted);

        return menuNodeDto;
    }

    private List<string> ConvertToSetPermissionDto(List<RolePermissionNodeDto> permissions)
    {
        var resultList = new List<string>();

        if (permissions is not { Count: > 0 }) return resultList;

        // Get all granted permissions
        foreach (var permissionNode in permissions.Where(permissionNode => permissionNode.IsGranted))
        {
            // Check current menu item has required permission
            if (!string.IsNullOrWhiteSpace(permissionNode.RequiredPermissionUniqueName)
                && StaticData.AllPermissions().Contains(permissionNode.RequiredPermissionUniqueName))
            {
                resultList.Add(permissionNode.RequiredPermissionUniqueName);
            }

            // check child menus
            if (permissionNode.Childs is { Count: > 0 })
            {
                resultList.AddRange(ConvertToSetPermissionDto(permissionNode.Childs));
            }
        }

        return resultList;
    }

    private async Task<List<RolePermissionNodeDto>> SetTreeUniqueKey(List<RolePermissionNodeDto> items, string baseKey)
    {
        int listIndex = 1;

        foreach (var item in items.OrderBy(x => x.OrderNumber))
        {
            item.OrderHierarchy = baseKey == null ? $"{listIndex.ToString()}" : $"{baseKey}-{listIndex.ToString()}";

            if (item.Childs is { Count: > 0 })
            {
                item.Childs = await SetTreeUniqueKey(item.Childs, item.OrderHierarchy);
            }

            listIndex++;
        }

        return items;
    }
}