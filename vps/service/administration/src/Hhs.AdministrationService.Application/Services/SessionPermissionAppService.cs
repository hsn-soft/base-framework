using System.Net;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Data;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Dtos.Filters;
using Hhs.AdministrationService.Application.Contracts.PermissionDomain.Services;
using Hhs.AdministrationService.Domain.old;
using Hhs.Shared.Contracts.Cache;
using HsnSoft.Base;
using HsnSoft.Base.Logging.Abstracts;
using Microsoft.Extensions.DependencyInjection;

namespace Hhs.AdministrationService.Application.Services;

public sealed class SessionPermissionAppService : ApplicationServiceBase, ISessionPermissionAppService
{
    private readonly IFrameworkLogger _logger;

    private readonly ICachePermissionAssignmentRepository _cachePermissionAssignmentRepository;
    private readonly ICachePermissionConstraintAssignmentRepository _cachePermissionConstraintAssignmentRepository;
    private readonly IMenuPermissionAppService _menuPermissionAppService;

    public SessionPermissionAppService(IServiceProvider provider,
        ICachePermissionAssignmentRepository cachePermissionAssignmentRepository,
        ICachePermissionConstraintAssignmentRepository cachePermissionConstraintAssignmentRepository,
        IMenuPermissionAppService menuPermissionAppService) : base(provider)
    {
        _logger = provider.GetRequiredService<IFrameworkLogger>();

        _cachePermissionAssignmentRepository = cachePermissionAssignmentRepository;
        _cachePermissionConstraintAssignmentRepository = cachePermissionConstraintAssignmentRepository;
        _menuPermissionAppService = menuPermissionAppService;
    }

    public async Task<SessionPermissionsDto> GetSessionPermissionsAsync(GetSessionPermissionsFilter filter)
    {
        filter ??= new GetSessionPermissionsFilter();

        var result = new SessionPermissionsDto
        {
            Client = filter.ClientKey,
            Roles = filter.RoleKeys,
            User = filter.UserKey,
            Permissions = new List<SessionPermissionItemDto>(),
            ChannelMenus = new List<SessionMenuChannelDto>()
        };

        // only system user can access system menus
        var menuFilter = new GetMenuMapsFilter();
        if (CurrentUser.IsSystemTenant)
        {
            menuFilter.ShowSystemMenus = true;
        }

        var entityMenuMaps = await _menuPermissionAppService.GetNodeMenuListAsync(menuFilter);
        if (entityMenuMaps == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.RequestTimeout);
        }

        var cachedPermissions = await _cachePermissionAssignmentRepository.GetSessionPermissionsAsync(filter.ClientKey, filter.RoleKeys, filter.UserKey);
        if (cachedPermissions == null)
        {
            throw new BaseHttpException((int)HttpStatusCode.RequestTimeout);
        }

        foreach (var groupItem in cachedPermissions.GroupBy(x => x.Permission))
        {
            result.Permissions.Add(new SessionPermissionItemDto(groupItem.Key, groupItem.Select(s => $"[{s.ProviderName}] {s.ProviderKey}").ToList()));
        }

        var checkPermissionKeys = result.Permissions.Select(s => s.PermissionUniqueName).Distinct().ToList();
        foreach (var groupItem in entityMenuMaps.GroupBy(x => x.ClientMenuType))
        {
            var channelMenu = new SessionMenuChannelDto { Channel = groupItem.Key, MenuNodes = new List<SessionMenuNodeDto>() };

            // check has access menu map
            var checkedChannelMenuMaps = groupItem
                .Where(x =>
                    x.MapType != MenuMapType.Leaf
                    || (x.MapType == MenuMapType.Leaf && checkPermissionKeys.Contains($"{x.UniqueName}{WebChannelMenu.LeafPermissionName}"))
                )
                .ToList();

            if (checkedChannelMenuMaps is { Count: > 0 })
            {
                // Get root menus
                var channelRootMenus = checkedChannelMenuMaps.Where(x => x.MapType != MenuMapType.Caption && x.ParentUniqueName is not { Length: > 0 }).ToList();
                if (channelRootMenus is { Count: > 0 })
                {
                    // Convert node tree
                    var sessionMenuNodes = channelRootMenus.Select(x => ConvertToSessionMenuNodeDto(x, ref checkedChannelMenuMaps)).ToList();

                    // Remove has no leaf node menu
                    var checkedSessionMenuNodes = ControlNodeMenuHasChildNodes(sessionMenuNodes);

                    // add caption menus if exists
                    var channelCaptionMenus = checkedChannelMenuMaps.Where(x => x.MapType == MenuMapType.Caption && x.ParentUniqueName is null).ToList();
                    if (channelCaptionMenus is { Count: > 0 })
                    {
                        var captionMenuNodes = channelCaptionMenus.Select(x => ConvertToSessionMenuNodeDto(x, ref checkedChannelMenuMaps)).ToList();
                        foreach (var captionMenuNode in captionMenuNodes)
                        {
                            // if exist more than caption menu order, add menu caption
                            if (checkedSessionMenuNodes.Any(x => x.OrderNumber > captionMenuNode.OrderNumber))
                            {
                                checkedSessionMenuNodes.Add(captionMenuNode);
                            }
                        }
                    }

                    // set menu nodes
                    channelMenu.MenuNodes = checkedSessionMenuNodes.OrderBy(x => x.OrderNumber).ToList();
                }
            }

            result.ChannelMenus.Add(channelMenu);
        }

        return result;
    }

    // Recursive tree method
    private SessionMenuNodeDto ConvertToSessionMenuNodeDto(MenuMapDto channelMenuMap, ref List<MenuMapDto> allChannelMenuMap)
    {
        if (channelMenuMap is null || allChannelMenuMap is not { Count: > 0 }) return null;

        var menuNode = new SessionMenuNodeDto
        {
            UniqueName = channelMenuMap.UniqueName,
            Url = channelMenuMap.Url,
            Icon = channelMenuMap.Icon,
            OrderNumber = channelMenuMap.OrderNo,
            OrderHierarchy = channelMenuMap.Hierarchy,
            Leaf = channelMenuMap.MapType is MenuMapType.Leaf,
            Caption = channelMenuMap.MapType is MenuMapType.Caption,
            Childs = new List<SessionMenuNodeDto>()
        };

        if (channelMenuMap.MapType is MenuMapType.Leaf or MenuMapType.Caption) return menuNode;

        // Check and Add Child Menu
        var childNodes = allChannelMenuMap.Where(x => x.ParentUniqueName == channelMenuMap.UniqueName).ToList();
        if (childNodes is { Count: > 0 })
        {
            foreach (var child in childNodes.OrderBy(x => x.OrderNo))
            {
                menuNode.Childs.Add(ConvertToSessionMenuNodeDto(child, ref allChannelMenuMap));
            }
        }

        return menuNode;
    }

    // Recursive tree method
    private List<SessionMenuNodeDto> ControlNodeMenuHasChildNodes(List<SessionMenuNodeDto> menuNodeDtos)
    {
        var checkedNodes = new List<SessionMenuNodeDto>();

        if (menuNodeDtos is not { Count: > 0 }) return checkedNodes;

        foreach (var menuNodeDto in menuNodeDtos)
        {
            if (menuNodeDto.Leaf)
            {
                checkedNodes.Add(menuNodeDto);
                continue;
            }

            if (menuNodeDto.Childs is not { Count: > 0 }) continue;

            if (ControlNodeMenuHasChildNodes(menuNodeDto.Childs) is { Count: > 0 }) checkedNodes.Add(menuNodeDto);
        }

        return checkedNodes;
    }
}