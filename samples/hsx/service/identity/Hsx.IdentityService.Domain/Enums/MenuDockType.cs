using System.ComponentModel.DataAnnotations;

namespace Hsx.IdentityService.Domain.Enums;

/// <summary>Where a menu is shown.</summary>
public enum MenuDockType
{
    [Display(Name = "Sidebar Menu", Prompt = "sidebar-menu")]
    SidebarMenu = 1,

    [Display(Name = "Topbar Menu", Prompt = "topbar-menu")]

    TopbarMenu = 2,
}
