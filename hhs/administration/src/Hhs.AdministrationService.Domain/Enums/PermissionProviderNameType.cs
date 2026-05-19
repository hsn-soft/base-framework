using System.ComponentModel.DataAnnotations;

namespace Hhs.AdministrationService.Domain.Enums;

public enum PermissionProviderNameType
{
    [Display(Name = "Unknown")]
    Unknown = 0,

    [Display(Name = "User")]
    User = 1,

    [Display(Name = "Role")]
    Role = 2,

    [Display(Name = "Client")]
    Client = 3
}