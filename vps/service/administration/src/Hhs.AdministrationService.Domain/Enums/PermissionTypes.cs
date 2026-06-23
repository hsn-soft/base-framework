using System.ComponentModel.DataAnnotations;

namespace Hhs.AdministrationService.Domain.Enums;

public enum PermissionTypes
{
    [Display(Name = "Unknown", Prompt = "unknown")]
    Unknown = 0,

    [Display(Name = "Operation Access", Prompt = "operation")]
    OperationAccess = 1,

    [Display(Name = "Constraint", Prompt = "constraint")]
    Constraint = 2,

    [Display(Name = "Page", Prompt = "page")]
    Page = 3,

    [Display(Name = "Service", Prompt = "service")]
    Service = 4,
}