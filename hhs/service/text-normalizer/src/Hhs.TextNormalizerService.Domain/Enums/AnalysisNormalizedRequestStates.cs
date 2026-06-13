using System.ComponentModel.DataAnnotations;

namespace Hhs.TextNormalizerService.Domain.Enums;

public enum AnalysisNormalizedRequestStates
{
    [Display(Name = "CreatedWaitForOutline")]
    CreatedWaitForOutline = 0,

    [Display(Name = "Fail")]
    OperationFail = 51,

    [Display(Name = "Success")]
    OperationSuccess = 99
}