using System.ComponentModel.DataAnnotations;

namespace Hsx.IdentityService.Domain.Enums;

/// <summary>The client a menu is for.</summary>
public enum MenuChannelType
{
    [Display(Name = "Mobile", Prompt = "mobile")]
    Mobile = 1,

    [Display(Name = "Back Office", Prompt = "back-office")]

    BackOffice = 2,
}
