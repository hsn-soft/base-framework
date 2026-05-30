using System.ComponentModel.DataAnnotations;

namespace Hhs.ContentService.Domain.Enums;

public enum AnalysisContentOperationStates
{
    [Display(Name = "CreatedWaitForNormalize")]
    CreatedWaitForNormalize = 0,
 
    [Display(Name = "NormalizedWaitForVideoGeneration")]
    NormalizedWaitForVideoGeneration = 11,
 
    [Display(Name = "Fail")]
    OperationFail = 51,
 
    [Display(Name = "Success")]
    OperationSuccess = 99
}