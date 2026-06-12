using System.ComponentModel.DataAnnotations;

namespace Hhs.EventManagerService.Domain.Enums;

public enum FailedIntegrationEventStates
{
    [Display(Name = "CreatedWaitForHandling")]
    CreatedWaitForHandling = 0,

    [Display(Name = "Fail")]
    OperationFail = 51,

    [Display(Name = "Success")]
    OperationSuccess = 99
}