using System.ComponentModel.DataAnnotations;

namespace Hhs.AdministrationService.Domain.Enums;

public enum ChannelTypes
{
    [Display(Name = "Unknown", Prompt = "unknown")]
    Unknown = 0,

    [Display(Name = "Back Office", Prompt = "back-office")]
    BackOffice = 1,

    [Display(Name = "Mobile-App", Prompt = "mobile")]
    MobileApp = 2,
}