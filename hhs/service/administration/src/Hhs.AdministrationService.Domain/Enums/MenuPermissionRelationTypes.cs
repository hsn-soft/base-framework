using System.ComponentModel.DataAnnotations;

namespace Hhs.AdministrationService.Domain.Enums;

public enum MenuPermissionRelationTypes
{
    [Display(Name = "Unknown")] Unknown = 0,

    [Display(Name = "OperationAccess")] OperationAccess = 1,

    [Display(Name = "Constraint")] Constraint = 2,

    [Display(Name = "Visibility")] Visibility = 3,

    [Display(Name = "Required Service")] RequiredService = 4,

    [Display(Name = "Action Service")] ActionService = 5,
}