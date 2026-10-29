using Edx.IdentityService.Application.Contracts.IdentityDomain.Dtos;
using Edx.IdentityService.Application.Contracts.IdentityDomain.Services;
using Edx.IdentityService.Domain.IdentityDomain.Entities;
using Edx.IdentityService.Domain.IdentityDomain.Repositories;
using HsnSoft.Base.Application;

namespace Edx.IdentityService.Application.Services;

/// <summary>
/// The menu of the caller. An item is in it only when the caller may see it: a leaf when it has the permission of its visibility (and a leaf that has none is shown to nobody), a node when something under it is shown, a caption when something after it, up to the
/// next caption, is. So a caller that may see nothing gets an empty menu, and the client says so. Each leaf carries what its page needs and whether the caller has each of it, for the client to enable its buttons and to know what it will be refused.
/// </summary>
public sealed class MenuAppService(IMenuRepository menus, IPermissionGrantRepository grants, IServiceProvider services) : ApplicationService(services), IMenuAppService
{
    public async Task<List<MenuDto>> GetMyMenusAsync(CancellationToken cancellationToken = default)
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
            if (visible[index].NodeType != nameof(MenuNodeType.Caption))
            {
                result.Add(visible[index]);
                continue;
            }

            int next = visible.FindIndex(index + 1, item => item.NodeType == nameof(MenuNodeType.Caption));
            int end = next < 0 ? visible.Count : next;
            if (end > index + 1)
            {
                result.Add(visible[index]);
            }
        }

        return result;
    }

    // the items under a parent, in order, with only the ones the caller may see (a caption is decided by its place, in the caller)
    private static IEnumerable<MenuDto> Children(Guid? parentId, ILookup<Guid?, MenuWithPermissions> byParent, HashSet<string> granted)
    {
        foreach (MenuWithPermissions item in byParent[parentId].OrderBy(entry => entry.Menu.SortOrder).ThenBy(entry => entry.Menu.Code, StringComparer.Ordinal))
        {
            Menu menu = item.Menu;
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
                        yield return ToDto(menu, [], [.. item.Permissions.Select(permission => new MenuPermissionDto
                        {
                            Code = permission.Code,
                            Type = permission.Type.ToString(),
                            Relation = permission.Relation.ToString(),
                            Granted = granted.Contains(permission.Code),
                        })]);
                    }

                    break;
            }
        }
    }

    private static MenuDto ToDto(Menu menu, IReadOnlyList<MenuDto> children, IReadOnlyList<MenuPermissionDto> permissions)
        => new() { Code = menu.Code, Title = menu.Title, Route = menu.Route, Icon = menu.Icon, NodeType = menu.NodeType.ToString(), Children = children, Permissions = permissions };
}
