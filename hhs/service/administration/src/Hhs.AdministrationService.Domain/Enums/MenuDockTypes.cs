using System.ComponentModel.DataAnnotations;

namespace Hhs.AdministrationService.Domain.Enums;

public enum MenuDockTypes
{
    [Display(Name = "Unknown")]
    Unknown = 0,

    [Display(Name = "Top-Menu")]
    TopMenu = 1,

    [Display(Name = "Left-Menu")]
    LeftMenu = 2,

    [Display(Name = "Right-Menu")]
    RightMenu = 3,

    [Display(Name = "Bottom-Menu")]
    BottomMenu = 4,
}