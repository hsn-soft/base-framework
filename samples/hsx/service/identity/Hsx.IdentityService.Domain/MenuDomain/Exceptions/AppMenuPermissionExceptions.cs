using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.MenuDomain.Exceptions;

/// <summary>The permission of a menu item is not there (or the caller may not see it): answered as 404.</summary>
public static class AppMenuPermissionNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"The permission of a menu item {id} was not found.").WithKey("app_menu_permission_not_found", id);
}
