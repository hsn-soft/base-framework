using System.ComponentModel.DataAnnotations;

namespace Hhs.IdentityService.Domain.Enums;

public enum TenantTypes
{
    [Display(Name = "Unknown", Prompt = "unknown")]
    Unknown = 0,

    [Display(Name = "System", Prompt = "system")]
    System = 1,

    [Display(Name = "Reseller", Prompt = "reseller")]
    Reseller = 2,

    [Display(Name = "Account", Prompt = "account")]
    Account = 3
}