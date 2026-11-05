using HsnSoft.Base;

namespace Hsx.IdentityService.Domain.MenuDomain.Exceptions;

/// <summary>A menu item is not there (or the caller may not see it): answered as 404.</summary>
public static class AppMenuNotFound
{
    public static NotFoundException For(Guid id) => new NotFoundException($"A menu item {id} was not found.").WithKey("app_menu_not_found", id);
}
