using HsnSoft.Base.Application.Dtos;
using Hsx.IdentityService.Application.Contracts.MenuDomain.Dtos;
using Hsx.IdentityService.Application.Contracts.MenuDomain.Services;
using Hsx.IdentityService.Domain.Enums;
using Hsx.IdentityService.Domain.MenuDomain.Entities;
using Hsx.IdentityService.Domain.MenuDomain.Repositories;
using Hsx.IdentityService.Domain.PermissionDomain.Repositories;

namespace Hsx.IdentityService.Application.Services;

/// <summary>
/// The menu of the caller. An item is in it only when the caller may see it: a leaf when it has the permission of its visibility (and a leaf that has none is shown to nobody), a node when something under it is, a caption when something after it, before the
/// next caption, is. So a caller that may see nothing gets an empty menu, and the client says so. Each leaf carries what its page needs and whether the caller has each of it, for the client to enable its buttons.
/// </summary>
public sealed class MenuAppService(IAppMenuRepository menus, IPermissionGrantRepository grants, IServiceProvider services) : ApplicationServiceBase(services), IMenuAppService
{
    public async Task<ListDataResultDto<MenuDto>> GetMyMenusAsync(CancellationToken cancellationToken = default)
    {
        HashSet<string> granted = CurrentUser.Id is { } id
            ? [.. (await grants.GetEffectiveGrantsAsync(id, cancellationToken).ConfigureAwait(false)).Select(grant => grant.Code)]
            : [];
        IReadOnlyList<MenuWithPermissions> all = await menus.GetActiveAsync(MenuChannelType.BackOffice, MenuDockType.SidebarMenu, cancellationToken).ConfigureAwait(false);
        ILookup<Guid?, MenuWithPermissions> byParent = all.ToLookup(item => item.Menu.ParentId);
        List<MenuDto> visible = [.. Children(null, byParent, granted)];

        // a caption stands for what follows it: it is kept when something after it, before the next caption, is shown
        List<MenuDto> result = [];
        for (int index = 0; index < visible.Count; index++)
        {
            if (visible[index].NodeType != MenuNodeType.Caption)
            {
                result.Add(visible[index]);
                continue;
            }

            int next = visible.FindIndex(index + 1, item => item.NodeType == MenuNodeType.Caption);
            int end = next < 0 ? visible.Count : next;
            if (end > index + 1)
            {
                result.Add(visible[index]);
            }
        }

        return new ListDataResultDto<MenuDto>(result);
    }

    // the items under a parent, in order, with only the ones the caller may see (a caption is decided by its place, in the caller)
    private static IEnumerable<MenuDto> Children(Guid? parentId, ILookup<Guid?, MenuWithPermissions> byParent, HashSet<string> granted)
    {
        foreach (MenuWithPermissions item in byParent[parentId].OrderBy(entry => entry.Menu.SortOrder).ThenBy(entry => entry.Menu.Code, StringComparer.Ordinal))
        {
            AppMenu menu = item.Menu;
            switch (menu.NodeType)
            {
                case MenuNodeType.Caption:
                    yield return ToDto(menu, [], []);
                    break;
                case MenuNodeType.Node:
                    List<MenuDto> children = [.. Children(menu.Id, byParent, granted)];
                    if (children.Count > 0)
                    {
                        yield return ToDto(menu, children, []);
                    }

                    break;
                default:
                    if (item.Permissions.Any(permission => permission.Relation == MenuPermissionRelationType.Visibility && granted.Contains(permission.Code)))
                    {
                        yield return ToDto(menu, [], [.. item.Permissions.Select(permission => new MenuPermissionDto { Code = permission.Code, Type = permission.Type, Relation = permission.Relation, Granted = granted.Contains(permission.Code) })]);
                    }

                    break;
            }
        }
    }

    private static MenuDto ToDto(AppMenu menu, IReadOnlyList<MenuDto> children, IReadOnlyList<MenuPermissionDto> permissions)
        => new() { Code = menu.Code, Title = menu.Title, Route = menu.Route, Icon = menu.Icon, NodeType = menu.NodeType, Children = children, Permissions = permissions };
}
